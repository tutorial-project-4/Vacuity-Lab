---
title: "지형 시스템 현황"
document_version: "2.0"
status: "1차 구현 완료 (테스트 씬 통과)"
last_updated: "2026-07-22"
language: "ko-KR"
---

# 지형 시스템 현황

> 기획 협의 이력·상세 사양·테스트 케이스 전체는 `docs/terrain_system_handover_full_v1.1.md` 참고.
> 이 문서는 현재 구현 상태와 연동 규약만 담는다.

## 설계 원칙 (변경 금지)

1. 지형 타입의 진실의 원천은 **`TerrainDescriptor` 컴포넌트**다. 외형·에셋 이름·Tilemap 레이어로 판별하지 않는다.
2. 경계 벽(`BoundaryWall`)은 벽뚫대시를 포함한 모든 정상 이동보다 우선한다.
3. 위험 지형은 보스를 직접 참조하지 않는다. 연결은 `HazardChannel` 신호로만 한다.
4. **TBD 항목은 하드코딩 금지** — Inspector 설정값으로 열어두고 기획 확정 후 채운다.

## 1. 현재 상태

| 작업 | 상태 |
|---|---|
| 지형 타입 정의 (`TerrainKind` 9종) | ✅ |
| 일반 벽 / 벽뚫대시 벽 / 경계 벽 코드 구분 | ✅ |
| 위험 지형 상태 기계 (가시·전기장판·강화 전기장판) | ✅ |
| Trigger Channel (보스↔장판 느슨한 연결) | ✅ (송신부는 임시 디버그 패널) |
| 테스트 씬 + 플레이어 스폰 + 피해 연동 | ✅ 통과 (2026-07-22) |
| 배치용 프리팹 4종 (대시벽·가시·전기장판·강화장판) | ✅ `Tools > Terrain > Create Terrain Prefabs` → `Assets/Prefabs/Terrain/` |
| 디자이너 배치 가이드 | ✅ `terrain_placement_guide.md` |
| 벽뚫대시 통과 로직 | ⬜ 플레이어 대시 자체가 미구현 (연욱 담당과 연동 필요) |
| 보스 신호 송신부 | ⬜ 보스 담당 연동 필요 |
| 아트 적용 | ⬜ 아트 확정 후. 상태 연출은 `HazardBase.StateChanged` 이벤트에 어댑터를 구독시키면 됨 |

## 2. 지형 타입

| `TerrainKind` | 플레이어 | 보스 | Layer | 비고 |
|---|---|---|---|---|
| `Background` | 충돌 없음 | 충돌 없음 | - | Collider 없음 |
| `Floor` | 이동 가능 | 이동 가능 | `Solid`(3) | |
| `SolidWall` | 통과 불가 (대시 포함) | 통과 불가 | `Solid`(3) | |
| `DashPassableWall` | 벽뚫대시 중에만 통과 | 통과 불가 | `DashPassableWall`(11) | 대시 미구현 → 현재 일반 벽처럼 동작 |
| `BoundaryWall` | 절대 통과 불가 | 절대 통과 불가 | `Solid`(3) | 맵 외곽 4면 |
| `Platform` | **TBD** (임시 Solid) | 위에 설 수 있음 | `Solid`(3) | 정책은 Inspector `playerPlatformPolicy` |
| `Spike` | 피해 (Active 시) | TBD | `Hazard`(9) | 채널 트리거 |
| `ElectricFloor` | 피해 (Active 시) | TBD | `Hazard`(9) | 자체 주기, 보스 불필요 |
| `EnhancedElectricFloor` | 피해 (Active 시) | TBD | `Hazard`(9) | 보스 채널 트리거 |

## 3. 코드 구조

경로: `Assets/Scripts/Map/`

| Component | 역할 |
|---|---|
| `TerrainDescriptor` | 모든 지형에 부착. `terrainKind`, `targetMask`, `blocksProjectile`(TBD), `playerPlatformPolicy`(TBD). 충돌체에서 찾을 때는 `TerrainDescriptor.From(collider)` |
| `HazardBase` (abstract) | 위험 지형 공통: Inactive→Warning→Active→Cooldown, 상태별 색상, Active에서만 피해. 시간값 전부 TBD 임시값. 상태 변경 시 `StateChanged` 이벤트 발행(아트/사운드 연출 훅) |
| `AutoCycleHazard` | 일반 전기장판. `startDelay`/`phaseOffset`/`loop`로 자체 반복 |
| `ChannelTriggeredHazard` | 가시·강화 전기장판. `triggerChannel` 신호 수신 시 1회 사이클 |
| `HazardChannel` (static) | 신호 버스: `HazardChannel.Send("채널ID", activate)` |
| `HazardDebugTrigger` | 테스트용 수동 발사 버튼 (보스 송신부 대체) |

### 피해 전달 방식 (플레이어 담당 확인 필요)

플레이어 Rigidbody2D가 Kinematic(Full Kinematic Contacts 꺼짐)이라 **정적 트리거와 물리 이벤트가 발생하지 않는다.**
그래서 `HazardBase`가 Active 중 `Physics2D.OverlapBox` 겹침 검사로 `PlayerHealth.TakeDamage()`를 직접 호출한다.
피해량은 자식 `DamageTrigger`의 `PlayerDamageSource.Damage`(팀 규약)에서 읽는다. 무적·중복 피격은 `PlayerHealth` 책임.

### Trigger Channel

| Channel ID | 수신자 | 송신 |
|---|---|---|
| `LAB_SPIKE_A` | Spike_A | 보스 측: `HazardChannel.Send("LAB_SPIKE_A")` 한 줄. 현재는 디버그 패널 |
| `LAB_ELECTRIC_ENHANCED_A` | EnhancedElectricFloor_A | 동일 |

### 벽뚫대시 연동 지점 (미래 작업)

평상시 플레이어 `solidLayer` 마스크 = `Solid | DashPassableWall`.
대시 중에만 `DashPassableWall` 비트를 빼고, 접촉 지형이 `TerrainDescriptor.From()`으로 `DashPassableWall`인지 확인한다.
경계 밖 출구·출구 막힘이면 통과 거부.

## 4. 테스트 씬

- 생성: 메뉴 `Tools > Terrain > Build Sample Scene` (재실행 시 덮어씀)
- 씬: `Assets/Scenes/TerrainSampleScene.unity` — 스폰→벽→대시벽→발판→가시→전기장판→강화장판→경계 순회 구성
- 생성기: `Assets/Scripts/Map/Editor/TerrainSampleSceneBuilder.cs` (`DashPassableWall` Layer 자동 등록, 임시 스프라이트 `Assets/Art/WhiteSquare.png` 자동 생성)

## 5. 기획 확정 필요 (TBD)

높은 우선순위만. 전체 목록은 full 문서 18절.

1. 발판-플레이어 상호작용 (단방향 통과 여부 등) → `playerPlatformPolicy` 값으로 반영
2. 가시·전기장판이 보스에게 피해를 주는지 → `targetLayers`에 Boss 추가 여부
3. 피해량(현재 임시 1하트)·경고/활성/쿨다운 시간(현재 임시 1s/2s/1s)
4. 벽·발판의 공격/투사체 차단 여부 → `blocksProjectile`
5. 벽뚫대시 최대 벽 두께, 출구 막힘 처리
6. 강화 전기장판의 "강화" 요소가 무엇인지
