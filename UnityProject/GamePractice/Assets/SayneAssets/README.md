# SayneAssets

프로젝트를 넘어 재사용하는 공용 코드. 이 폴더를 통째로 복사하면 다른 프로젝트에서 그대로 쓸 수 있다.
의존: R3 (구독 해제용). 프로젝트별 코드는 여기에 넣지 않는다.

## Core

| 파일 | 역할 |
|---|---|
| `ManagerBase` | 순수 클래스 매니저의 공통 수명. `Init()` → `OnInit()`, `Release()` → `OnRelease()`를 강제하고, `LifeToken`(CancellationToken)을 제공한다. R3 구독은 `.RegisterTo(LifeToken)`, UniTask는 `LifeToken`을 넘기면 `Release` 시점에 자동 취소된다. |

## PhaseSystem

계층형 상태 머신. 기계는 게임 의미(전투, 상점 등)를 모른다. 의존: R3, UniTask.

| 파일 | 역할 |
|---|---|
| `PhaseBase` | 페이즈 한 장면. `Key`(식별 문자열), `Enter(token)` / `MainLogicAsync(token)` / `Exit()`. 프로젝트에서 상속해 구체 페이즈를 만든다. |
| `PhaseManager` | 페이즈를 갈아끼우고 흐름을 돌린다. `RunAsync(PhaseBase)`로 시작하면 각 페이즈가 돌려준 다음 페이즈를 따라 끝까지 흐른다. 현재 페이즈는 `CurrentPhase`(ReadOnlyReactiveProperty)로 노출한다. |

### 사용 규칙

- **전환은 반환값으로.** `MainLogicAsync`가 반환하는 것이 곧 "내 일은 끝났다"이고, 돌려준 페이즈가 다음 차례다. `null`이면 거기서 흐름이 끝난다. 페이즈가 매니저를 붙잡고 직접 갈아끼우지 않는다 — 아직 안 끝난 자기를 끝내는 꼴이라 자기 토큰이 취소된다.
- **토큰은 페이즈마다 새로 난다.** `Enter`와 `MainLogicAsync`가 받는 토큰은 그 페이즈가 나가는 순간 취소된다. 구독과 비동기 작업에 그대로 넘기면 정리가 따라온다.
- 전환은 객체로: `return new BattlePhase(...)`. 문자열 `Key`는 식별·로그·저장용이다.
- 키는 프로젝트에서 상수 문서(예: `PhaseID`)로 관리한다. 값은 `"Battle/Prepare"`처럼 경로 형태로 계층을 드러낸다.
- 계층(HSM): 페이즈가 자식 `PhaseManager`를 소유하면 서브 상태 머신이 된다. 소유한 페이즈는 반드시 `Enter`에서 자식 `Init()`, `Exit`에서 자식 `Release()`를 부른다. 이 두 줄로 정리 연쇄가 바깥에서 안쪽까지 전파된다.
- 부모의 생명주기가 자식을 소유한다. 자식 머신을 바깥에서 직접 만지지 않는다.

## UI / Tutorial

안내 대사 위젯. 그림과 글을 받아서 틀 뿐, 누구의 어떤 대사인지는 모른다. 의존: UniTask, TextMeshPro.
연출은 전부 unscaled 시간으로 돌아서 게임이 멈춘(timeScale 0) 동안에도 움직인다.

| 파일 | 역할 |
|---|---|
| `SpeechBubble` | 말풍선 한 개. `PlayAsync(text, token)` — 튀어나오고, 한 글자씩 찍고, `Advance()` 를 받으면 닫힌다. 글이 길면 풍선이 늘어난다. |
| `TutorialWidget` (+프리팹) | 화면 아래 초상화 + 말풍선. `PlayAsync(portrait, text, token)`. 화면 아무 데나 누르면 넘어간다. |
| `OverlaySpeechBubble` (+프리팹) | 캐릭터 머리 위를 따라다니는 말풍선. `OverlayHPBar` 처럼 만들자마자 `Init(camera, target, worldOffset, screenOffset, portrait)` 로 붙인 뒤 `PlayAsync(text, token)`. 끝나면 만든 쪽이 치운다. |

```csharp
var widget = Object.Instantiate(tutorialWidgetPrefab, canvas.transform);   // 스크린 오버레이 캔버스 + GraphicRaycaster
await widget.PlayAsync(portraitSprite, "조이스틱을 끌어서 움직여 보세요.", token);

var bubble = Object.Instantiate(overlayBubblePrefab, canvas.transform);
bubble.Init(camera, hero.transform, new Vector3(0f, 2.2f, 0f), new Vector2(0f, 36f), portraitSprite);
await bubble.PlayAsync("저 고블린부터 잡자!", token);
Object.Destroy(bubble.gameObject);
```

프리팹은 폰트(`Assets/Fonts/TMP/SB_Aggro_Bold SDF`)를 참조한다. 다른 프로젝트로 옮기면 폰트만 다시 지정한다.

## UI / DamageText

맞은 자리에 떠오르는 숫자. 의존: DOTween, TextMeshPro.

| 파일 | 역할 |
|---|---|
| `DamageText` | 튀어오르고, 떠오르며, 흐려지고 스스로 사라진다. `Show(text)` 하나뿐이고, **문자열을 그대로 띄운다** — 얼마나 아팠는지·크리티컬인지 같은 해석은 부르는 쪽이 끝내고 온다. |
| `DamageText.example.prefab` | 흰 글자 + 검은 아웃라인 견본. 같은 폴더의 `DamageTextOutline.mat` 을 써서 혼자 선다. 게임에서는 이걸 복제해 자기 폴더에 두고 글꼴·색을 갈아끼운다. |

```csharp
var text = Object.Instantiate(damageTextPrefab, screenCanvas.transform);   // 스크린 오버레이 캔버스
text.RectTransform.position = camera.WorldToScreenPoint(hitPoint);
text.Show("128");
```

## 사용법

### 0. 새 프로젝트에 도입

1. 이 폴더(`SayneAssets/`)를 `Assets/` 아래로 복사한다.
2. R3 패키지가 설치돼 있어야 한다 (`manifest.json`에 `com.cysharp.r3`).
3. 게임 코드 asmdef의 `references`에 `"SayneAssets"`를 추가한다. asmdef를 안 쓰는 프로젝트면 그대로 동작한다.

### 1. 키 문서 만들기

페이즈 키를 상수로 모은 `PhaseID` 문서를 프로젝트에 하나 둔다. 중첩 클래스 하나가 머신 하나이고, 값은 `"Battle/Prepare"` 경로 형태다. 상세 규칙과 전체 예시는 [PhaseSystem/PhaseID.sample.md](PhaseSystem/PhaseID.sample.md) 참고.

### 2. 페이즈 만들기

`PhaseBase`를 상속한다. `Enter`에서 만든 것은 `Exit`에서 치운다. 할 일은 `MainLogicAsync`에 쓰고, 끝나면서 다음 페이즈를 돌려준다.

```csharp
public class PreparePhase : PhaseBase
{
    public override string Key => PhaseID.BattleSub.Prepare;

    public override void Enter(CancellationToken token) { }

    public override async UniTask<PhaseBase> MainLogicAsync(CancellationToken token)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);

        return new CombatPhase();   // 내 일은 끝났고, 다음은 얘다
    }

    public override void Exit() { }
}
```

끝이 없는 페이즈(예: 무한히 도는 전투)는 `while (true)` 로 돌면 된다. 반환할 일이 없다.

### 3. 조립하고 시작하기 (컴포지션 루트)

```csharp
var phaseManager = new PhaseManager("RootPhase", destroyCancellationToken);
phaseManager.Init();

phaseManager.RunAsync(new LoadingPhase(inGamePhase)).Forget();   // 시작 페이즈

// 끝날 때 (OnDestroy 등)
phaseManager.Release();   // 현재 페이즈 Exit까지 연쇄 정리
```

생성자의 이름은 로그에 찍히는 머신 이름이고, 토큰은 이 머신의 수명이다. 그 토큰이 취소되면 돌던 페이즈도 같이 멈춘다.

첫 페이즈만 넘기면 된다. 그 뒤로는 각 페이즈가 돌려주는 대로 흐른다 — 조립하는 쪽이 순서를 알 필요가 없다.

전환할 때마다 `RootPhase: Loading -> InGame` 형식의 로그가 찍힌다.

### 4. 계층 만들기 (복합 페이즈)

페이즈가 자식 `PhaseManager`를 소유하면 그 안이 서브 상태 머신이 된다. `Enter`에서 `Init`, `Exit`에서 `Release` — 이 두 줄이 규약의 전부다.

```csharp
public class BattlePhase : PhaseBase
{
    private PhaseManager _sub;

    public override string Key => PhaseID.Battle;

    public override void Enter(CancellationToken token)
    {
        // 자식 머신의 수명은 내 토큰이다. 내가 나가면 자식도 같이 멈춘다.
        _sub = new PhaseManager("BattlePhase", token);
        _sub.Init();
    }

    public override async UniTask<PhaseBase> MainLogicAsync(CancellationToken token)
    {
        await _sub.RunAsync(new PreparePhase());   // 준비 → 전투 → 정산 이 다 끝날 때까지

        return new ResultPhase();
    }

    public override void Exit()
    {
        _sub.Release();   // 자식의 현재 페이즈 Exit까지 전파
    }
}
```
