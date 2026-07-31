using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// #8 전기 바닥 필드 스케줄러 (세부기획 D-3, E-1).
/// 보스 Behavior Graph와 완전 독립으로 반복한다:
/// 1~3구역 무작위 선택 → 존 사이클(예고 1s → 활성 2s, HazardBase 담당) → 대기 2s → 반복.
/// 5구역 중 최대 3구역 선택이 곧 "최소 안전 2구역" 보장이다 — 개체 공격까지 포함한
/// 안전 공간 검사(DETAIL I절)는 #9에서 연결한다.
public sealed class ElectricFloorScheduler : MonoBehaviour
{
    [Tooltip("바닥 5구역, 왼쪽부터 B1~B5. 존별 시간은 HazardBase에서 예고 1s/활성 2s/쿨다운 0으로 설정한다.")]
    [SerializeField] private HazardBase[] zones;

    [Tooltip("전기 종료 후 다음 배치까지 대기 (잠정 2s, 플레이테스트 범위 1.5~3s)")]
    [SerializeField, Min(0f)] private float restDuration = 2f;

    [Tooltip("단독 테스트용 자동 시작. 실전 흐름(#9)은 그래프가 학습 패턴 종료 후 Begin()을 호출한다.")]
    [SerializeField] private bool autoStart = false;

    private Coroutine loop;

    private void Start()
    {
        if (autoStart)
        {
            Begin();
        }
    }

    /// 스케줄러 시작 (#9: 최초 학습 패턴 종료 후 / 재도전 준비시간 후 호출).
    public void Begin()
    {
        if (loop == null && isActiveAndEnabled)
        {
            loop = StartCoroutine(Loop());
        }
    }

    /// #9 최초 학습 패턴 고정 배치(세부기획 C-4): B1~B3 1사이클만 발동한다.
    /// 루프와 무관한 1회성 — 루프 시작은 학습 패턴 종료 후 Begin()으로.
    public void RunLearningBatch()
    {
        for (int i = 0; i < 3 && i < zones.Length; i++)
        {
            if (zones[i] != null)
            {
                zones[i].StartCycle();
            }
        }
        Debug.Log("[ElectricFloor] 학습 배치: B1~B3 사이클 시작");
    }

    /// 즉시 중단하고 모든 예고·판정을 제거한다 (#10 전환 컷신, #15 리셋용).
    public void Stop()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }

        foreach (HazardBase zone in zones)
        {
            if (zone != null)
            {
                zone.Deactivate();
            }
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    private IEnumerator Loop()
    {
        var picked = new List<HazardBase>();
        while (true)
        {
            int count = Random.Range(1, 4); // 1~3구역 (최대 3 → 안전 2구역 자동 보장)
            picked.Clear();
            picked.AddRange(zones);
            picked.RemoveAll(z => z == null);
            while (picked.Count > count)
            {
                picked.RemoveAt(Random.Range(0, picked.Count));
            }

            foreach (HazardBase zone in picked)
            {
                zone.StartCycle();
            }
            Debug.Log($"[ElectricFloor] {picked.Count}구역 사이클 시작: {string.Join(", ", picked.ConvertAll(z => z.name))}");

            yield return new WaitUntil(() => picked.TrueForAll(z => !z.IsCycleRunning));
            yield return new WaitForSeconds(restDuration);
        }
    }
}
