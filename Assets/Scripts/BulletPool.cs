using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [Header("对象池设置")]
    [Tooltip("子弹预制体")]
    public GameObject bulletPrefab;

    [Tooltip("初始池大小")]
    public int poolSize = 20;

    [Tooltip("是否可扩展（池不够时自动增加）")]
    public bool expandable = true;

    private Queue<GameObject> bulletQueue = new Queue<GameObject>();
    private List<GameObject> activeBullets = new List<GameObject>();

    private static BulletPool instance;
    public static BulletPool Instance => instance;

    void Awake()
    {
        // 单例模式
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 初始化对象池
        InitializePool();
    }

    void InitializePool()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: 请设置子弹预制体！");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            CreateNewBullet();
        }

        Debug.Log($"子弹对象池初始化完成，池大小: {poolSize}");
    }

    GameObject CreateNewBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab);
        bullet.SetActive(false);
        bullet.transform.SetParent(transform); // 设置为对象池的子对象，便于管理
        bulletQueue.Enqueue(bullet);
        return bullet;
    }

    public GameObject GetBullet()
    {
        GameObject bullet;

        // 如果池中有可用子弹
        if (bulletQueue.Count > 0)
        {
            bullet = bulletQueue.Dequeue();
        }
        // 如果池为空且允许扩展
        else if (expandable)
        {
            Debug.Log("对象池已满，创建新子弹");
            bullet = CreateNewBullet();
        }
        else
        {
            Debug.LogWarning("对象池已空且不允许扩展！");
            return null;
        }

        bullet.SetActive(true);
        activeBullets.Add(bullet);
        return bullet;
    }

    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;

        // 重置子弹状态
        bullet.SetActive(false);
        bullet.transform.SetParent(transform);

        // 重置 Rigidbody2D 速度
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 从活跃列表移除，加入队列
        activeBullets.Remove(bullet);
        bulletQueue.Enqueue(bullet);
    }

    // 清空所有活跃的子弹（游戏重置时使用）
    public void ClearAllActiveBullets()
    {
        // 复制列表避免在遍历时修改
        List<GameObject> bulletsToReturn = new List<GameObject>(activeBullets);

        foreach (GameObject bullet in bulletsToReturn)
        {
            ReturnBullet(bullet);
        }
    }

    // 显示对象池状态
    void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"对象池: {bulletQueue.Count} 可用 | {activeBullets.Count} 活跃");
        }
    }
}