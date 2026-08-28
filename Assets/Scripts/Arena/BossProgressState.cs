using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossProgressState : MonoBehaviour
{
    [SerializeField] private bool isBoss1Cleared;

    public bool IsBoss1Cleared => isBoss1Cleared;

    public void MarkBoss1Cleared()
    {
        isBoss1Cleared = true;
    }

    public void ResetBoss1Clear()
    {
        isBoss1Cleared = false;
    }
}
