using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("射击设置")]
    [Tooltip("子弹预制体（拖入你的子弹模型）")]
    public GameObject bulletPrefab;

    [Tooltip("子弹发射点（可选，不设置则从玩家中心发射）")]
    public Transform firePoint;

    [Tooltip("射击冷却时间（秒）")]
    public float fireRate = 0.2f;

    [Header("音效设置（可选）")]
    public AudioClip shootSound;

    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private Camera mainCamera;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        mainCamera = Camera.main;

        // 如果没有设置发射点，创建一个
        if (firePoint == null)
        {
            // GameObject firePointObj = new GameObject("FirePoint");
            // firePointObj.transform.SetParent(transform);
            // firePointObj.transform.localPosition = Vector3.zero;
            // firePoint = firePointObj.transform;
            firePoint = this.transform;
        }
    }

    void Update()
    {
        // 按下鼠标左键且冷却完成
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        // 获取鼠标世界坐标
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Debug.Log(mousePos);
        // 计算射击方向
        Vector3 shootDirection = (mousePos - firePoint.position).normalized;

        // 计算旋转角度
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

        // 从对象池获取子弹
        GameObject bullet = null;
        if (BulletPool.Instance != null)
        {
            bullet = BulletPool.Instance.GetBullet();
        }
        else if (bulletPrefab != null)
        {
            // 如果没有对象池，使用传统方式（兼容性）
            Debug.LogWarning("未找到 BulletPool，使用 Instantiate 创建子弹");
            bullet = Instantiate(bulletPrefab);
        }
        else
        {
            Debug.LogError("未设置子弹预制体且未找到对象池！");
            return;
        }

        if (bullet == null)
        {
            Debug.LogWarning("无法获取子弹！");
            return;
        }

        // 设置子弹位置和方向
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = Quaternion.Euler(0, 0, 0);
        bullet.GetComponent<Bullet>().direction = shootDirection;

        // 播放射击音效
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        Debug.Log($"射击！方向: {shootDirection}");
    }

    // 可视化射击点
    void OnDrawGizmos()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.1f);
        }
    }
}