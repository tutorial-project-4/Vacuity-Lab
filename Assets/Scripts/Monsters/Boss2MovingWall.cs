using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Boss2MovingWall : MonoBehaviour
{
    readonly HashSet<PlayerController> contactedPlayers = new();
    Action<GameObject> exited;
    Action knockedBackPlayer;
    float speed;
    float despawnX;
    bool exiting;

    public void Initialize(float moveSpeed, float leftX, Action<GameObject> onExited, Action onKnockedBackPlayer)
    {
        speed = moveSpeed;
        despawnX = leftX;
        exited = onExited;
        knockedBackPlayer = onKnockedBackPlayer;

        int layer = LayerMask.NameToLayer("DashPassableWall");
        SetLayerRecursively(transform, layer);
        TerrainDescriptor descriptor = gameObject.AddComponent<TerrainDescriptor>();
        descriptor.terrainKind = TerrainKind.DashPassableWall;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            if (renderer.GetComponent<Collider2D>() != null) continue;
            BoxCollider2D collider = renderer.gameObject.AddComponent<BoxCollider2D>();
            collider.size = renderer.sprite.bounds.size;
        }
        Rigidbody2D body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.useFullKinematicContacts = true;
    }

    void FixedUpdate()
    {
        transform.position += Vector3.left * (speed * Time.fixedDeltaTime);
        if (transform.position.x <= despawnX) Exit();
    }

    void OnCollisionEnter2D(Collision2D collision) => TryKnockback(collision);
    void OnCollisionStay2D(Collision2D collision) => TryKnockback(collision);

    void OnCollisionExit2D(Collision2D collision)
    {
        PlayerController player = collision.collider.GetComponentInParent<PlayerController>();
        if (player != null) contactedPlayers.Remove(player);
    }

    void TryKnockback(Collision2D collision)
    {
        PlayerController player = collision.collider.GetComponentInParent<PlayerController>();
        if (player == null) return;
        PlayerWallPhaseDash wallDash = player.GetComponent<PlayerWallPhaseDash>();
        if (wallDash != null && wallDash.IsWallPhaseDashing) return;
        if (contactedPlayers.Contains(player)) return;
        contactedPlayers.Add(player);
        player.ReceiveHit(collision.GetContact(0).point, 0f, collision.otherCollider);
        knockedBackPlayer?.Invoke();
    }

    void Exit()
    {
        if (exiting) return;
        exiting = true;
        exited?.Invoke(gameObject);
        Destroy(gameObject);
    }

    public void SuppressExitCallback() => exiting = true;

    void OnDestroy()
    {
        if (!exiting) exited?.Invoke(gameObject);
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root) SetLayerRecursively(child, layer);
    }
}
