using UnityEngine;

/// 테스트 씬용 수동 트리거 패널 (문서 10 HazardTriggerPanel).
/// 보스 신호 송신부가 구현되기 전까지 이 버튼이 보스 역할을 대신한다 (문서 12.6).

public sealed class HazardDebugTrigger : MonoBehaviour
{
    [SerializeField] private string[] channels = { "LAB_SPIKE_A", "LAB_ELECTRIC_ENHANCED_A" };

    private void OnGUI()
    {
        float y = 10f;
        foreach (string channel in channels)
        {
            if (GUI.Button(new Rect(10f, y, 280f, 30f), $"Trigger: {channel}"))
            {
                HazardChannel.Send(channel);
            }

            y += 34f;
        }
    }
}
