using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Boss2MovingWall : MonoBehaviour
{
    const int ContactDamage = 1;
    readonly HashSet<PlayerController> contactedPlayers = new();
    Action entered;
    Action<GameObject> exited;
    float speed;
    float arenaRightX;
    float despawnX;
    bool enteredArena;
    bool exiting;

    public void Initialize(float moveSpeed, float rightX, float leftX, Action onEntered, Action<GameObject> onExited)
    {
        speed = moveSpeed;
        arenaRightX = rightX;
        despawnX = leftX;
        entered = onEntered;
        exited = onExited;

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
        if (!enteredArena && transform.position.x <= arenaRightX)
        {
            enteredArena = true;
            entered?.Invoke();
        }
        if (transform.position.x <= despawnX) Exit();
    }

    void OnCollisionEnter2D(Collision2D collision) => TryKnockback(collision.collider);
    void OnCollisionStay2D(Collision2D collision) => TryKnockback(collision.collider);

    void OnCollisionExit2D(Collision2D collision)
    {
        PlayerController player = collision.collider.GetComponentInParent<PlayerController>();
        if (player != null) contactedPlayers.Remove(player);
    }

    void TryKnockback(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        PlayerWallPhaseDash wallDash = player.GetComponent<PlayerWallPhaseDash>();
        if (wallDash != null && wallDash.IsWallPhaseDashing) return;
        if (contactedPlayers.Contains(player)) return;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null && health.TakeDamage(ContactDamage, transform.position))
        {
            contactedPlayers.Add(player);
            player.ReceiveKnockback(Vector2.left * 2.5f, .25f);
        }
    }

    void Exit()
    {
        if (exiting) return;
        exiting = true;
        exited?.Invoke(gameObject);
        Destroy(gameObject);
    }

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
