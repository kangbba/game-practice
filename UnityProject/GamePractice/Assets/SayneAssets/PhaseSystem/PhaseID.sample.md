# PhaseID 예시 문서

페이즈 키 상수 문서의 표준 형태. 실제 파일은 각 프로젝트에 두고(예: `Scripts/Phases/PhaseID.cs`), 이 문서는 규칙의 예시다.

```csharp
namespace Sayne
{
    public static class PhaseID
    {
        public static class Root
        {
            public const string Main = "Main";
            public const string Shop = "Shop";
            public const string Battle = "Battle";
        }

        public static class Battle
        {
            public const string Prepare = "Battle/Prepare";
            public const string Combat = "Battle/Combat";
            public const string Result = "Battle/Result";
            public const string Pause = "Battle/Pause";
        }
    }
}
```

## 규칙

- **중첩 클래스 하나 = 머신 하나.** `Root`는 게임 최상위 머신, `Battle`은 `BattlePhase`가 소유한 자식 머신. 이 문서의 클래스 목록이 곧 머신 목록이다.
- **값은 경로 형태.** `"Battle/Prepare"`처럼 소유 머신을 앞에 붙여, 로그·세이브에 찍혔을 때 문자열만으로 계층이 보이게 한다.
- **폴더·클래스명도 같은 축.** 페이즈 파일은 머신 이름 폴더에 두고, 서브 페이즈 클래스는 머신 이름을 접두사로 붙인다 (`Battle/BattlePreparePhase.cs`). 루트 장면은 접두사 없음 (`MainPhase.cs`).
- **오타 방지.** 코드에서는 문자열 리터럴 대신 반드시 `PhaseID.Battle.Prepare` 상수를 쓴다. 잘못 치면 컴파일 에러로 잡힌다.
- 서브 머신이 새로 생기면 세트로 추가한다: `PhaseID` 중첩 클래스 + 페이즈 폴더 + 접두사 클래스들.
