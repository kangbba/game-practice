# 문폴 — SD 다크 판타지 2D 에셋 세트

두 번째 참고 이미지의 깔끔한 SD 비율, 굵은 외곽선, 단순한 셀 음영, 남색 패널과 노란 버튼을 기준으로 제작한 버전입니다.

## 시작

1. `DarkFantasy2D.unitypackage`를 Unity 6000.3.7f1 프로젝트로 가져옵니다.
2. `Assets/DarkFantasy2D/Scenes/AssetShowcase.unity`를 열고 Play를 누릅니다.
3. 대기 / 이동 / 공격 / 피격 / 사망 / 부활 버튼으로 세 캐릭터를 확인합니다.
4. 오른쪽의 이펙트 버튼으로 파티클 8종을 재생합니다.
5. `성장` 버튼으로 훈련 UI를 열고, 훈련을 눌러 능력치·코인 변화와 닫기를 확인합니다.

현재 game-practice 프로젝트에는 동일한 `Assets/DarkFantasy2D` 폴더가 들어 있습니다.
다른 프로젝트에서는 패키지 가져오기와 `UnityAssets/DarkFantasy2D` 폴더 복사 중 하나를 사용하세요. `.meta` 파일을 함께 옮겨야 참조가 유지됩니다.

## 구성

- **캐릭터 프리팹 3개**: 은빛 사신(낫), 고블린 정찰병(단검), 오우거 전사(곤봉). 모두 기본 우측 방향.
- **리깅**: 머리·몸통·팔·팔뚝·다리·머리카락/장식·망토·무기를 분리한 Transform 본 계층의 2D 컷아웃 리그. 각 파츠는 SpriteRenderer에 연결됩니다. 가중치 메시 변형이나 Spine 스켈레톤 형식은 아닙니다.
- **애니메이션 15개**: 캐릭터마다 Idle, Walk, Attack, Hit, Death. Animator Controller 3개, 부활 전이, 공격 타격 프레임 이벤트 포함.
- **VFX 프리팹 8개**: Slash, HitSpark, Impact, DeathSmoke, FootstepDust, Heal, SkillCharge, Portal. 모두 실제 Unity ParticleSystem이며 2.8초 후 정리됩니다.
- **UI 프리팹 9개**: CombatHUD, GrowthPanel, PrimaryButton, SecondaryButton, CloseButton, ItemFrame, HealthBar, Toggle, Slider.
- **투명 PNG**: 캐릭터 파츠 27개, UI 아이콘 16개, 파츠 원본 아틀라스 4개, UI/파티클 기본 텍스처와 조립 영웅 초상화.
- **데모 씬**: 캐릭터·애니메이션·전투 HUD·성장 UI·이펙트 미리보기.

## 스크립트 연결

```csharp
using DarkFantasy2D;

CharacterRig rig = hero.GetComponent<CharacterRig>();
rig.SetMoving(true);
rig.Attack();
rig.TakeDamage(15);
rig.Heal(20);
rig.FaceRight(false);
rig.Revive();

// 실제 공격 판정은 게임 쪽에서 연결합니다.
rig.onStrike.AddListener(() => { /* 범위 검사와 대상 피해 처리 */ });
```

`CharacterRig`는 체력, 애니메이션, 이펙트 및 공격 이벤트를 제공합니다. 이동 경로, 적 AI, 충돌 기반 공격 판정은 게임 로직에서 연결합니다. Collider2D는 트리거 형태로 제공됩니다.

`CombatHUD`의 SetHealth / SetExperience / SetLevel / SetCurrency / SetStage / SetCooldown으로 표시를 변경할 수 있습니다. 상점·소환·던전·이벤트·채팅 메뉴 버튼에는 프로젝트의 화면 전환을 연결하세요.

`GrowthPanel`은 데모용 코인과 공격력/체력 레벨을 관리하며 훈련과 닫기를 제공합니다. 데모 성장 수치는 영웅 전투 능력치나 저장 시스템으로 자동 전달되지 않습니다. `onTrained` 이벤트에서 실제 게임 모델로 연결할 수 있습니다. 잠긴 탭과 레벨업은 잠금 상태를 보여주는 UI 예시입니다.

## 렌더링과 입력

- 캐릭터/VFX는 포함된 `DarkFantasy2D/UnlitTransparent` 머티리얼을 사용합니다.
- UI는 Unity UGUI를 사용합니다. Korean font: Nanum Gothic Bold, SIL Open Font License. 라이선스는 Documentation에 있습니다.
- 데모는 기존 Input Manager와 Input System을 조건부 지원합니다. Input System 프로젝트에서는 ShowcaseInputSetup이 기본 UI 입력 액션을 연결합니다.
- 재사용 UI는 1600×900 기준 CanvasScaler를 사용합니다. 다른 비율·모바일 Safe Area에 맞춘 배치는 적용할 게임에서 조정하세요.

## 파일

- `Preview.png`: 전체 디자인
- `Preview_Attack.png`: 공격 중간 자세
- `Preview_Growth.png`: 성장 패널
- `SourceArt/`: 최종 원본 아틀라스
- `UnityAssets/DarkFantasy2D/`: 메타 포함 Unity 에셋 폴더
- `Documentation/Validation.txt`: 에셋 검증 결과
- `Documentation/RuntimeValidation.txt`: Play Mode 검증 결과
- `Documentation/FinalGenerationPrompts.json`: 최종 이미지 생성 프롬프트 (built-in image_gen 사용)

에디터 메뉴 `Tools > Dark Fantasy 2D > Rebuild Asset Pack`은 이 팩의 에셋을 다시 생성합니다. 수정한 프리팹/클립을 덮어쓸 수 있으므로 커스텀 작업은 복제본에서 하세요. 기본 출력은 바탕화면 DarkFantasy2D_AssetSet이며, DARKFANTASY_EXPORT_DIR 환경 변수로 바꿀 수 있습니다.
