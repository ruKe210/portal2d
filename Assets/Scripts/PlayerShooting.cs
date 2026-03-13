using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("射击设置")]
    public GameObject bulletPrefab;
    public Transform firePoint;
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
        if (firePoint == null)
            firePoint = this.transform;
    }

    void Update()
    {
        // 左键射红门
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            Shoot(isRed: true);
            nextFireTime = Time.time + fireRate;
        }
        // 右键射绿门
        if (Input.GetButtonDown("Fire2") && Time.time >= nextFireTime)
        {
            Shoot(isRed: false);
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot(bool isRed)
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 shootDirection = (mousePos - firePoint.position).normalized;

        GameObject bullet = null;
        if (BulletPool.Instance != null)
            bullet = BulletPool.Instance.GetBullet();
        else if (bulletPrefab != null)
            bullet = Instantiate(bulletPrefab);

        if (bullet == null) return;

        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = Quaternion.identity;

        Bullet b = bullet.GetComponent<Bullet>();
        b.direction = shootDirection;
        b.isRedPortalBullet = isRed;

        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }

    void OnDrawGizmos()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.1f);
        }
    }
}
