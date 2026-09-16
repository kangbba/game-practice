# Kage — 가면 검사

- 프리팹: `Assets/Resources/Heroes/Kage/Kage.prefab`
- 런타임 생성: `HeroManager.SpawnHero(HeroID.Kage, position)`
- 현재 시작 영웅은 Kage. 방향키 이동, Space 공격은 기존 입력 그대로 사용한다.
- 애니메이션 확인: `Tools > Dark Fantasy > Kage > Preview Animation`. 클립 선택, 시간 슬라이더, 좌우 반전을 지원한다.
- 재생성: `Tools > Dark Fantasy > Kage > Rebuild Assets`. 기존 Kage 에셋을 갱신하므로 수동으로 수정한 클립은 먼저 보관한다.
- 참조 검사: `Tools > Dark Fantasy > Kage > Validate Assets`.

## 리그

`Graphic/Root/Torso/Arm/Forearm/Weapon` 순서로 어깨, 팔꿈치, 쥐는 손, 검 손잡이가 연결된다. 뼈와 Skin의 스케일은 모두 1이며, 이미지 자체의 커스텀 피벗과 pixels-per-unit으로 맞춘다. 목 피벗은 머리 아래 목깃과 몸통 목깃이 겹치는 위치다. 반대 팔도 동일한 관절 길이를 사용한다. 원본 시트의 칸 경계가 실제 그림과 일치하지 않아 균등 분할 대신 파츠별 영역을 지정했다. 영역과 관절 좌표는 `KageBuilder.ImportParts`가 원본이다.

## 모션

Idle / Walk / Attack / Hit / Death / Ultimate 전용 클립과 상체 AvatarMask를 사용한다. 공격 0.50초: 준비 0–0.075초, 베기 0.075–0.155초, 여운 0.155–0.21초, 복귀 0.21–0.50초. 몸통 회전과 고개 보정, 다리 벌림, 스카프 지연 회전을 함께 적용한다. 이동 중에는 상체만 공격한다.

`KageGraphic`만 공격 시작점을 0으로 사용한다. 기존 캐릭터의 40% 시작점은 유지한다. Kage 임시 공격 간격은 0.56초로, Space를 누르고 있어도 회수 전에 모션이 반복해서 끊기지 않는다. 공격 사거리는 2이다. 실제 피해는 기존 `HeroInputManager` 방식대로 입력 시점에 적용되며, 검 접촉 시점의 애니메이션 이벤트 판정은 이번 작업 범위에 포함하지 않았다.

원본 그림은 내장 image_gen으로 생성했다. 생성 명세는 `ART_SOURCE.md`에 기록했다. Unity 재생성 작업에서 출력하는 6개 공격 자세 이미지는 `Temp/KageReview/Attack.png`에 저장된다.
