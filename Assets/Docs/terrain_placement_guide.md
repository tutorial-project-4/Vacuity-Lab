# 맵 배치 가이드 (디자이너용)

디자인한 맵에 지형 시스템을 입힐 때 지켜야 할 규칙입니다. 1페이지가 전부입니다.
개발 배경이 궁금하면 `terrain_system_handover.md` 참고.

## 핵심 규칙 딱 3개

1. **보이는 것과 충돌하는 것을 분리하세요.** 배경 그림에는 절대 콜라이더를 붙이지 않습니다.
2. **지형 종류별로 Tilemap을 따로 만드세요.** 한 Tilemap에 바닥과 벽을 섞어 그리면 안 됩니다.
3. **외형으로 지형 종류를 바꾸지 마세요.** 벽이 무엇인지는 그림이 아니라 붙어 있는
   `TerrainDescriptor` 컴포넌트가 결정합니다. 예쁜 벽 타일을 복사했다고 통과 규칙이 복사되지 않습니다.

## 씬 계층 구조

```text
ArenaRoot
├─ Background
│  └─ BackgroundTilemap        ← 콜라이더 없음, 마음껏 그리기
├─ Geometry
│  ├─ FloorTilemap             ← 바닥
│  ├─ SolidWallTilemap         ← 일반 벽
│  ├─ BoundaryWallTilemap      ← 맵 외곽 4면 (틈 없이!)
│  ├─ PlatformTilemap          ← 발판
│  └─ DashPassableWalls        ← 프리팹 배치
└─ Hazards                     ← 프리팹 배치
```

## Tilemap으로 그리는 것

각 Tilemap **GameObject 자체**에 아래 3개를 설정합니다 (타일 하나하나가 아님).

| Tilemap | Layer | 추가 컴포넌트 | TerrainDescriptor의 terrainKind |
|---|---|---|---|
| BackgroundTilemap | Default | 없음 (콜라이더 금지) | 안 붙여도 됨 |
| FloorTilemap | `Solid` | TilemapCollider2D + TerrainDescriptor | `Floor` |
| SolidWallTilemap | `Solid` | TilemapCollider2D + TerrainDescriptor | `SolidWall` |
| BoundaryWallTilemap | `Solid` | TilemapCollider2D + TerrainDescriptor | `BoundaryWall` |
| PlatformTilemap | `Solid` | TilemapCollider2D + TerrainDescriptor | `Platform` |

- 콜라이더 틈 방지: TilemapCollider2D에서 **Used By Composite** 체크 + 같은 오브젝트에
  **CompositeCollider2D** 추가 (자동으로 붙는 Rigidbody2D는 Body Type을 **Static**으로).
- 경계(BoundaryWall)는 맵 바깥 전체를 한 바퀴 둘러야 합니다. 카메라에 안 보이는 곳도 막으세요.

## 프리팹으로 놓는 것 (`Assets/Prefabs/Terrain/`)

씬에 드래그해서 놓고, 크기는 **Transform Scale**로 조절하면 콜라이더도 같이 맞춰집니다.

| 프리팹 | 역할 | 배치 시 할 일 |
|---|---|---|
| `DashPassableWall` | 벽뚫대시로만 통과하는 벽 | 놓기만 하면 됨. Layer 바꾸지 말 것 |
| `Spike` | 가시 (신호 받으면 발동) | 인스턴스마다 Inspector의 **Trigger Channel**을 고유 ID로 (예: `LAB_SPIKE_B`) |
| `ElectricFloor` | 전기장판 (자동 반복) | 여러 개 깔 때 **Phase Offset**으로 시차 주기 가능 |
| `EnhancedElectricFloor` | 강화 전기장판 (보스 신호) | Spike처럼 채널 ID를 고유하게 |

- 위험 지형은 바닥 **위에 겹쳐** 놓는 트리거입니다. 바닥 타일을 지우고 놓는 게 아닙니다.
- 지금은 단색 박스 그래픽입니다. 아트 교체는 개발 쪽에서 연결하니 위치·크기만 잡아 주세요.

## 하지 말 것

- 배경에 콜라이더 추가 ❌
- 프리팹/Tilemap의 **Layer 변경** ❌ (통과 판정이 깨짐)
- `TerrainDescriptor` 없는 콜라이더 지형 추가 ❌
- 벽 종류를 바꾸고 싶을 때 그림만 교체 ❌ → terrainKind를 같이 바꾸거나 개발 담당에게 요청

## 확인 방법

1. 메뉴 `Tools > Terrain > Build Sample Scene` → 동작 기준이 되는 샘플 씬을 볼 수 있음
2. 본인 맵 씬에서 Play → 화면 좌상단 버튼으로 가시/강화장판 수동 발동 테스트
   (버튼이 없으면 빈 오브젝트에 `HazardDebugTrigger` 컴포넌트를 붙이고 채널 ID 입력)
3. 이상하면 개발 담당(김도연)에게: 씬 이름 + 어느 오브젝트인지
