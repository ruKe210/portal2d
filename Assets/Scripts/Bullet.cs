using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("子弹设置")]
    public float speed = 10f;
    public float lifeTime = 5f;

    [HideInInspector] public Vector3 direction;
    [HideInInspector] public bool isRedPortalBullet = true; // true=红门, false=绿门

    private Rigidbody2D rb;
    private Coroutine lifetimeCoroutine;
    private Player player;

    void Awake()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        if (lifetimeCoroutine != null)
            StopCoroutine(lifetimeCoroutine);
        lifetimeCoroutine = StartCoroutine(LifetimeTimer());

        if (rb != null)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    IEnumerator LifetimeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ShouldIgnore(other.gameObject)) return;
        // 用子弹自身位置作为碰撞点（Trigger 没有 contact point）
        HandleHit(transform.position, GetWallNormal(other), other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (ShouldIgnore(collision.gameObject)) return;

        Vector2 normal = SnapToAxis(collision.contacts[0].normal);
        Vector3 hitPoint = collision.contacts[0].point;
        HandleHit(hitPoint, normal, collision.collider);
    }

    bool ShouldIgnore(GameObject obj)
    {
        int layer = obj.layer;
        return layer == LayerMask.NameToLayer("Portal")
            || layer == LayerMask.NameToLayer("Player");
    }

    /// <summary>
    /// 从 Trigger 碰撞中估算墙壁法线（没有 ContactPoint，用子弹方向反推）
    /// </summary>
    Vector2 GetWallNormal(Collider2D other)
    {
        // 用子弹飞行方向的反方向作为近似法线
        // 然后对齐到最近的轴向（上下左右），让传送门贴合墙壁
        Vector2 approxNormal = -direction.normalized;
        return SnapToAxis(approxNormal);
    }

    /// <summary>
    /// 将方向对齐到最近的轴向（上下左右）
    /// </summary>
    Vector2 SnapToAxis(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? Vector2.right : Vector2.left;
        else
            return dir.y > 0 ? Vector2.up : Vector2.down;
    }

    void HandleHit(Vector3 hitPosition, Vector2 wallNormal, Collider2D wallCol)
    {
        if (player != null)
        {
            player.SpawnPortal(isRedPortalBullet, hitPosition, wallNormal, wallCol);
        }
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        if (BulletPool.Instance != null)
            BulletPool.Instance.ReturnBullet(gameObject);
        else
            Destroy(gameObject);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
