using UnityEngine;

public sealed class StartBoss2IntroAction : DialogueInteractionAction
{
    [SerializeField] private Boss2IntroTrigger introTrigger;
    [SerializeField] private BossArenaRespawnController respawnController;
    [SerializeField] private BossRetryCheckpoint retryCheckpoint;

    public override void Run()
    {
        ActivateRetryCheckpoint();

        if (introTrigger == null)
        {
            Debug.LogWarning("[StartBoss2IntroAction] Boss2IntroTrigger 참조가 없습니다.", this);
            return;
        }

        introTrigger.PlayIntro();
    }

    private void ActivateRetryCheckpoint()
    {
        BossArenaRespawnController controller = respawnController;
        if (controller == null)
        {
#if UNITY_2023_1_OR_NEWER
            controller = FindFirstObjectByType<BossArenaRespawnController>();
#else
            controller = FindObjectOfType<BossArenaRespawnController>();
#endif
        }

        if (controller == null || retryCheckpoint == null)
        {
            Debug.LogWarning("[StartBoss2IntroAction] Boss2 리트라이 체크포인트 참조가 부족합니다.", this);
            return;
        }

        controller.ActivateCheckpoint(retryCheckpoint);
    }
}
