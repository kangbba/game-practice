# 세이브 시스템 설계 (추후 도입 예정)

> 상태: **설계 확정, 구현 보류.** 게임플레이가 자리잡은 뒤 도입한다.
> 개발 중에는 세이브 데이터가 오히려 방해되므로(스키마가 계속 바뀜) 지금은 넣지 않는다.
> 작성일: 2026-09-16

## 목표

자동사냥(웨이브 반복) 게임에서 **전투 도중 상태까지 정확히 복원**한다.
적 8마리 중 4마리를 죽여놓고 껐다 켜면, 남은 4마리가 같은 위치·같은 HP로 서 있어야 한다.

## 핵심 원리: 데이터가 진실, 씬은 뷰

> **게임은 언제나 GameState로부터 구동된다. 뷰는 레코드를 따라간다.**

- "복원"이라는 개념을 별도로 만들지 않는다. 새 게임 = 초기값 GameState, 이어하기 = 역직렬화한 GameState. 실행 코드는 이 둘을 구분하지 않는다.
- 저장이라는 행위도 사실상 없다 — GameState가 항상 최신 진실이므로, 오토세이브는 들고 있는 객체를 직렬화하는 것뿐. 씬을 긁어모으는 Capture 단계가 없다.
- 결정론적 리플레이(시드+입력 녹화) 방식은 배제한다. 실시간 유저 입력과 float 비결정론 때문에 세션 간 보장이 불가능하다.

## 3층 구조와 네이밍 규약

| 층 | 접미사 | 성격 | 예 |
|---|---|---|---|
| 청사진 | `~Definition` | 불변·공유·재사용. 기획값 | `EnemyDefinition` (maxHP, moveSpeed, attackPower…) |
| 장부 | `~Record` | 가변·개별·**저장 대상**. 인스턴스당 하나 | `EnemyRecord` (enemyID, position, currentHP) |
| 뷰 | 무접미사 (MonoBehaviour) | 휘발. 레코드를 표현만 | `Enemy` (기존 클래스) |

한 줄 규칙: **"Definition은 읽기만, Record는 저장만, 뷰는 표현만."**

- 연결은 ID로: `EnemyRecord.EnemyID` → `EnemyDefinition` 조회.
- Record에 기획값(maxHP 등)을 복사해 넣지 않는다. 넣는 순간 밸런스 패치가 세이브 파일에 안 먹는 고전 버그가 생긴다.
- 현재 `CharacterStats`가 하던 역할이 Definition이다. 추후 ScriptableObject로 빼기 자연스럽다.
- 최상위는 `GameState`보다 `SaveData`/`GameRecord`처럼 규약에 맞는 이름으로.

```csharp
[Serializable]
public class GameState            // 최상위 SSOT
{
    public int Wave;
    public HeroRecord Hero;       // heroID, position, currentHP
    public List<EnemyRecord> Enemies;
    public long Gold;             // 추후 재화·성장
    public long SavedAtUtc;       // 오프라인 정산용
}
```

## 저장/재생성 경계

- **저장하는 것 (본질 상태)**: 적 종류·위치·현재HP, 히어로 위치·현재HP, 웨이브 번호, 재화/성장. → 플레이어가 차이를 감지할 수 있는 전부.
- **저장하지 않고 로드 시 재생성 (파생·과도 상태)**: AI 타이머(`_nextThinkTime`)·조준점, 공격 쿨다운, 파티클, HP바. → 로드 직후 0.5초 내 재타겟팅되므로 체감 불가.
- 페이즈도 저장하지 않는다. `state.Enemies`가 비어있지 않으면 Combat 도중이었던 것 — **상태가 곧 위치다.**

## 스폰의 단일 경로

스폰 함수는 항상 Record를 받는다. 초기 생성과 복원의 차이는 Record가 방금 만들어졌느냐 파일에서 왔느냐뿐.

```csharp
// 뷰 구체화는 오직 이 경로 하나
public Enemy Spawn(EnemyRecord record)
{
    var definition = _enemyAssetManager.GetDefinition(record.EnemyID);
    var view = Object.Instantiate(definition.Prefab);
    view.Bind(record, definition);   // 뷰는 레코드를 구독할 뿐
}

// CombatPhase.Enter — "새 웨이브" = 새 레코드 생성일 뿐
if (state.Enemies.Count == 0)
    for (...) state.Enemies.Add(EnemyRecord.CreateNew(id, RandomPosition(), def.MaxHP));
foreach (var record in state.Enemies)
    _enemyManager.Spawn(record);     // 새것이든 로드된 것이든 같은 줄

// PreparePhase — 히어로도 완전 대칭
state.Hero ??= HeroRecord.CreateNew(HeroID.Aldric, Vector3.zero, def.MaxHP);
_heroManager.Spawn(state.Hero);
```

- `CreateNew` 팩토리가 초기값 규칙("새로 태어나면 풀피")의 유일한 거처. Spawn은 초기값을 모른다.
- **죽음 = 리스트에서 레코드 제거**가 진실이고, 뷰 파괴·연출은 결과. "죽였는데 켜보니 살아있음" 버그가 구조적으로 불가능해진다.
- HP 소유권은 뷰(`Character`)에서 Record로 이동. 뷰는 Record의 HP를 구독해 플래시/사망 연출만. 데미지 처리도 Record의 HP를 깎는 것.
- 위치만 예외적으로 양방향: 이동의 주인은 Transform이므로 매 프레임(혹은 저장 직전) Transform → Record 미러링. 이 한 줄이 유일한 동기화.
- R3 배선은 기존 패턴 그대로 확장: 지금 `Spawned` Subject에 WorldUIManager·ParticleManager가 반응하듯, 레코드 리스트의 추가/제거에 뷰 생성/파괴를 물린다.

## 저장 타이밍

- 전투 중 5~10초 주기 오토세이브 + `OnApplicationQuit`/`OnApplicationPause`. 캡처(직렬화)는 한 프레임 안에서 동기로, 파일 쓰기만 비동기로.
- **원자적 쓰기 필수**: 임시 파일에 쓰고 rename. 쓰는 도중 전원이 나가도 직전 세이브가 살아있어야 한다.
- 위치: JSON → `Application.persistentDataPath`.
- 영속 진행도(웨이브·골드)는 ResultPhase에서 커밋, 전투 현장(적 배치·HP)은 오토세이브 — 2층 커밋 구조.

## 웨이브 루프와의 관계 (선행 작업)

세이브와 무관하게 확정된 페이즈 구조:

```csharp
await phaseManager.RunAsync(new PreparePhase(...));   // 맵+히어로, 1회
while (!token.IsCancellationRequested)
{
    await phaseManager.RunAsync(new CombatPhase(enemyManager, wave));  // 종료 조건: 적 전멸
    await phaseManager.RunAsync(new ResultPhase(wave));                // 웨이브 정산 + 짧은 딜레이
    wave++;
}
```

- 한 웨이브 = 한 CombatPhase 실행. `UniTask.Never` → "적 전멸까지 대기"(폴링 `UniTask.WaitUntil`이면 충분)로 교체.
- ResultPhase의 현재 정리 로직(히어로 디스폰+맵 제거)은 루프 안에서 하면 안 되므로 제거하고, 웨이브 정산 슬롯으로 남긴다.
- 히어로 사망 처리(부활 vs 사냥 종료)는 미결 — 당장은 종료 조건을 적 전멸 하나로.

## 부수 이득

- 전투 상태가 순수 데이터이므로 **씬 없이 시뮬레이션 가능** → 오프라인 정산을 수식 근사가 아니라 실제 틱으로 계산 가능, 전투 로직 단위 테스트 가능.
- 오프라인 정산: `SavedAtUtc` 대비 경과 시간 ÷ 웨이브당 평균 소요 시간 → 웨이브·재화 반영. GameState가 SSOT면 함수 하나.

## 도입 시 작업 순서

1. `~Definition`/`~Record` 데이터 모델과 `GameState`
2. `Character.Bind(record, definition)`로 HP 소유권 이전
3. `EnemyManager`/`HeroManager`를 `Spawn(record)` 뷰 팩토리로 재정의
4. Prepare/Combat/Result를 레코드 기반 웨이브 루프로
5. `SaveManager` (주기 저장 + 원자적 쓰기)
