# 맵 구성 계획 — Region 1 ~ 3

> 기준: WorldScene 2 → MainRegion → Region_01 프리팹의 구조를 그대로 따름.
> 생성 파일: `Region_01_AddRooms.prefab`, `Region_02.prefab`, `Region_03.prefab` (Ground 블록아웃 + 기믹 배치 완료, Background/Forground 그림은 비어 있음)

---

## 1. 파악한 규칙 (Region_01 / Region_Test01 기준)

| 항목 | 규칙 |
|---|---|
| 청크 | 32 × 32 유닛. 청크 인덱스 = `floor((위치 + 16) / 32)` → 청크 (cx, cy)의 중심은 월드 (32cx, 32cy), 범위는 [32cx − 16, 32cx + 16) |
| 지역(Region) | `Region_xx` 루트: Grid(셀 1) + `Region` 컴포넌트(`_regionId`, `_neighborRegionIds`). Addressable 주소 = 지역 id. 모든 지역이 같은 월드 좌표를 공유하므로 **청크 인덱스가 지역끼리 겹치면 안 됨** |
| 방(Room) | `Room_Axx` (위치 0,0) + `Room` 컴포넌트(`_roomId`), 청크 1~4개 묶음. 자식으로 `Tile cx, cy` |
| 타일(청크) | `Tile cx, cy` 는 월드 (32cx, 32cy)에 두고, 자식 `Background(300) / Ground(500, TilemapCollider+Composite) / Forground(700) / Object / Gimmicks`. 타일맵 원점 (−16, −16), 크기 32 × 32. `MapChunk.Transform_Object` → Object (청크 활성/비활성 대상), Gimmicks는 항상 켜짐 |
| 기믹 | Tile의 `Gimmicks` 자식 아래에 프리팹 인스턴스로 둠 (Region_01의 Gimmick_LightZone 방식). 신호 연결은 프리팹 오버라이드로 저장됨 |
| 방 사이 | 같은 방의 청크끼리는 벽 없음, 방 경계는 2칸 두께 벽. 통로는 가로 출입구 5칸 높이, 세로 출입구 4칸 너비 |

생성 시 적용한 블록아웃 규칙: 바닥/벽 2칸 두께, 같은 방 안에서 위·아래 청크가 만나면 필요한 곳에만 2칸 바닥판(구멍 포함), 발판은 1칸 두께 6~8칸 너비, 점프 간격은 Region 2 = 3칸(기본 점프), Region 3 = 5칸(토끼 2단 점프) 가정.

---

## 2. 월드 청크 배치

```
      x: 0   1   2   3   4   5   6   7 |  10  11  12  13  14  15  16  17  18  19 |  22  23  24  25  26  27  28  29  30  31
y3                                     | B01 B02 B02 B03 B08 B08 B10 B10  .   .  | C03 C03 C04 C04  .  C08 C08 C09 C10 C10
y2                                     | B04 B04 B03 B03 B07 B09 B09 B10 B11 B11 | C02 C03 C04 C05 C05 C07 C08 C09 C10 C11
y1   .  A02 A02 A04 A04 A05 A05  .     | B04 B05 B05 B06 B07 B09  .  B11 B11 B12 | C02  .   .  C05 C05 C07  .  C09  .  C11
y0  A01 A02 A02 A03 A03  .  A05 A06    |  .  B05 B05 B06  .   .   .   .   .   .  | C01 C01  .  C06 C06 C06  .   .  C12 C11
         Region_01 (기존 + 추가)        |              Region_02 (쥐)             |              Region_03 (토끼)
```

* Region_01 ↔ Region_02, Region_02 ↔ Region_03 은 **텔레포터**로 연결 (물리적으로 떨어져 있어도 됨). `_neighborRegionIds`: Region_01 → [Region_02], Region_02 → [Region_01, Region_03], Region_03 → [Region_02]
* 테스트 지역(Region_Test01~03, 청크 x 5·8)과 겹치지 않도록 Region_02 는 x 10부터 시작

---

## 3. Region 1 — 튜토리얼 (6방)

| 방 | 청크 | 모양 | 목적 | 기믹 구성 | 상태 |
|---|---|---|---|---|---|
| A01 | (0,0) | 1칸 | 시작 지점 | 스폰 | 기존 |
| A02 | (1,0)(2,0)(1,1)(2,1) | 2×2 | 빛이 장애물임을 인지 | BlinkingLight ×3, Switch → Door | 기존 |
| A03 | (3,0)(4,0) | 가로 2 | 가시 장애물 | Door + 가시 | 기존 |
| A04 | (3,1)(4,1) | 가로 2 | 물체 밀기 | Grabbable 상자 | 기존 |
| **A05** | (5,1)(6,1)(6,0) | ㄱ자 3 | 종합 검증 | ① (5,1) 깜빡이 3개(시작 지연 0/1/2초) 통로 → ② (6,1) 스위치(1회용, 시작 꺼짐) → 문 → ③ 바닥 구멍(24~28)으로 (6,0) 낙하 → ④ 상자(Marker_GrabBox)를 발판 위에 → 발판(Invert Signal) → 출구를 막은 LightZone 꺼짐 | **생성** |
| **A06** | (7,0) | 1칸 | 텔레포터 | Marker_Teleporter_To_Region_02, Marker_Sign | **생성** |

> A05/A06 은 `Region_01_AddRooms.prefab` 로 따로 만들었습니다. Region_01 프리팹 모드에서 이 프리팹을 끌어다 놓고 **Unpack Completely** 한 뒤 `Room_A05`, `Room_A06` 을 Region_01 루트 바로 아래로 옮기고 남은 빈 루트는 지우면 됩니다. A04 (4,1)의 오른쪽 벽(바닥 위 5칸)은 Region_01 쪽 타일맵이라 직접 뚫어야 합니다.

---

## 4. Region 2 — 쥐 변신 구역 (12방)

| 방 | 청크 | 모양 | 목적 | 기믹 구성 / 신호 |
|---|---|---|---|---|
| B01 | (10,3) | 1 | 쥐 변신 획득 | 텔레포터 도착 마커, 받침대(계단 2단) 위 `Marker_ShadowPickup_Mouse` |
| B02 | (11,3)(12,3) | 가로 2 | 쥐 기믹 1 — 배관 기본 | 천장까지 막힌 벽, 벽 양쪽에 Pipe In/Out (경로: 위로 7 → 가로 13), 힌트 표지판 마커 |
| B03 | (13,3)(13,2)(12,2) | ㄴ자 3 | 쥐 기믹 2 — 빛 아래 배관 통과 | (13,3) 바닥 구멍(24~28)으로 낙하 → (13,2) 바닥을 18×5 LightZone 이 막음 → 오른쪽 Pipe 로 천장 쪽 경로를 타고 (12,2) 로 → (12,2) Switch → LightZone 끄기(복귀용). 오른쪽 계단은 되돌아가는 길 |
| B04 | (11,2)(10,2)(10,1) | ㄱ자 3 | 쥐 기믹 3 — 배관으로만 가는 스위치 방 | (11,2) 오른쪽 아래 밀실(8×6) 안에 Switch(1회용, 시작 꺼짐). 밀실은 Pipe 로만 진입 → Switch → (10,1) 왼쪽 포켓을 막는 Door 열림. (10,2) 바닥 구멍(2~6)으로 (10,1) 낙하, (10,1) 통로에 BlinkingLight 1개 |
| B05 | (11,1)(12,1)(11,0)(12,0) | 2×2 | 종합 검증 1 | 위층: 깜빡이 3개(0/1/2초) 통로 → (12,1) 바닥 구멍(10~14) 낙하 → 아래층: `Marker_GrabBox` 상자를 깜빡이(2초/2초) 사이로 밀어 PressurePlate 위에 → 출구 Door 열림. (11,0)↔(11,1) 복귀용 Pipe |
| B06 | (13,1)(13,0) | 세로 2 | 텔레포터 허브 1 | `Marker_Teleporter_Hub_1`, 지그재그 계단(3칸 간격)으로 위층, (13,1) 바닥 구멍(2~8) |
| B07 | (14,1)(14,2) | 세로 2 | 기믹 1 — 이동 광원 | MovingLight(레일 lx 24 → 8, 시작 = 계단 위) 가 오른쪽 계단을 비춤. 입구 Switch(시작 꺼짐) → 신호 켜짐 → 광원이 레일 끝(입구 쪽)으로 이동 → 계단 통과. 다시 누르면 돌아옴. (14,2) 계단 → 천장 구멍(14~18) |
| B08 | (14,3)(15,3) | 가로 2 | 기믹 2 — 회전 광원 | RotatingLight(Continuous, 시작부터 회전, 60°/s) 가 방을 쓸고 지나감, 기둥 2개 뒤에서 대기. 끝의 Switch → 회전 정지(복귀용). (15,3) 바닥 구멍(22~26)으로 B09 |
| B09 | (15,2)(16,2)(15,1) | ㄴ자 3 | 기믹 3 — 거울 빔 + 고정 발판 | 천장 RayCast 빔(아래 방향, 폭 3)이 바닥의 Mirror(−45°)에 꺾여 **오른쪽 통로**를 막음. (15,2) 바닥 구멍(26~30) → (15,1) 포켓의 PressurePlate(고정, Invert) → 빔 꺼짐. 포켓에 `Marker_Collectible`. (16,2) 계단 → 천장 구멍(14~18) |
| B10 | (16,3)(17,3)(17,2) | ㄱ자 3 | 기믹 4 — 상자 발판 + 깜빡이 | (16,3) 상자를 깜빡이 2개(0/1.5초) 사이로 밀어 PressurePlate 위에 → (17,3) 벽의 Door 열림 → 바닥 구멍(20~24)으로 (17,2) 낙하 → 바닥 구멍(14~18)으로 B11 |
| B11 | (18,2)(19,2)(18,1)(17,1) | ㄴ+ㄱ 4 | 종합 검증 2 | (17,1) 완전히 막힌 벽 → Pipe 로 (18,1) → 상자를 깜빡이 2개 사이로 밀어 PressurePlate → (19,2) 벽의 Door 열림 → 계단으로 (18,2) (구멍 26~30) → (18,2) Switch → (19,2) 천장 RayCast 빔 끄기 → Door 통과 → (19,2) 바닥 구멍(14~18)으로 B12 |
| B12 | (19,1) | 1 | 텔레포터 허브 2 | `Marker_Teleporter_To_Region_03`, 표지판 마커 |

---

## 5. Region 3 — 토끼 변신 구역 (12방, 2단 점프 5칸 기준)

| 방 | 청크 | 모양 | 목적 | 기믹 구성 / 신호 |
|---|---|---|---|---|
| C01 | (22,0)(23,0) | 가로 2 | 토끼 변신 획득 | 텔레포터 도착 마커, 받침대 위 `Marker_ShadowPickup_Rabbit`. (22,0) 5칸 간격 계단 → 천장 구멍(6~10)으로 첫 2단 점프 연습 |
| C02 | (22,1)(22,2) | 세로 2 | 토끼 기믹 1 — 높은 점프 통로 | 5칸 간격 지그재그 발판, 중간에 가로로 긴 BlinkingLight 2개(12×3, 2초/2초, 지연 0/2초) → 꺼졌을 때 통과. 천장 구멍(24~28) |
| C03 | (22,3)(23,3)(23,2) | ㄱ자 3 | 토끼 기믹 2 — 빛 벽 넘기 + 회전 광원 | (23,3) 바닥에서 10칸 높이 LightZone 벽(6×8), 양옆 5칸 발판에서 2단 점프로 넘기 → 바닥 구멍(22~26) 낙하 → (23,2) RotatingLight(Continuous 50°/s) + 기둥 2개 → 오른쪽 출구 |
| C04 | (24,3)(25,3)(24,2) | ㄴ자 3 | 토끼 기믹 3 — 상자 발판 + 높은 계단 | (24,2) 상자를 PressurePlate 위에 → (25,3) 왼쪽 벽 Door 열림. 5칸 계단으로 (24,3) 구멍(4~8) → Door → (25,3) 깜빡이 2개 → 바닥 구멍(14~18)으로 C05 |
| C05 | (25,2)(26,2)(25,1)(26,1) | 2×2 | 종합 검증 1 | 위층: Switch(시작 꺼짐) → MovingLight(레일 lx 6 → 24) 가 (26,2) 입구에서 반대쪽으로 → (26,2) 끝 PressurePlate(고정) → 아래층 (25,1) Door 열림 → (25,2) 구멍(24~28) 낙하 → Door 통과 → (25,1) 구멍(6~10)으로 C06. (26,1) 은 LightZone 으로 막힌 보너스 포켓(`Marker_Collectible`) |
| C06 | (25,0)(26,0)(27,0) | 가로 3 | 텔레포터 허브 1 | `Marker_Teleporter_Hub_1`, (27,0) 5칸 계단 → 천장 구멍(6~10) |
| C07 | (27,1)(27,2) | 세로 2 | 복합 1 — 배관(쥐) + 점프(토끼) | (27,1) 가운데 벽을 Pipe 로 통과(쥐) → 오른쪽 5칸 계단(토끼)으로 (27,2) → 천장 구멍(24~28). 형태 전환 연습 |
| C08 | (27,3)(28,3)(28,2) | ㄱ자 3 | 복합 2 — 거울 빔 + 상자 + 스위치 | (27,3) 왼쪽 벽 RayCast 가로 빔(높이 3, 폭 2)이 Mirror(−45°)에서 바닥으로 꺾임 → 빔 왼쪽의 Switch(1회용) 에 가려면 (28,3) 상자를 PressurePlate(Invert) 에 올려 빔을 꺼야 함 → Switch → (28,3) 벽 Door 열림 → 바닥 구멍(26~30) → (28,2) 깜빡이 2개 → 오른쪽 출구 |
| C09 | (29,3)(29,2)(29,1) | 세로 3 타워 | 복합 3 — 회전 광원 타워 | (29,2) 중앙 RotatingLight(45°/s) 가 타워를 쓸고 지나감, 5칸 지그재그 발판으로 (29,3) 까지 → 오른쪽 출구(높이 4~9). (29,2) 바닥 구멍(4~8) 아래 (29,1) 은 보너스 포켓(`Marker_Collectible`) |
| C10 | (30,3)(31,3)(30,2) | ㄱ자 3 | 복합 4 — 이동 광원 + 상자 발판 | (31,3) 입구를 MovingLight(레일 lx 6 → 24, 느림) 가 막음. (30,3) 상자를 PressurePlate 에 올리면(유지) 광원이 반대편으로 → (31,3) 통과 → 바닥 구멍(14~18)으로 C11. (30,3) 구멍(26~30) 아래 (30,2) 보너스 포켓 |
| C11 | (31,2)(31,1)(31,0) | 세로 3 타워(하강) | 최종 종합 검증 | (31,2) 깜빡이 2개 → 오른쪽 끝 Switch(1회용) → 왼쪽 벽 Door → 구멍(4~8) 낙하 → (31,1) 천장 RayCast 빔(폭 3)을 왼쪽 PressurePlate(고정, Invert) 로 끄기 → 구멍(24~28) 낙하 → (31,0) RotatingLight(60°/s) → 왼쪽 출구 |
| C12 | (30,0) | 1 | 텔레포터 / 엔딩 | `Marker_Teleporter_Hub_2`, `Marker_Ending` |

---

## 6. 적용 순서

1. 파일 복사 (이미 커밋됨)
   * `Assets/Prefabs/Tile/Region 0/Region_02.prefab`, `Region_03.prefab`, `Region_01_AddRooms.prefab`
   * `Assets/Scripts/Editor/TilemapRefreshTool.cs`
2. Unity 가 임포트한 뒤 각 프리팹을 **프리팹 모드로 열고** 루트 선택 → 메뉴 `Tools > Map > Refresh Tilemaps In Selection` → 저장. (생성된 Ground 타일은 RuleTile_Mossy 의 기본 스프라이트 하나로 저장되어 있어 한 번 새로 고쳐야 이웃에 맞는 모양이 됩니다. Composite Collider 도 이때 다시 생성됩니다)
3. Addressables Groups 에서 `Region_02.prefab`, `Region_03.prefab` 을 Addressable 로 등록하고 주소를 각각 `Region_02`, `Region_03` 으로 (Region_01 과 같은 방식).
4. Region_01 프리팹 모드에서 `Region_01_AddRooms` 를 끌어 넣고 Unpack Completely → `Room_A05`, `Room_A06` 을 Region_01 루트 아래로 이동, 빈 루트 삭제. A04 (4,1) 오른쪽 벽 뚫기.
5. 마커(빈 오브젝트, Object 자식) 교체
   * `Marker_GrabBox` → 팀원의 Grabbable 상자 프리팹
   * `Marker_ShadowPickup_Mouse / _Rabbit` → 변신 획득 오브젝트 (플레이어 담당)
   * `Marker_Teleporter_*` → 텔레포터 (지역 전환은 `MapManager.EnterRegionAsync(regionId)` 로 가능)
   * `Marker_Collectible`, `Marker_Sign_*`, `Marker_Ending` → 수집품 / 표지판 / 엔딩 트리거
6. 테스트: WorldScene 2 의 MapManager `_startRegionId` 를 `Region_02` 로 바꾸고 플레이어를 (320, 82) 근처(B01)에 두면 Region 2 부터 바로 확인 가능. Region 3 은 `Region_03`, 플레이어 (704, −14) 근처(C01).

---

## 7. 확인·조정이 필요한 항목

* **점프 높이 가정**: Region 2 발판 간격 3칸, Region 3 는 5칸(2단 점프)으로 만들었습니다. 실제 플레이어 점프 높이에 맞춰 `steps` 간격을 조정해야 할 수 있습니다. 모든 발판은 Ground 타일이라 Tile Palette 로 바로 고칠 수 있습니다.
* **빛 피해 처리는 보류 중** (플레이어 담당). 지금은 LightZone 에 닿아도 아무 일이 없으므로 통과 여부만 눈으로 확인.
* **MovingLight 의 GrabArea** 콜라이더가 프리팹에서 `Is Trigger` 가 꺼져 있습니다 (플레이어와 부딪힘). 프리팹에서 켜 주세요. 이 계획에서는 이동 광원을 모두 신호(스위치/발판)로 움직이게 해서 손으로 잡는 방은 없습니다.
* **Pipe 경로 점**: 경로는 Pipe In 쪽에만 넣었고(Out 은 거꾸로 사용), 경로 점이 Ground 타일 뒤로 지나가면 불빛이 가려질 수 있습니다. Blip 의 Sorting Order 를 Ground(500) 보다 크게 하거나 경로 점을 통로 안으로 옮기세요.
* **Mirror 각도**: B09 는 위에서 내려오는 빔을 오른쪽으로, C08 은 왼쪽에서 오는 빔을 바닥으로 꺾도록 −45° 로 두었습니다. `_localNormal` 이 (0,1) 인 상태를 기준으로 했으니, 씬에서 빔이 다른 방향으로 꺾이면 각도만 ±90° 돌려 보세요.
* **RotatingLight** 의 빛 모양(Pivot 아래 Gimmick_LightZone 크기)은 프리팹 기본값 그대로입니다. 방 크기에 맞게 길이를 늘려 주세요.
* **PressurePlate `_isInvertSignal`** 을 쓴 곳: A05, B09, C08, C11 (밟으면 빛 꺼짐).
* Region_01 의 `_neighborRegionIds` 에 `Region_02` 가 들어 있는지 확인.
* 방 모양이 단조로우면 Background/Forground 타일과 발판 배치로 변화를 주면 됩니다. 청크 좌표·방 id 만 유지하면 됩니다.

---

## 8. Mossy 데코레이션 (Background / Forground)

`ThirdParty/Maaott_Tile_n_Sprite/Mossy Tileset` 의 슬라이스된 스프라이트를 각 Tile 의 `Background`, `Forground` 오브젝트 아래에 SpriteRenderer 오브젝트로 배치했습니다. (장식 스프라이트가 격자 타일이 아니라 불규칙한 크기라 타일맵에 찍지 않고 오브젝트로 두었습니다. Background/Forground 타일맵 자체는 비어 있으니 필요하면 그 위에 더 찍어도 됩니다)

| 위치 | 사용한 시트 / 스프라이트 | 부모 | Sorting Order | 비고 |
|---|---|---|---|---|
| 배경 실루엣 (기둥·언덕·덤불·모서리) | BackgroundDecoration_0~5, MossyHills_2·6·8 | Background | 280 / 290 | Ground(500) 뒤. 색을 0.42~0.55 로 어둡게 틴트 |
| 바닥 잡초·덤불·바위 | Decorations&Hazards_9~17·22~34 (풀), MossyHills_0·5·7·10 (덤불), Decorations&Hazards_0~2 (바위) | Forground | 510 | 플레이어 뒤에 두려면 플레이어 Sorting Order 를 510 보다 크게 |
| 천장 매달린 식물 | Hanging Plants_0~6 (늘어진 잎), _7~9 (말린 잎) | Forground | 705 | Forground(700) 앞 |
| 벽 덩굴 | Decorations&Hazards_18~21 | Forground | 705 | 오른쪽 벽은 FlipX |

* 스프라이트 PPU 가 100 이라(타일셋은 512) 오브젝트 스케일을 0.45 (배경 실루엣 0.6~0.9) 로 줄였습니다. 너무 작거나 크면 같은 이름의 오브젝트를 전체 선택해서 스케일만 바꾸면 됩니다.
* 기믹 주변 2.5칸 안에는 장식을 두지 않았고, 매달린 식물은 빈 공간이 충분할 때만 넣었습니다.
* 가시 덩굴(Decorations&Hazards_3~8)은 장식으로 쓰지 않고 남겨 두었습니다. A03 의 가시 장애물 그림으로 쓰기 좋습니다.
* 장식 오브젝트는 Object 가 아니라 Background/Forground 아래라 청크 비활성화의 영향을 받지 않습니다. 성능이 신경 쓰이면 두 오브젝트를 `MapChunk.Transform_Object` 아래로 옮기거나 Sprite Atlas 로 묶으면 됩니다.
* `Docs/Map/Region_02_preview.jpg`, `Region_03_preview.jpg` 가 장식까지 합성한 미리보기입니다.

---

## 9. Region 1 동굴 데코레이션 (Assets 1024 Cave)

Region_01 프리팹(A01~A06, 13 타일)의 Ground 타일맵을 읽어서 `ThirdParty/Maaott_Tile_n_Sprite/Assets 1024 Cave` 스프라이트를 자동 배치했습니다. 기존 오브젝트(기믹, 마커, 직접 놓은 `Cave - Platforms_*`)는 그대로 두고 `Deco_*` 오브젝트만 추가했습니다.

| 위치 | 스프라이트 | 부모 | Sorting Order | 스케일 |
|---|---|---|---|---|
| 배경 암벽 (빈 공간을 겹쳐 덮음) | Platforms_0·2·3·5·9·10·12·14 | Background | 305~308 | 1.1~1.6, 틴트 0.58~0.74 |
| 배경 바위 언덕 / 첨탑 | BigRocks1_0·1·2·3·5·6~9, Floor_19·21·22·24·25 | Background | 312 | 0.6, 틴트 0.6 |
| 바닥 턱 | Floor_0·1·2·5·6·12~15 | Forground | 505 | 0.8 |
| 바닥 돌·바위·석순 | SmallRocks (작은 돌 0~13·26~30, 바위 14·16~20, 석순 31·33·35·41), RockCombinations1 (돌무더기 1·3·5·7·8·9, 석순 군집 13~16) | Forground | 510 | 0.65 / 0.5 |
| 종유석 | SmallRocks_7·8·21·23·24, RockCombinations1_2 (천장 군집) | Forground | 705 | 0.65 / 0.5 |

* Background 가 비어 있던 A05/A06 타일(5,1 / 6,1 / 6,0 / 7,0)은 다른 타일과 같은 타일로 32×32 를 채워 두었습니다.
* 배경 암벽은 Background 타일맵(300) 앞·Ground(500) 뒤에 있어서, 바닥 타일이 암벽을 자연스럽게 가립니다. 더 어둡게/밝게 하려면 `Deco_Platforms_*` 를 전체 선택해 Color 만 바꾸면 됩니다.
* 기믹·마커 주변 2.5칸은 비워 두었고, 석순은 위로 4칸, 종유석은 아래로 3칸 이상 빈 곳에만 넣었습니다.
* 미리보기: `Docs/Map/Region_01_cave_preview.jpg`
