# Unity 공용 셋업 템플릿

새 Unity 프로젝트에 공용 셋업(SETUP.md 내용)을 한 번에 적용한다.

## 사용법

Unity Hub로 새 프로젝트를 만든 뒤 **에디터를 닫고**, 복사할 것 없이 여기서 바로 실행:

```sh
python3 ~/Documents/GitHub/game-practice/unity-template/setup.py <유니티_프로젝트_경로>
```

`<유니티_프로젝트_경로>`는 `Assets` / `Packages` / `ProjectSettings`가 있는 폴더.
템플릿 파일은 스크립트 위치 기준으로 찾으므로 어디서 실행해도 된다.

대상 프로젝트를 연 에디터가 떠 있으면 스크립트가 중단한다 — 에디터가 ProjectSettings를
메모리에 들고 있다가 종료 시 덮어써서 Sorting Layers 등이 날아가기 때문(`--force`로 무시 가능).

## 적용 내용

- `.gitignore` / `.gitattributes` — git 리포 루트에 배치 (유니티용)
- manifest.json에 git 패키지 추가: UniTask, R3.Unity, DOTween(미러), NuGetForUnity
- manifest.json에서 IAP·Ads·Analytics 제거 (Unity Cloud 미연결 시 에러 원인)
- R3 코어 dll을 NuGet에서 직접 받아 `Assets/Packages/`에 배치 + `Assets/packages.config` 생성
  (NuGetForUnity 자동 복원은 R3.Unity 컴파일 에러가 먼저 나면 막히는 닭-달걀 문제가 있어 스크립트가 직접 처리)
- `Assets/Scripts/Game.asmdef` + `Assets/Scripts/Editor/Game.Editor.asmdef`
- 기본 폰트 Gothic A1(한글, OFL) 다운로드 + TMP 기본 폰트로 자동 지정하는 에디터 스크립트 배치
- Active Input Handling → Input System (New)
- Transparency Sort Mode → Custom Axis (0,0,1)
- Sorting Layers → Ground / GroundEffect / Shadow / Default / Actor / Effect / WorldUI

재실행해도 안전하다(이미 적용된 항목은 skip). 적용 후 에디터를 열면 패키지 설치와
NuGet 복원이 자동으로 진행되고, DOTween만 Utility Panel에서 Setup 한 번 눌러주면 된다.
