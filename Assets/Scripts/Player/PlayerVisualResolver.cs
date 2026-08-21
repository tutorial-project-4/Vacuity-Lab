using UnityEngine;

public static class PlayerVisualResolver
{
    private const string VisualRootName = "PlayerVisual";

    public static Transform FindVisualRoot(Transform owner, Transform cachedRoot = null)
    {
        if (cachedRoot != null)
        {
            return cachedRoot;
        }

        return owner != null ? owner.Find(VisualRootName) : null;
    }

    public static SpriteRenderer FindSpriteRenderer(Component owner, Transform visualRoot = null)
    {
        if (owner == null)
        {
            return null;
        }

        Transform visual = FindVisualRoot(owner.transform, visualRoot);
        if (visual != null && visual.TryGetComponent(out SpriteRenderer visualRenderer))
        {
            return visualRenderer;
        }

        SpriteRenderer[] renderers = owner.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].transform != owner.transform)
            {
                return renderers[i];
            }
        }

        return owner.GetComponent<SpriteRenderer>();
    }

    public static Animator FindAnimator(Component owner, Transform visualRoot = null)
    {
        if (owner == null)
        {
            return null;
        }

        Transform visual = FindVisualRoot(owner.transform, visualRoot);
        if (visual != null && visual.TryGetComponent(out Animator visualAnimator))
        {
            return visualAnimator;
        }

        return owner.GetComponent<Animator>();
    }
}
