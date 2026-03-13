using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("传送门设置")]
    [Tooltip("目标传送门")]
    public Portal targetPortal;

    [Tooltip("传送偏移距离（避免重复触发）")]
    public float teleportOffset = 1f;

    [Tooltip("传送冷却时间")]
    public float cooldownTime = 0.5f;

    private bool canTeleport = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否是玩家，且可以传送，且目标传送门存在
        Debug.Log("触发传送门: " + other.gameObject.name);
        if (canTeleport && targetPortal != null)
        {
            TeleportPlayer(other.gameObject);
        }
    }
    // void OnTriggerStay2D(Collider2D collision)
    // {
    //     Debug.Log("触发传送门: " + collision.gameObject.name);
    //     if (canTeleport && targetPortal != null)
    //     {
    //         TeleportPlayer(collision.gameObject);
    //     }
    // }
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("触发传送门: " + collision.gameObject.name);
        if (canTeleport && targetPortal != null)
        {
            TeleportPlayer(collision.gameObject);
        }
    }


    void TeleportPlayer(GameObject player)
    {
        // 计算传送位置（目标传送门位置 + 偏移）
        Vector3 teleportPosition = targetPortal.transform.position;

        // 根据传送门的朝向添加偏移，避免玩家卡在传送门里
        if(Mathf.Abs(targetPortal.transform.position.x-this.transform.position.x) > Mathf.Abs(targetPortal.transform.position.y-this.transform.position.y))
        {
            if (targetPortal.transform.localScale.x > 0)
            {
                teleportPosition += Vector3.right * teleportOffset;
            }
            else
            {
                teleportPosition += Vector3.left * teleportOffset;
            }
        }
        else
        {
            if(targetPortal.transform.localScale.y < 0)
            {
                // teleportPosition += Vector3.up * teleportOffset;
            }
            else
            {
                teleportPosition += Vector3.down * teleportOffset;
            }
        }

        // 传送玩家
        player.transform.position = teleportPosition;

        // 禁用双方传送门的触发，避免来回传送
        StartCoroutine(TeleportCooldown());
        StartCoroutine(targetPortal.TeleportCooldown());

        Debug.Log($"玩家从 {gameObject.name} 传送到 {targetPortal.gameObject.name}");
    }

    public IEnumerator TeleportCooldown()
    {
        canTeleport = false;
        yield return new WaitForSeconds(cooldownTime);
        canTeleport = true;
    }

    // 可视化传送门连接（在编辑器中显示）
    void OnDrawGizmos()
    {
        if (targetPortal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPortal.transform.position);
        }
    }
}