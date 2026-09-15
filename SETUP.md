# 새 Unity 프로젝트 셋업

> AI가 멋대로 수정하는 문서가 아니다. 이 문서에 추가·수정을 명시적으로 부탁했을 때만 고친다.

> **빠른 적용**: 아래 내용 전부를 `unity-template/setup.py`가 자동 적용한다. 새 프로젝트를 만들고
> **에디터를 닫은 뒤** `python3 ~/Documents/GitHub/game-practice/unity-template/setup.py <프로젝트경로>`.
> 자세한 건 [unity-template/README.md](unity-template/README.md). 남는 수동 작업은 DOTween Utility Panel의 Setup 하나뿐.

## 깃 세팅

- 유니티용 `.gitignore` 적용 — `Library/ Temp/ Logs/ UserSettings/ obj/ Build(s)/`, IDE 파일(`*.csproj *.sln *.slnx .vscode/ .idea/`), `.DS_Store`, 빌드 산출물 제외. 커밋 대상은 `Assets / Packages / ProjectSettings`만 남게.
- `.gitattributes` — Unity YAML 에셋(`*.unity *.prefab *.asset *.meta` 등)은 text, 이미지·오디오·모델·dll은 binary.

## 패키지 (manifest.json에 git URL로 추가)

```json
"com.cysharp.r3": "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity",
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
"com.gameframex.unity.demigiant.dotween": "https://github.com/gameframex/com.gameframex.unity.demigiant.dotween.git",
"com.github-glitchenzo.nugetforunity": "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity"
```

- DOTween 미러는 `DOTween.Runtime.asmdef`이 포함돼 있어 Utility Panel의 Create ASMDEF 불필요. 설치 후 Setup만 한 번.
- R3 본체는 NuGet 배포라 UPM으로 안 온다. **NuGetForUnity 자동 복원에 의존하면 안 된다** —
  R3.Unity가 R3.dll을 못 찾아 컴파일 에러(`Observable<>` / `Subject<>` CS0246)가 먼저 나면
  도메인 리로드가 막혀 복원이 영영 안 도는 닭-달걀 문제가 생긴다.
  dll을 NuGet에서 직접 받아 `Assets/Packages/<Id>.<Ver>/lib/<tfm>/`에 깔고 `packages.config`를 맞춰둔다
  (`setup.py`가 이걸 한다). 필요한 목록 — R3.Unity.asmdef의 precompiledReferences + R3.dll 참조 기준:
  | 패키지 | 버전 |
  |---|---|
  | R3 | 1.3.1 |
  | Microsoft.Bcl.TimeProvider | 8.0.0 |
  | Microsoft.Bcl.AsyncInterfaces | 8.0.0 |
  | System.ComponentModel.Annotations | 5.0.0 |
  | System.Runtime.CompilerServices.Unsafe | 6.0.0 |
  | System.Threading.Channels | 8.0.0 |

  `System.Buffers` / `System.Memory`는 Unity가 netstandard 2.1 shim으로 주니 넣지 않는다(중복 어셈블리 에러).
- Input System, 2D Sprite, Unity MCP(`com.unity.ai.assistant`)는 Unity 레지스트리에서 설치 (템플릿에 이미 포함된 경우 많음).
- IAP·Ads·Analytics는 Unity Cloud 연결 없으면 Package Manager 에러를 띄우니 안 쓰면 제거.

## asmdef

게임 코드는 `Assets/Scripts/Game.asmdef` 하나로 시작:

```json
{
    "name": "Game",
    "rootNamespace": "Game",
    "references": ["UniTask", "R3.Unity", "DOTween.Runtime", "Unity.InputSystem"],
    "overrideReferences": false,
    "autoReferenced": true
}
```

`overrideReferences: false`라 NuGet으로 받은 R3.dll은 자동 참조된다.

에디터 확장 코드는 `Assets/Scripts/Editor/Game.Editor.asmdef` — `"references"`에 `"Game"` 추가, `"includePlatforms": ["Editor"]`.

**규칙**: 내 코드는 전부 asmdef 안에. 서드파티(Assets 안, asmdef 없는 것)는 Assembly-CSharp로 컴파일되는데 asmdef 코드에서 Assembly-CSharp는 참조 불가 — 서드파티 코드를 직접 참조할 필요가 생겼을 때만 그 폴더에 asmdef를 얹는다(에셋 업데이트 시 덮어써질 수 있음에 주의).

## 기본 폰트

- **Gothic A1** (Google Fonts, OFL 라이선스) — 한글 지원, 굵기 9종이라 본문·제목 다 커버된다.
  `Assets/Fonts/`에 Regular / Bold / Black + `OFL.txt`(재배포 시 동봉 의무)를 받는다.
  받는 곳: `https://raw.githubusercontent.com/google/fonts/main/ofl/gothica1/<파일명>`
- TMP 폰트 에셋은 **Dynamic 모드**로 만든다. 한글은 글리프가 많아 Static으로 구우면 아틀라스가
  터지지만, Dynamic은 쓰는 글자만 채워 넣어 가볍다.
- 만든 Regular 에셋을 **TMP Settings의 Default Font Asset**으로 지정한다.
  `Assets/Editor/DefaultFontSetup.cs`가 에디터 첫 로드 때 자동으로 생성+지정한다
  (수동 재실행은 `Tools > Setup > 기본 폰트(Gothic A1) 설정`).
- TMP 필수 리소스가 없으면 스크립트가 안내만 하고 멈춘다 →
  `Window > TextMeshPro > Import TMP Essential Resources` 후 메뉴로 다시 실행.

## 프로젝트 설정

- Active Input Handling: Input System (New) — `ProjectSettings.asset`의 `activeInputHandler: 1`
- Sorting Layers: `Ground / GroundEffect / Shadow / Default / Actor / Effect / WorldUI` — `TagManager.asset`의 `m_SortingLayers`
- (2D) Transparency Sort Mode: Custom Axis (0,0,1) — `GraphicsSettings.asset`의 `m_TransparencySortMode: 2`, `m_TransparencySortAxis: (0,0,1)`
