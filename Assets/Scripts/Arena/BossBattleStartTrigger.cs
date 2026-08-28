using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BossBattleStartTrigger : MonoBehaviour
{
    [Tooltip("IBossEncounter를 구현한 보스 컴포넌트입니다.")]
    [SerializeField] MonoBehaviour boss;
    [SerializeField] float dropHeight = 20f;
    [SerializeField] float entranceDuration = 1f;
    bool triggered;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || other.GetComponentInParent<PlayerController>() == null) return;
        if (boss is not IBossEncounter encounter)
        {
            Debug.LogWarning("[BossBattleStartTrigger] IBossEncounter 보스 참조가 없습니다", this);
            return;
        }

        triggered = true;
        StartCoroutine(PlayEntrance(encounter));
    }

    IEnumerator PlayEntrance(IBossEncounter encounter)
    {
        Transform bossTransform = boss.transform;
        Vector3 end = bossTransform.position;
        Vector3 start = end + Vector3.up * dropHeight;
        Collider2D[] colliders = boss.GetComponentsInChildren<Collider2D>(true);
        bool[] enabledStates = System.Array.ConvertAll(colliders, item => item.enabled);
        Rigidbody2D body = boss.GetComponent<Rigidbody2D>();
        bool bodySimulated = body == null || body.simulated;
        foreach (Collider2D item in colliders) item.enabled = false;
        if (body != null) body.simulated = false;

        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.deltaTime;
            bossTransform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / entranceDuration));
            yield return null;
        }

        bossTransform.position = end;
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = enabledStates[i];
        if (body != null) body.simulated = bodySimulated;
        encounter.BeginBattle();
    }
}
