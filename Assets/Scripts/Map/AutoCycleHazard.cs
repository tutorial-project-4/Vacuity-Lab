using System.Collections;
using UnityEngine;

/// 일반 전기장판(ElectricFloor)용: 보스 없이 자체 타이머로 반복 작동한다 (문서 7.9, TERR-007).
/// 외부 트리거로 작동하는 가시/강화 전기장판은 ChannelTriggeredHazard를 쓴다.
/// OnEnable에서 시작하는 코루틴은 Disable 시 자동 중단되므로 중복 타이머가 생기지 않는다 (문서 14.8).

public sealed class AutoCycleHazard : HazardBase
{
    [Header("Auto Cycle (TBD - 기획 확정 전 임시값)")]
    [SerializeField] private float startDelay = 1f;
    [Tooltip("같은 그룹 내 장판 간 시작 시차 (문서 7.9 PhaseOffset)")]
    [SerializeField] private float phaseOffset = 0f;
    [SerializeField] private bool loop = true;

    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(AutoRoutine());
    }

    private IEnumerator AutoRoutine()
    {
        yield return new WaitForSeconds(startDelay + phaseOffset);

        do
        {
            StartCycle();
            yield return new WaitUntil(() => !IsCycleRunning);
        } while (loop);
    }
}
