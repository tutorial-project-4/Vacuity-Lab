# 보스 씬 통합 명세서

> 기준 구현: `Assets/Prefabs/Monster/Boss/Boss.prefab`, `Assets/Behavior/BossBrain.asset`  
> 기준 문서: `BOSS_PLAN.md`, `BOSS_PLAN_DETAIL.md`  
> 적용 대상 예시: `Assets/Scenes/arena_boss_test.unity`

## 1. 목적

보스는 프리팹만 씬에 배치해서는 모든 기능이 동작하지 않는다. 플레이어, 씬 전용 기믹, UI를 배치하고 보스 컴포넌트와 Behavior Blackboard에 참조를 연결해야 한다.

이 문서는 보스가 들어가는 씬이 만족해야 하는 **씬 계약(Scene Contract)** 을 정의한다. 아래에서 `필수`가 누락되면 오류가 발생하거나 전투 진행이 중단될 수 있고, `기능 필수`가 누락되면 해당 패턴이 생략되거나 대체 동작으로 실행된다.

## 2. 가장 빠른 적용 순서

1. `Assets/Prefabs/Monster/Boss/Boss.prefab`을 씬에 배치한다.
2. 플레이어 오브젝트를 씬에 배치한다.
3. 일반 전기 바닥, 강화 전기, 가시벽, 빔, 보스 게이지를 생성한다.
4. 보스 루트의 `Boss.target`에 플레이어 Transform을 할당한다.
5. `BehaviorGraphAgent`의 노출 변수에 `ElectricFloor`와 `BossBeam`을 할당한다.
6. `Boss.spikeWalls`, `Boss.enhancedElectric`과 게이지 참조를 확인한다.
7. 레이어와 물리 충돌 설정을 확인한 뒤 아래 QA를 수행한다.

> 주의: 현재 `Tools/Boss/Setup ...` 에디터 메뉴들은 생성 대상 씬이 `Assets/Scenes/Boss_test_Scene.unity`로 하드코딩되어 있다. `arena_boss_test.unity`를 열어 놓고 실행해도 테스트 씬을 열고 수정한다. 다른 씬에서는 오브젝트를 복사하여 좌표를 맞추거나, 셋업 스크립트의 대상 씬/좌표를 해당 맵에 맞게 수정한 뒤 사용해야 한다.

## 3. 씬 계약 요약

| 구분 | 필요 요소 | 중요도 | 연결 위치 | 누락 시 증상 |
| --- | --- | --- | --- | --- |
| 보스 | `Boss.prefab` 인스턴스 | 필수 | 씬 루트 | 보스 없음 |
| 플레이어 | `PlayerHealth`, `PlayerController`, `PlayerMovement`를 가진 플레이어 | 필수 | `Boss.target` | 추적 안 됨, 시작 시 `target 미할당` 경고, 사망/컷신 연동 안 됨 |
| 행동 그래프 | `BossBrain.asset` | 필수 | `BehaviorGraphAgent` | 보스 행동 전체 미실행 |
| 일반 전기 | `ElectricFloorScheduler` + 5개 `HazardBase` | 필수 | Blackboard `ElectricFloor` | 학습 패턴/1페이즈 전기 미동작 |
| 빔 | `BossBeam` | 2페이즈 기능 필수 | Blackboard `Beam` | 빔 행동 실패 또는 빔 미출력 |
| 강화 전기 | `EnhancedElectric` | 2페이즈 기능 필수 | `Boss.enhancedElectric` | 2페이즈에 일반 전기를 대신 재사용 |
| 가시벽 | `SpikeWalls` | 2페이즈 기능 필수 | `Boss.spikeWalls` | 페이즈 전환 후 가시벽 없음 |
| 체력 UI | `BossHealthGauge` + Fill 2개 | 표시 필수 | `boss`, `fill`, `fillTop` | 체력 게이지가 없거나 갱신 안 됨 |
| 충돌 레이어 | `Boss`, `Solid`, `Hazard` | 필수 | 오브젝트/LayerMask | 플레이어 공격 미적중, 돌진이 벽을 통과, 기믹 충돌 이상 |

## 4. 오브젝트별 상세 명세

### 4.1 보스 프리팹

- 경로: `Assets/Prefabs/Monster/Boss/Boss.prefab`
- 루트에 필요한 컴포넌트:
  - `BossHealth`
  - `Boss`
  - `BehaviorGraphAgent`
  - `Rigidbody2D`
  - `Collider2D`
- 루트 레이어: `Boss`
- 자식 `BodyHitbox`: 몸체 접촉 피해용 `PlayerDamageSource`와 Trigger Collider 필요
- 자식 **`SlamHitbox`**: 점프 강하 판정용 `PlayerDamageSource`와 Trigger Collider 필요, 기본 비활성

`SlamHitbox`는 `JumpSlamAction`이 `transform.Find("SlamHitbox")`로 찾으므로 이름과 보스의 직계 자식 관계를 바꾸면 안 된다.

`BossHealth` 기본 계약:

| 필드 | 기준값 | 설명 |
| --- | ---: | --- |
| `maxHp` | 1000 | 최대 체력 |
| `invulnDuration` | 0.1초 | 보스 피격 직후 무적 |

`Boss` Inspector 계약:

| 필드 | 할당값 | 중요도 |
| --- | --- | --- |
| `target` | 플레이어 자신 또는 `PlayerHealth`/`PlayerController`의 자식 Transform | 필수 |
| `spikeWalls` | `SpikeWalls` 루트 | 2페이즈 기능 필수 |
| `enhancedElectric` | `EnhancedElectric`의 `ElectricFloorScheduler` | 2페이즈 기능 필수 |
| `onDeathSequenceFinished` | 출구 개방, 보상, 플레이어 회복, 진행 저장 담당 이벤트 | 게임 진행 연동 시 필수 |

### 4.2 플레이어 계약

`Boss.target`으로 지정한 Transform의 자신 또는 부모 계층에서 아래 컴포넌트를 찾을 수 있어야 한다.

- `PlayerHealth`: 보스가 플레이어 사망 이벤트를 구독하고 컷신 무적을 적용한다.
- `PlayerController`: 페이즈 전환 중 이동과 공격을 잠근다.
- `PlayerMovement`: `PlayerController`의 필수 컴포넌트다.

플레이어 공격은 다음 계약을 만족해야 한다.

- 보스 레이어를 검출해야 한다. 현재 `PlayerAttack`은 `targetLayer`가 비어 있으면 `Boss` 레이어를 사용한다.
- 맞은 Collider의 부모에서 `BossHealth`를 찾고 `TakeDamage(int)`를 호출한다.
- 보스/기믹의 `PlayerDamageSource`는 플레이어에게 기본 1하트 피해를 준다.

### 4.3 Behavior Graph / Blackboard 계약

- 그래프 에셋: `Assets/Behavior/BossBrain.asset`
- 변수 이름과 타입은 대소문자를 포함해 변경하지 않는다.

| 변수명 | 타입 | 초기값/할당 | 담당 |
| --- | --- | --- | --- |
| `Target` | GameObject | `Boss.Start()`가 `Boss.target`으로 자동 설정 | 추적, 돌진, 점프 강하 |
| `Phase` | int | 1 | 페이즈별 공격 후보 제한 |
| `LearningDone` | bool | false | 최초 학습 패턴 실행 여부 |
| `LastAttackIndex` | int | -1 | 같은 개체 공격 연속 선택 방지 |
| `ElectricFloor` | GameObject | 일반 전기 루트 | 학습 패턴 및 1페이즈 전기 시작 |
| `Beam` | GameObject | `BossBeam` | 2페이즈 빔 공격 |

`Target`은 런타임에 코드가 덮어쓰지만, `ElectricFloor`와 `Beam`은 씬 오브젝트이므로 **각 씬의 `BehaviorGraphAgent` 노출 변수에서 직접 할당**해야 한다.

### 4.4 일반 전기 바닥

권장 계층:

```text
ElectricFloor
├─ Zone_1
│  └─ DamageTrigger (기본 비활성)
├─ Zone_2
│  └─ DamageTrigger
├─ Zone_3
│  └─ DamageTrigger
├─ Zone_4
│  └─ DamageTrigger
└─ Zone_5
   └─ DamageTrigger
```

- 루트: `ElectricFloorScheduler`
- `zones`: 5개의 `HazardBase`를 모두 할당
- 각 Zone: `SpriteRenderer`, `TerrainDescriptor(ElectricFloor)`, `HazardBase`
- 각 `DamageTrigger`: Trigger `BoxCollider2D`, `PlayerDamageSource`, 기본 비활성
- Blackboard `ElectricFloor`: 이 루트 GameObject 할당
- 기준 동작: 예고 1초 → 활성 2초 → 대기 2초, 5구역 중 1~3구역 선택

### 4.5 강화 전기

권장 계층은 일반 전기와 같으며 이름만 `EnhancedElectric`, `Line_1`~`Line_5`를 사용한다.

- 루트: `ElectricFloorScheduler`
- 각 Line: `HazardBase`, `TerrainDescriptor(EnhancedElectricFloor)`
- 각 DamageTrigger: Trigger Collider와 `PlayerDamageSource`
- `Boss.enhancedElectric`에 루트의 스케줄러 할당
- 기준 동작: 예고 1초 → 활성 2.5초 → 대기 2초

강화 전기가 할당되지 않아도 예외 대신 일반 전기가 2페이즈에서 재사용된다. 이는 폴백이며 최종 맵 구성은 아니다.

### 4.6 가시벽

권장 계층:

```text
SpikeWalls (기본 비활성)
├─ SpikeWall_L
└─ SpikeWall_R
```

- 자식 벽마다 Trigger `BoxCollider2D`, `PlayerDamageSource`
- 레이어: `Hazard`
- 루트는 기본 비활성
- `Boss.spikeWalls`에 루트 할당
- 페이즈 전환 컷신 시작 시 활성화되고, 사망/재도전 초기화 시 비활성화된다.

### 4.7 관통 빔

- 권장 이름: `BossBeam`
- 기본 비활성
- 레이어: `Hazard`
- 필수 컴포넌트:
  - `SpriteRenderer`
  - Trigger `BoxCollider2D`
  - `PlayerDamageSource`
  - `TimedDeactivate`
- `BehaviorGraphAgent` Blackboard의 `Beam`에 할당
- 아레나 전체 폭을 덮도록 크기를 조정한다. 높이는 공격 시 낮음/중간/높음 중 하나로 코드가 이동시킨다.

### 4.8 보스 체력 게이지

권장 계층:

```text
BossHealthCanvas
└─ BossGauge
   ├─ Fill       (HP 0~500)
   └─ FillTop    (HP 500~1000)
```

- `Canvas`: Screen Space Overlay 권장
- `Fill`, `FillTop`: `Image.Type = Filled`, Horizontal, Origin Left
- `BossHealthGauge.boss`: 씬의 보스 `BossHealth`
- `BossHealthGauge.fill`: 아래층 `Fill`
- `BossHealthGauge.fillTop`: 위층 `FillTop`

`fillTop`은 선택 참조라 누락해도 단일 게이지로 동작하지만, 기획된 2단 게이지를 쓰려면 두 Fill이 모두 필요하다.

## 5. 레이어 및 물리 설정

| 레이어 | 적용 대상 | 용도 |
| --- | --- | --- |
| `Boss` | 보스의 피격 Collider | `PlayerAttack`이 보스를 검출 |
| `Solid` | 바닥과 좌우 벽 등 돌진을 막는 지형 | `DashAction`의 Raycast로 돌진 조기 정지 |
| `Hazard` | 전기, 가시벽, 빔 등 | 위험 판정 분리 |

추가 확인 사항:

- 보스와 플레이어 사이의 Physics 2D 충돌 설정은 의도에 맞게 구성한다.
- 돌진은 `Solid` 레이어만 벽으로 인식한다. 벽이 다른 레이어이면 관통할 수 있다.
- 플레이어 공격의 `targetLayer`에 `Boss`가 포함되어야 한다.
- 모든 피해 Collider는 Trigger이며, 플레이어의 Trigger 이벤트 수신 조건(Rigidbody2D 포함)을 만족해야 한다.

## 6. `arena_boss_test.unity` 적용 시 확인할 점

현재 씬에는 이름상 `boss`, `electric floor`, `enhanced electric`, `SpikeWall`, 체력 UI로 보이는 오브젝트가 존재한다. 그러나 구현 코드와 셋업 도구의 권장 이름(`ElectricFloor`, `EnhancedElectric`, `SpikeWalls`, `BossBeam`, `BossHealthCanvas`)과 다르므로 **이름만 보고 연결 완료로 판단하면 안 된다.** Inspector의 실제 컴포넌트와 직렬화 참조를 확인해야 한다.

특히 다음을 우선 확인한다.

- `Boss.target`이 실제 플레이어를 가리키는가
- Blackboard `ElectricFloor`가 일반 전기 스케줄러 루트를 가리키는가
- Blackboard `Beam`이 기본 비활성 `BossBeam`을 가리키는가
- `Boss.enhancedElectric`과 `Boss.spikeWalls`가 할당되어 있는가
- `BossHealthGauge`의 `boss`, `fill`, `fillTop`이 모두 할당되어 있는가
- 보스 레이어가 `Boss`, 지형 레이어가 `Solid`인가
- 보스 프리팹 자식 `SlamHitbox`의 이름과 계층이 유지되어 있는가

## 7. 수동 통합 QA 체크리스트

### 시작 전

- [ ] Console에 컴파일 오류가 없다.
- [ ] 보스 프리팹 인스턴스가 씬에 하나만 있다.
- [ ] `Boss.target`이 할당되어 있다.
- [ ] Blackboard `ElectricFloor`, `Beam`이 할당되어 있다.
- [ ] `Boss.spikeWalls`, `Boss.enhancedElectric`이 할당되어 있다.
- [ ] 체력 게이지의 `boss`, `fill`, `fillTop`이 할당되어 있다.
- [ ] `SpikeWalls`, `BossBeam`, `SlamHitbox`가 시작 시 비활성이다.

### 플레이 모드

- [ ] 보스가 플레이어를 추적한다.
- [ ] 최초 학습 패턴과 일반 전기 바닥이 실행된다.
- [ ] 돌진이 `Solid` 벽 앞에서 멈춘다.
- [ ] 점프 강하 시에만 `SlamHitbox`가 잠시 활성화된다.
- [ ] 플레이어 공격이 보스 HP와 2단 게이지를 감소시킨다.
- [ ] 보스 공격/기믹이 플레이어에게 1하트 피해를 준다.
- [ ] HP 500에서 행동이 중단되고 페이즈 전환 컷신과 입력 잠금이 실행된다.
- [ ] 전환 시 일반 전기가 사라지고 가시벽이 켜진다.
- [ ] 2페이즈에서 강화 전기와 빔이 실행된다.
- [ ] 보스 사망 시 모든 공격 판정이 꺼지고 `onDeathSequenceFinished`가 호출된다.
- [ ] 플레이어 사망 시 화면이 정지하며, 재도전 호출 후 보스/기믹/입력이 정상 복구된다.

### 에디터용 빠른 테스트

Play Mode에서 `Boss` 컴포넌트 컨텍스트 메뉴를 사용할 수 있다.

- `Test: Damage 500`: 페이즈 2 전환 확인
- `Test: Kill`: 보스 사망 처리 확인
- `Test: Freeze (Player Death)`: 플레이어 사망 정지 확인
- `Test: Reset`: 재도전 초기화 확인

## 8. 자주 발생하는 증상별 점검

| 증상/로그 | 우선 점검 |
| --- | --- |
| `[Boss] target 미할당 — 추적이 동작하지 않음` | `Boss.target`에 플레이어 Transform 할당 |
| 보스가 서 있기만 함 | `BehaviorGraphAgent`의 그래프가 `BossBrain.asset`인지 확인 |
| 학습 패턴 또는 일반 전기 미동작 | Blackboard `ElectricFloor`와 루트의 `ElectricFloorScheduler.zones` 확인 |
| 점프 강하 피해 없음 | 직계 자식 이름 `SlamHitbox`, 활성 상태, Trigger Collider, `PlayerDamageSource` 확인 |
| 돌진이 벽을 통과함 | 벽이 `Solid` 레이어인지 확인 |
| 플레이어 공격이 보스에게 안 맞음 | 보스 Collider 레이어 `Boss`, `PlayerAttack.targetLayer` 확인 |
| 빔이 안 나옴/즉시 실패 | Blackboard `Beam`, `BossBeam` 컴포넌트와 기본 비활성 상태 확인 |
| 2페이즈에 일반 전기가 계속 나옴 | `Boss.enhancedElectric` 미할당 여부 확인 |
| 페이즈 전환 시 가시벽 없음 | `Boss.spikeWalls` 할당 및 루트 기본 비활성 확인 |
| 게이지가 안 줄어듦 | `BossHealthGauge.boss/fill/fillTop` 참조 확인 |
| 재도전 후 게임이 계속 멈춤 | 게임 흐름 코드가 `Boss.ResetForRetry()`를 호출하는지 확인 |

## 9. 외부 시스템 연동 API

게임 흐름 담당 코드가 직접 사용할 공개 진입점은 다음과 같다.

```csharp
// 플레이어 공격이 보스에게 피해를 줄 때
bossHealth.TakeDamage(damage);

// 플레이어 사망 시 자동 정지를 끈 구성에서 수동 호출
boss.FreezeForPlayerDeath();

// 블랙아웃 완료 후 재도전 시작
boss.ResetForRetry();
```

보스 처치 뒤 출구 개방, 보상 지급, 플레이어 완전 회복, 진행 저장은 `Boss.onDeathSequenceFinished` UnityEvent에 연결한다. 해당 후처리는 현재 보스 코드 내부에서 자동 구현하지 않는다.

