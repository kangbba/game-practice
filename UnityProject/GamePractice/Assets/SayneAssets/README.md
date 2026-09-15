# SayneAssets

프로젝트를 넘어 재사용하는 공용 코드. 이 폴더를 통째로 복사하면 다른 프로젝트에서 그대로 쓸 수 있다.
의존: R3 (구독 해제용). 프로젝트별 코드는 여기에 넣지 않는다.

## Core

| 파일 | 역할 |
|---|---|
| `ManagerBase` | 순수 클래스 매니저의 공통 수명. `Init()` → `OnInit()`, `Release()` → `OnRelease()`를 강제하고, `LifeToken`(CancellationToken)을 제공한다. R3 구독은 `.RegisterTo(LifeToken)`, UniTask는 `LifeToken`을 넘기면 `Release` 시점에 자동 취소된다. |

## PhaseSystem

계층형 상태 머신 + 페이즈 연동 UI. 기계는 게임 의미(전투, 상점 등)를 모른다.

| 파일 | 역할 |
|---|---|
| `PhaseBase` | 페이즈 한 장면. `Key`(식별 문자열), `OnEnter` / `OnExit`. 프로젝트에서 상속해 구체 페이즈를 만든다. |
| `PhaseManager` | 상태 전환만 담당. `ChangePhase(PhaseBase)`로 전환(이전 `OnExit` → 다음 `OnEnter`), 현재 페이즈를 `CurrentPhase`(ReadOnlyReactiveProperty)로 노출한다. |
| `PhaseUIBase` | 페이즈와 짝을 이루는 UI 한 장. `PhaseKey`가 현재 페이즈의 `Key`와 일치하면 보이고 아니면 숨는다. MonoBehaviour — 씬의 패널에 붙인다. |
| `PhaseUIManager` | `PhaseManager.CurrentPhase`를 구독해 등록된 `PhaseUIBase`들의 표시를 갱신한다. `RegisterView`로 뷰를 등록한다. |

### 사용 규칙

- 전환은 객체로: `ChangePhase(new BattlePhase(...))`. 문자열 `Key`는 식별·로그·저장용이다.
- 키는 프로젝트에서 상수 문서(예: `PhaseID`)로 관리한다. 값은 `"Battle/Prepare"`처럼 경로 형태로 계층을 드러낸다.
- 계층(HSM): 페이즈가 자식 `PhaseManager`를 소유하면 서브 상태 머신이 된다. 소유한 페이즈는 반드시 `OnEnter`에서 자식 `Init()`, `OnExit`에서 자식 `Release()`를 부른다. 이 두 줄로 정리 연쇄가 바깥에서 안쪽까지 전파된다.
- 부모의 생명주기가 자식을 소유한다. 자식 머신을 바깥에서 직접 만지지 않는다.

## 사용법

### 0. 새 프로젝트에 도입

1. 이 폴더(`SayneAssets/`)를 `Assets/` 아래로 복사한다.
2. R3 패키지가 설치돼 있어야 한다 (`manifest.json`에 `com.cysharp.r3`).
3. 게임 코드 asmdef의 `references`에 `"SayneAssets"`를 추가한다. asmdef를 안 쓰는 프로젝트면 그대로 동작한다.

### 1. 키 문서 만들기

페이즈 키를 상수로 모은 `PhaseID` 문서를 프로젝트에 하나 둔다. 중첩 클래스 하나가 머신 하나이고, 값은 `"Battle/Prepare"` 경로 형태다. 상세 규칙과 전체 예시는 [PhaseSystem/PhaseID.sample.md](PhaseSystem/PhaseID.sample.md) 참고.

### 2. 페이즈 만들기

`PhaseBase`를 상속한다. `OnEnter`에서 만든 것은 `OnExit`에서 치운다.

```csharp
public class PreparePhase : PhaseBase
{
    public override string Key => PhaseID.BattleSub.Prepare;

    public override void OnEnter() { }
    public override void OnExit() { }
}
```

### 3. 조립하고 시작하기 (컴포지션 루트)

```csharp
var phaseManager = new PhaseManager();
phaseManager.Init();
phaseManager.ChangePhase(new BattlePhase(context));   // 시작 페이즈

// 끝날 때 (OnDestroy 등)
phaseManager.Release();   // 현재 페이즈 OnExit까지 연쇄 정리
```

전환할 때마다 `PhaseManager: Battle/Prepare -> Battle/Combat` 형식의 로그가 찍힌다.

### 4. 페이즈 UI 붙이기

1. 씬의 패널 오브젝트에 `PhaseUIBase`를 상속한 스크립트를 붙이고 `PhaseKey`로 짝이 되는 페이즈 키를 돌려준다.

```csharp
public class PrepareUI : PhaseUIBase
{
    public override string PhaseKey => PhaseID.BattleSub.Prepare;
}
```

2. `PhaseUIManager`를 만들고 뷰를 등록한다. 이후는 자동이다 — 페이즈가 바뀌면 키가 일치하는 뷰만 켜진다.

```csharp
var phaseUIManager = new PhaseUIManager(phaseManager);
phaseUIManager.Init();
phaseUIManager.RegisterView(prepareUI);   // [SerializeField]로 바인딩해서 전달
```

표시 방식을 바꾸고 싶으면(페이드 등) `SetVisible`을 재정의한다.

### 5. 계층 만들기 (복합 페이즈)

페이즈가 자식 `PhaseManager`를 소유하면 그 안이 서브 상태 머신이 된다. `OnEnter`에서 `Init`, `OnExit`에서 `Release` — 이 두 줄이 규약의 전부다.

```csharp
public class BattlePhase : PhaseBase
{
    private readonly PhaseManager _sub = new PhaseManager();
    public override string Key => PhaseID.Battle;

    public override void OnEnter()
    {
        _sub.Init();
        _sub.ChangePhase(new PreparePhase());
    }

    public override void OnExit()
    {
        _sub.Release();   // 자식의 현재 페이즈 OnExit까지 전파
    }
}
```

서브 머신의 UI가 필요하면 `new PhaseUIManager(_sub)`를 같은 페이즈 안에서 소유하면 된다.
