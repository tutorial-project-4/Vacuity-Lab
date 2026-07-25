using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class CinemachineScreenShake2D : CinemachineExtension
{
    private float shakeTimer;
    private float shakeDuration;
    private float shakeStrength;

    public void Shake(float duration, float strength)
    {
        shakeDuration = Mathf.Max(0f, duration);
        shakeTimer = shakeDuration;
        shakeStrength = Mathf.Max(0f, strength);
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase virtualCamera,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || shakeTimer <= 0f)
        {
            return;
        }

        shakeTimer = Mathf.Max(0f, shakeTimer - deltaTime);
        float fade = shakeDuration > 0f ? shakeTimer / shakeDuration : 0f;
        Vector2 randomOffset = Random.insideUnitCircle * shakeStrength * fade;
        state.PositionCorrection += new Vector3(randomOffset.x, randomOffset.y, 0f);
    }
}
