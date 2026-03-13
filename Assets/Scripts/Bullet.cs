using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("子弹设置")]
    [Tooltip("子弹速度")]
    public float speed = 10f;

    [Tooltip("子弹生存时间（秒），超时自动销毁")]
    public float lifeTime = 5f;

    [Tooltip("使用射线检测防止穿透")]
    public bool useRaycast = true;

    public Portal RedPortal;
    public Portal GreenPortal;


    public Vector3 direction ;
    private Rigidbody2D rb;
    private Coroutine lifetimeCoroutine;
    private Vector3 lastPosition;
    
    private Player player;
    // private bool flag = false;

    void Awake()
    {
        // flag = false;
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // 每次从对象池激活时，重置生存时间计时器
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }
        lifetimeCoroutine = StartCoroutine(LifetimeTimer());

        // 记录初始位置
        lastPosition = transform.position;

        // 设置 Rigidbody2D 为 Continuous 碰撞检测
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    IEnumerator LifetimeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 碰到任何物体都销毁子弹
        Debug.Log($"子弹碰到: {other.gameObject.name}");
        if(other.gameObject.layer == LayerMask.NameToLayer("Portal")||other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            return;
        }
        else
        {
            if(player.flag)
            {
                player.GreenPortal = Instantiate(GreenPortal, transform.position, Quaternion.identity);
                player.MakePortalConnection();
            }
            else
            {
                player.RedPortal = Instantiate(RedPortal, transform.position, Quaternion.identity);

            }
            player.flag=!player.flag;

        }
        ReturnToPool();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 如果使用的是普通碰撞器（非Trigger）
        Debug.Log($"子弹碰到: {collision.gameObject.name}");
        if(collision.gameObject.layer == LayerMask.NameToLayer("Portal")||collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            return;
        }
        else
        {
            if(player.flag)
            {
                player.GreenPortal = Instantiate(GreenPortal, transform.position, Quaternion.identity);
                player.MakePortalConnection();
            }
            else
            {
                player.RedPortal = Instantiate(RedPortal, transform.position, Quaternion.identity);
            }
            player.flag=!player.flag;
            // Instantiate(RedPortal, transform.position, Quaternion.identity);
        }
        ReturnToPool();
    }

    void ReturnToPool()
    {
        // 停止生存时间协程
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        // 返回到对象池
        if (BulletPool.Instance != null)
        {
            BulletPool.Instance.ReturnBullet(gameObject);
        }
        else
        {
            // 如果没有对象池，直接销毁
            Destroy(gameObject);
        }
    }
    void Update()
    {
        this.transform.Translate(direction * speed * Time.deltaTime);
    }
}