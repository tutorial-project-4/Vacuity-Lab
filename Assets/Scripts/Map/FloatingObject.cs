using UnityEngine;

[DisallowMultipleComponent]
public class FloatingObject : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float amplitude = 0.08f;
    [SerializeField] private float frequency = 1.4f;
    [SerializeField] private float phaseOffset;
    [SerializeField] private bool useUnscaledTime;

    private Vector3 startLocalPosition;

    private void Awake()
    {
        CacheStartPosition();
    }

    private void OnEnable()
    {
        CacheStartPosition();
    }

    private void Update()
    {
        Transform target = GetTarget();
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float offsetY = Mathf.Sin((time + phaseOffset) * frequency) * amplitude;
        target.localPosition = startLocalPosition + Vector3.up * offsetY;
    }

    private void CacheStartPosition()
    {
        startLocalPosition = GetTarget().localPosition;
    }

    private Transform GetTarget()
    {
        return visualRoot != null ? visualRoot : transform;
    }

    private void OnValidate()
    {
        amplitude = Mathf.Max(0f, amplitude);
        frequency = Mathf.Max(0f, frequency);
    }
}
