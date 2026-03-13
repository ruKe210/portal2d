using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalSmooth : MonoBehaviour
{
    [Header("传送门设置")]
    [Tooltip("目标传送门")]
    public PortalSmooth targetPortal;

    [Tooltip("传送门朝向（1=右, -1=左）")]
    public float portalDirection = 1f;

    [Header("视觉克隆设置")]
    private GameObject playerClone;
    private Transform currentPlayer;
    private Animator playerAnimator;
    private Animator cloneAnimator;
    private SpriteRenderer playerSpriteRenderer;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && targetPortal != null)
        {
            currentPlayer = other.transform;
            playerSpriteRenderer = currentPlayer.GetComponent<SpriteRenderer>();
            playerAnimator = currentPlayer.GetComponent<Animator>();

            // 在目标传送门创建克隆体
            targetPortal.CreateClone(currentPlayer.gameObject);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && currentPlayer != null && targetPortal != null)
        {
            // 实时更新克隆体位置
            targetPortal.UpdateClone(currentPlayer, transform.position, portalDirection);

            // 检查是否完全穿过
            if (HasPassedThrough(currentPlayer))
            {
                CompleteTeleport();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 如果玩家退出传送门（没有完成传送），销毁克隆
            targetPortal?.DestroyClone();
            currentPlayer = null;
        }
    }

    void CreateClone(GameObject player)
    {
        if (playerClone != null) return;

        // 创建克隆对象
        playerClone = new GameObject("PlayerClone");
        playerClone.transform.position = transform.position;

        // 复制外观
        SpriteRenderer cloneRenderer = playerClone.AddComponent<SpriteRenderer>();
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();

        cloneRenderer.sprite = playerRenderer.sprite;
        cloneRenderer.sortingLayerName = playerRenderer.sortingLayerName;
        cloneRenderer.sortingOrder = playerRenderer.sortingOrder;
        cloneRenderer.flipX = playerRenderer.flipX;
        cloneRenderer.color = playerRenderer.color;

        // 如果玩家有动画，复制动画控制器
        Animator playerAnim = player.GetComponent<Animator>();
        if (playerAnim != null && playerAnim.runtimeAnimatorController != null)
        {
            cloneAnimator = playerClone.AddComponent<Animator>();
            cloneAnimator.runtimeAnimatorController = playerAnim.runtimeAnimatorController;
        }
    }

    void UpdateClone(Transform player, Vector3 sourcePortalPos, float sourceDirection)
    {
        if (playerClone == null || player == null) return;

        // 计算玩家相对于源传送门的偏移
        Vector3 offset = player.position - sourcePortalPos;

        // 镜像 X 轴偏移（因为从另一边出来）
        offset.x = -offset.x;

        // 应用到克隆体
        playerClone.transform.position = transform.position + offset;
        playerClone.transform.localScale = player.localScale;

        // 同步精灵和动画
        SpriteRenderer cloneRenderer = playerClone.GetComponent<SpriteRenderer>();
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();

        if (cloneRenderer != null && playerRenderer != null)
        {
            cloneRenderer.sprite = playerRenderer.sprite;
            cloneRenderer.flipX = playerRenderer.flipX;
        }

        // 同步动画状态
        if (cloneAnimator != null && playerAnimator != null)
        {
            AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
            cloneAnimator.Play(stateInfo.fullPathHash, 0, stateInfo.normalizedTime);
        }

        // 根据玩家穿过传送门的程度，调整透明度
        float distanceThrough = Mathf.Abs(offset.x);
        float alpha = Mathf.Clamp01(distanceThrough / 1f); // 1f 是传送门宽度

        if (cloneRenderer != null)
        {
            Color color = cloneRenderer.color;
            color.a = alpha;
            cloneRenderer.color = color;
        }

        // 同时调整原始玩家的透明度（相反）
        if (playerRenderer != null)
        {
            Color playerColor = playerRenderer.color;
            playerColor.a = 1f - alpha * 0.5f; // 不完全透明，保持可见
            playerRenderer.color = playerColor;
        }
    }

    bool HasPassedThrough(Transform player)
    {
        if (player == null) return false;

        // 计算玩家相对传送门的位置
        float relativeX = (player.position.x - transform.position.x) * portalDirection;

        // 如果玩家已经穿过传送门（相对位置为负）
        return relativeX < -0.3f;
    }

    void CompleteTeleport()
    {
        if (currentPlayer == null || targetPortal.playerClone == null) return;

        // 传送玩家到克隆体位置
        Vector3 targetPosition = targetPortal.playerClone.transform.position;
        currentPlayer.position = targetPosition;

        // 恢复玩家透明度
        SpriteRenderer playerRenderer = currentPlayer.GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            Color color = playerRenderer.color;
            color.a = 1f;
            playerRenderer.color = color;
        }

        // 清理
        targetPortal.DestroyClone();
        currentPlayer = null;

        Debug.Log($"传送完成: {gameObject.name} → {targetPortal.gameObject.name}");
    }

    void DestroyClone()
    {
        if (playerClone != null)
        {
            Destroy(playerClone);
            playerClone = null;
        }
    }

    void OnDestroy()
    {
        DestroyClone();
    }

    // 编辑器可视化
    void OnDrawGizmos()
    {
        if (targetPortal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPortal.transform.position);
        }

        Gizmos.color = Color.green;
        Vector3 direction = portalDirection > 0 ? Vector3.right : Vector3.left;
        Gizmos.DrawRay(transform.position, direction * 1f);
    }
}