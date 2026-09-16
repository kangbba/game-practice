# 전투 HUD 디자인

남색 금속 패널, 얇은 샴페인 골드 프레임, 청록색 진행 게이지를 사용하는 전투 UI.

- 좌상단: 원형 초상화, HP/EXP, 아이콘을 붙인 재화, 성장 가이드.
- 상단 중앙: 스테이지와 처치 진행도. 우상단: 작은 잠금 메뉴 5개.
- 하단 중앙: 성장 / 장비 / 전투 / 편성 / 심연 장비.
- 우하단: 공격, 궁극기, 배속 표시. 좌하단: 보조 아이콘과 닉네임 안내.
- 조이스틱은 전체 화면을 사용하고 HUD는 `BattleHUDSafeArea`로 기기 안전 영역에 맞춘다.
- 장식은 입력을 차단하지 않는다. 공격·궁극기·장비 버튼 및 장비창의 기존 연결을 유지한다.
- 미구현 메뉴는 잠금 아이콘으로 표시한다. 재화·가이드·스테이지·배속 표시는 기존처럼 샘플 데이터다.

## 에셋

`UnityProject/GamePractice/Assets/Game/UI/Art/BattleHUD/`

| 폴더 | 내용 |
| --- | --- |
| Icons | 기존 DarkFantasy2D 아이콘 16종의 HUD 전용 사본 + 새 Ultimate.png |
| Frames | Panel(9-slice), Gauge, Medallion, Ring |
| Portraits | 기존 HeroPortrait의 HUD 전용 사본 |

기존 리소스는 경로와 GUID를 보존하고, HUD 사본에는 별도 GUID를 부여했다. PNG는 투명도, Sprite 임포트, mipmap 비활성화 설정을 사용한다. 프레임은 `BattleHUDArtBuilder`에서 재생성한다.

## 수정 및 재생성

- `★Sayne★ > 2. 전투 HUD 빌드`: 위젯 10종과 BattlePhaseUIPanel 프리팹 재생성.
- `★Sayne★ > UI > 전투 HUD 미리보기 저장`: 16:9, 와이드 및 장비창 프리뷰를 Unity 프로젝트의 `HUDPreviews/`에 저장. 열린 씬은 바꾸지 않는다.
- 배치 실행 진입점: `Sayne.Editor.BattleHUDPreview.BuildAndCapture`.
- 쿨타임 마스크와 아이콘은 버튼 RectTransform을 따라 늘어나므로 작은 궁극기 버튼에서도 영역이 일치한다.

## 검증

Unity 6000.3.2f1 별도 작업 복사본에서 컴파일, 프리팹 생성, 직렬화된 참조, 입력 대상 4개(조이스틱·공격·궁극기·장비), 버튼에 맞는 쿨타임 영역, 안전 영역 내 배치를 검증했다. 출력 프리팹 11개는 원본 프로젝트에 복사 후 해시 일치를 확인했고 기존 프리팹 GUID는 유지했다.

- [1920×1080 HUD](battle-hud-1920x1080.png)
- [2340×1080 HUD — 노치 여백 및 쿨타임 표시](battle-hud-2340x1080.png)
- [장비창](battle-hud-1920x1080-equipment.png)

이미지는 Unity에서 프리팹을 직접 렌더한 UI 전용 미리보기다. 실제 기기에서 전투 플레이를 실행한 검증은 포함하지 않는다.

## 생성 이미지 출처

Ultimate.png는 내장 image_gen 도구로 생성했다. 기존 이미지의 편집이 아닌 신규 이미지다.

프롬프트:

> Use case: stylized-concept. Asset type: single transparent game UI ability icon for a dark fantasy Korean action RPG. Primary request: one striking violet lightning bolt splitting into three jagged blades, with a small brilliant icy cyan core, bold black/navy outer silhouette, polished cel-shaded purple facets, tiny lavender edge highlights. Composition: centered isolated emblem filling 80 percent of square canvas, perfectly readable at 64 pixels. Style: premium hand-painted cartoon fantasy inventory icon, crisp graphic shape and strong contrast, matching gem and crescent-slash mobile RPG icons. Background: genuinely transparent alpha, no circular badge, no frame, no text, no watermark, no surrounding scenery or large glow.
