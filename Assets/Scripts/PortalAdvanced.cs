using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalAdvanced : MonoBehaviour
{
    [Header("传送门设置")]
    [Tooltip("目标传送门")]
    public PortalAdvanced targetPortal;

    [Tooltip("传送门方向（玩家从哪边进入）")]
    public Vector2 portalDirection = Vector2.right;

    [Header("视觉设置")]
    [Tooltip("传送门的渲染顺序")]
    public int sortingOrder = 100;

    private GameObject playerClone;
    private bool isPlayerInPortal = false;
    private GameObject currentPlayer;
    private SpriteRenderer playerSpriteRenderer;
    private SpriteMask portalMask;

    void Start()
    {
        // 创建传送门遮罩
        SetupPortalMask();
    }

    void SetupPortalMask()
    {
        // 添加 Sprite Mask 组件用于裁剪
        portalMask = GetComponent<SpriteMask>();
        if (portalMask == null)
        {
            portalMask = gameObject.AddComponent<SpriteMask>();
        }
        portalMask.isCustomRangeActive = true;
        portalMask.frontSortingOrder = sortingOrder;
        portalMask.backSortingOrder = sortingOrder - 1;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && targetPortal != null)
        {
            currentPlayer = other.gameObject;
            playerSpriteRenderer = currentPlayer.GetComponent<SpriteRenderer>();
            isPlayerInPortal = true;

            // 在目标传送门创建玩家的克隆体
            CreatePlayerClone();
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isPlayerInPortal && targetPortal != null)
        {
            // 更新玩家克隆体的位置和状态
            UpdatePlayerClone();

            // 检查玩家是否完全穿过传送门
            if (IsPlayerFullyThrough())
            {
                TeleportPlayer();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInPortal = false;
            DestroyPlayerClone();
        }
    }

    void CreatePlayerClone()
    {
        if (targetPortal.playerClone == null)
        {
            // 创建玩家的视觉克隆
            targetPortal.playerClone = new GameObject("PlayerClone");

            // 复制 SpriteRenderer
            SpriteRenderer cloneSpriteRenderer = targetPortal.playerClone.AddComponent<SpriteRenderer>();
            cloneSpriteRenderer.sprite = playerSpriteRenderer.sprite;
            cloneSpriteRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
            cloneSpriteRenderer.sortingOrder = playerSpriteRenderer.sortingOrder;
            cloneSpriteRenderer.flipX = playerSpriteRenderer.flipX;
            cloneSpriteRenderer.color = playerSpriteRenderer.color;

            // 设置克隆体受遮罩影响
            cloneSpriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }

    void UpdatePlayerClone()
    {
        if (targetPortal.playerClone == null || currentPlayer == null) return;

        // 计算玩家相对于传送门的位置
        Vector3 localPos = transform.InverseTransformPoint(currentPlayer.transform.position);

        // 转换到目标传送门的坐标系
        Vector3 targetLocalPos = new Vector3(-localPos.x, localPos.y, localPos.z);
        Vector3 targetWorldPos = targetPortal.transform.TransformPoint(targetLocalPos);

        // 更新克隆体位置
        targetPortal.playerClone.transform.position = targetWorldPos;
        targetPortal.playerClone.transform.rotation = currentPlayer.transform.rotation;
        targetPortal.playerClone.transform.localScale = currentPlayer.transform.localScale;

        // 同步精灵
        SpriteRenderer cloneSpriteRenderer = targetPortal.playerClone.GetComponent<SpriteRenderer>();
        if (cloneSpriteRenderer != null && playerSpriteRenderer != null)
        {
            cloneSpriteRenderer.sprite = playerSpriteRenderer.sprite;
            cloneSpriteRenderer.flipX = playerSpriteRenderer.flipX;
        }
    }

    bool IsPlayerFullyThrough()
    {
        if (currentPlayer == null) return false;

        // 计算玩家中心相对于传送门的位置
        Vector3 relativePos = currentPlayer.transform.position - transform.position;
        float dotProduct = Vector3.Dot(relativePos, portalDirection);

        // 如果玩家已经完全穿过传送门（在传送门背面）
        return dotProduct < -0.5f;
    }

    void TeleportPlayer()
    {
        if (currentPlayer == null || targetPortal == null) return;

        // 传送玩家到克隆体位置
        currentPlayer.transform.position = targetPortal.playerClone.transform.position;

        // 清理克隆体
        DestroyPlayerClone();
        targetPortal.DestroyPlayerClone();

        // 重置状态
        isPlayerInPortal = false;
        targetPortal.isPlayerInPortal = false;

        Debug.Log($"玩家传送完成: {gameObject.name} → {targetPortal.gameObject.name}");
    }

    void DestroyPlayerClone()
    {
        if (playerClone != null)
        {
            Destroy(playerClone);
            playerClone = null;
        }
    }

    void OnDestroy()
    {
        DestroyPlayerClone();
    }

    // 在编辑器中显示传送门方向
    void OnDrawGizmos()
    {
        // 绘制传送门连接线
        if (targetPortal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPortal.transform.position);
        }

        // 绘制传送门方向
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, portalDirection.normalized * 1f);
    }
}