# 코딩 취향 (Sayne)

> AI가 멋대로 수정하는 문서가 아니다. 이 문서에 추가·수정을 명시적으로 부탁했을 때만 고친다.

내가 중요시하는 습관은 SSOT · YAGNI · 캡슐화 · SOLID다.

## 작업 방식

- 별도 지시가 없는 한 시킨 것만, 보수적으로, 안전하게 작성
- 설계 갈림길이 있으면 마음대로 고르지 말고 물어본다
- 어떤 스크립트가 재사용 가치가 있을 때 역으로 제의한다
- R3 · UniRx · DOTween 사용을 선호하니, 쓸 만한 곳이 보이면 역으로 제의한다
- 아래 네이밍·코드·Unity 관습을 지킬 것

## 대화

- 간결하게. 설명 장황하게 하지 말 것
- 묻기만 한 건 실행하지 말 것. 묻는 건 진짜 궁금해서 묻는 거지 추궁이나 특정 행동을 바라는 비꼬기가 아님

## 네이밍

- 네임스페이스는 프로젝트명이 아니라 항상 `Sayne`
- private 필드는 `_camelCase`. 전부 닫고 시작하고, 실제 호출부가 생겼을 때만 프로퍼티로 연다
- bool은 `is` / `has` / `can` 접두사 — `_isAlive`
- 런타임에 변하는 현재값에는 `current`를 붙여 설정값과 구분한다 — `_currentHP` vs `_maxHP`
- 초기화 메서드는 `Initialize`가 아니라 `Init`
- 비동기 메서드는 `Async` 접미사 — `MainLogicAsync`
- 두 글자 약어는 둘 다 대문자 — `HP`, `MaxHP`
- 상수는 PascalCase. `UPPER_CASE` 안 씀
- 헝가리안 표기 안 씀
- `Button`은 `button`이 아니라 `btn`으로 줄인다 — `_rerollBtn`
- 임시·테스트값에는 `Test` 접두사 — `TestHeroMaxHP`

## R3

- 상태는 `ReactiveProperty`, 일반 변수 규칙 그대로 — `IsAlive`, `CurrentHP`
- 사건은 과거형 — `Attacked`, `Damaged`, `Spawned`
- 스트림에 `On~` / `~Property` / `Rp` 안 붙임

## 코드

- 상황에 따라 `var` 써도 됨
- 중복은 두 번째가 아니라 세 번째에 뽑는다
- 주석은 최소화한다. 설명이 필요하면 주석 대신 코드가 드러내게 고친다

## Unity

- 생명주기 함수(`Awake` / `Update` 등)는 한 줄이어도 중괄호 블록. `=>` 금지
- `SerializeField`는 바인딩에만 쓰는 걸 선호한다. 밸런스 수치나 설정값을 인스펙터에 노출하는 건 선호하지 않는다

## 커밋

- 접두사는 `feat:` / `fix:` / `refactor:`
- 제목은 한글
