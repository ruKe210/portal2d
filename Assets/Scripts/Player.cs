using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody2D rb;
    public float maxSpeed = 30f;
    public float moveSpeed = 5f;

    [Header("传送门预制体")]
    public GameObject redPortalPrefab;
    public GameObject greenPortalPrefab;

    // 当前场景中的传送门实例
    [HideInInspector] public GameObject activeRedPortal;
    [HideInInspector] public GameObject activeGreenPortal;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    void Update()
    {
        bool movingHorizontally = false;

        if (Input.GetKey(KeyCode.A))
        {
            rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
            movingHorizontally = true;
        }
        if (Input.GetKey(KeyCode.D))
        {
            rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
            movingHorizontally = true;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = new Vector2(rb.velocity.x, 10f);
        }

        if (!movingHorizontally)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    /// <summary>
    /// 生成或替换传送门。由 Bullet 碰撞时调用。
    /// </summary>
    /// <param name="isRed">true=红门, false=绿门</param>
    /// <param name="position">传送门生成位置</param>
    /// <param name="wallNormal">墙壁法线（传送门朝向）</param>
    public void SpawnPortal(bool isRed, Vector3 position, Vector2 wallNormal, Collider2D wallCol = null)
    {
        GameObject prefab = isRed ? redPortalPrefab : greenPortalPrefab;
        if (prefab == null)
        {
            Debug.LogError("Player: 传送门预制体未设置!");
            return;
        }

        // 销毁旧的同色传送门
        if (isRed && activeRedPortal != null)
        {
            Destroy(activeRedPortal);
        }
        else if (!isRed && activeGreenPortal != null)
        {
            Destroy(activeGreenPortal);
        }

        // 根据墙壁法线计算传送门旋转
        // 传送门的长边应该沿着墙壁表面（垂直于法线）
        // Quaternion.Euler(0,0,angle) 让 transform.right = wallNormal 方向
        // 我们需要 transform.up 沿墙壁表面，所以额外旋转 90 度
        float angle = Mathf.Atan2(wallNormal.y, wallNormal.x) * Mathf.Rad2Deg - 90f;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        // 直接在碰撞点生成
        Vector3 spawnPos = position;

        GameObject portal = Instantiate(prefab, spawnPos, rotation);

        // 设置 Portal2D 的法线方向
        Portal2D portalScript = portal.GetComponent<Portal2D>();
        if (portalScript != null)
        {
            portalScript.portalNormal = wallNormal;
            portalScript.wallCollider = wallCol;
        }

        if (isRed)
        {
            activeRedPortal = portal;
        }
        else
        {
            activeGreenPortal = portal;
        }

        // 如果两个传送门都存在，互相连接
        LinkPortals();
    }

    void LinkPortals()
    {
        if (activeRedPortal == null || activeGreenPortal == null) return;

        Portal2D red = activeRedPortal.GetComponent<Portal2D>();
        Portal2D green = activeGreenPortal.GetComponent<Portal2D>();

        if (red != null && green != null)
        {
            red.targetPortal = green;
            green.targetPortal = red;
            Debug.Log("传送门已连接: 红 <-> 绿");
        }
    }
}
