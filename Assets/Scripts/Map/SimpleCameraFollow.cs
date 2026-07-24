using UnityEngine;

/// 테스트 씬용 최소 추적 카메라.
public sealed class SimpleCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    public void SetTarget(Transform value) => target = value;

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}
