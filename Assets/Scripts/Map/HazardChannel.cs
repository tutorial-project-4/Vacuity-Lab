using System;

/// 보스 ↔ 위험 지형 간 느슨한 연결용 신호 버스 (문서 9.4~9.5, TERR-008).
/// 보스 측은 채널 문자열만 알고 HazardChannel.Send()를 호출한다.
/// 수신 지형 오브젝트를 직접 참조하지 않으므로 보스가 없거나 파괴돼도 안전하다.

public static class HazardChannel
{
    /// (channel, activate)
    public static event Action<string, bool> SignalSent;

    public static void Send(string channel, bool activate = true)
    {
        SignalSent?.Invoke(channel, activate);
    }
}
