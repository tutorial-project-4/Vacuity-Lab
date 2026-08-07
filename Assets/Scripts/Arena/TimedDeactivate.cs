using UnityEngine;

/// 지정 시간 뒤 자기 GameObject를 끈다 — 빔 잔상처럼 발사한 액션보다 오래 남는 판정용.
/// Arm() 전에는 아무것도 하지 않고, 어떤 경로로든 꺼지면(OnDisable) 무장 해제된다.
public class TimedDeactivate : MonoBehaviour
{
    float _end = float.PositiveInfinity;

    public void Arm(float seconds)
    {
        _end = Time.time + seconds;
    }

    void OnDisable()
    {
        _end = float.PositiveInfinity; // 컷신 등 외부 SetActive(false) 포함 — 다음 활성화 때 낡은 타이머로 즉시 꺼지는 것 방지
    }

    void Update()
    {
        if (Time.time >= _end) gameObject.SetActive(false);
    }
}
