using UnityEngine;

/// 외부 Trigger Channel 신호로 작동하는 위험 지형: 가시(Spike), 강화 전기장판(EnhancedElectricFloor).
/// 지형 종류 구분은 같은 오브젝트의 TerrainDescriptor.terrainKind가 담당한다.
/// 보스는 HazardChannel.Send(채널)만 호출하며 이 오브젝트를 직접 참조하지 않는다 (TERR-008).

public sealed class ChannelTriggeredHazard : HazardBase
{
    [SerializeField] private string triggerChannel = "LAB_SPIKE_A";

    public string TriggerChannel => triggerChannel;

    protected override void OnEnable()
    {
        base.OnEnable();
        HazardChannel.SignalSent += OnSignal;
    }

    protected override void OnDisable()
    {
        HazardChannel.SignalSent -= OnSignal;
        base.OnDisable();
    }

    private void OnSignal(string channel, bool activate)
    {
        // 다른 채널 신호는 무시한다 (문서 14.7)
        if (channel != triggerChannel)
        {
            return;
        }

        if (activate)
        {
            StartCycle();
        }
        else
        {
            Deactivate();
        }
    }

    /// 씬 빌더/에디터 셋업용.
    public void SetChannel(string channel) => triggerChannel = channel;
}
