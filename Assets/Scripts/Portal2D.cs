using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D Portal 传送门 —— 物体穿过时在两个传送门都能看到模型。
/// 
/// 使用方法：
/// 1. 传送门物体需要一个 BoxCollider2D (IsTrigger=true) 作为传送区域
/// 2. 传送门物体需要一个 SpriteRenderer（用于 Stencil Mask 的形状）
/// 3. 在 Inspector 中互相指定 targetPortal
/// 4. 传送门的 SpriteRenderer 会被自动设置为 Stencil Mask 材质
/// </summary>
public class Portal2D : MonoBehaviour
{
    [Header("传送门设置")]
    public Portal2D targetPortal;

    [Tooltip("传送门法线方向（物体从哪个方向进入）")]
    public Vector2 portalNormal = Vector2.right;

    [Tooltip("传送完成的穿越阈值")]
    public float teleportThreshold = 0.1f;

    [Header("Stencil 设置")]
    [Tooltip("此传送门的 Stencil ID（两个传送门需要不同的值）")]
    public int stencilId = 1;

    // 运行时数据
    private Material stencilMaskMaterial;
    private Dictionary<Collider2D, PortalTraveller> travellers = new Dictionary<Collider2D, PortalTraveller>();

    /// <summary>
    /// 记录每个正在穿越传送门的物体的信息
    /// </summary>
    private class PortalTraveller
    {
        public GameObject original;
        public GameObject clone;
        public SpriteRenderer originalRenderer;
        public SpriteRenderer cloneRenderer;
        public Material originalMaterial;     // 物体原始材质（备份）
        public Material clippedMaterialSrc;   // 源传送门侧裁剪材质
        public Material clippedMaterialDst;   // 目标传送门侧裁剪材质
        public Rigidbody2D rb;
        public int previousSide;              // 上一帧在传送门的哪一侧 (1=正面, -1=背面)
    }

    void Awake()
    {
        SetupStencilMask();
    }

    /// <summary>
    /// 将传送门自身的 SpriteRenderer 设置为 Stencil Mask
    /// </summary>
    void SetupStencilMask()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Shader maskShader = Shader.Find("Portal/StencilMask");
        if (maskShader == null)
        {
            Debug.LogError("Portal2D: 找不到 Portal/StencilMask shader!");
            return;
        }

        stencilMaskMaterial = new Material(maskShader);
        stencilMaskMaterial.SetInt("_StencilRef", stencilId);
        sr.material = stencilMaskMaterial;
        // 确保 mask 在 sprite 之前渲染
        sr.sortingOrder = -100;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (targetPortal == null) return;
        if (travellers.ContainsKey(other)) return;

        SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        PortalTraveller t = new PortalTraveller();
        t.original = other.gameObject;
        t.originalRenderer = sr;
        t.rb = other.GetComponent<Rigidbody2D>();
        t.originalMaterial = sr.sharedMaterial; // 备份原始材质

        // 判断物体当前在传送门的哪一侧
        t.previousSide = GetSide(other.transform.position);

        // --- 创建克隆体 ---
        t.clone = new GameObject(other.gameObject.name + "_PortalClone");
        t.cloneRenderer = t.clone.AddComponent<SpriteRenderer>();
        CopySpriteRenderer(sr, t.cloneRenderer);

        // --- 创建裁剪材质 ---
        Shader clippedShader = Shader.Find("Portal/ClippedSprite");
        if (clippedShader != null)
        {
            // 源传送门侧：只在本传送门 stencil 区域内显示（物体还没进去的部分）
            // 实际上我们用反向逻辑：物体在源传送门这边不裁剪，在目标传送门那边裁剪显示
            // 
            // 更简单的方案：
            // - 原始物体：正常渲染（不裁剪），但我们需要裁掉"已经进入传送门的部分"
            // - 克隆体：只在目标传送门的 stencil 区域内渲染
            //
            // 为了实现"原始物体被传送门裁剪"，我们让原始物体也用 stencil：
            // 原始物体：Stencil Ref=本传送门ID, Comp=NotEqual（不在传送门区域内的部分才渲染）
            // 克隆体：Stencil Ref=目标传送门ID, Comp=Equal（只在目标传送门区域内渲染）

            t.clippedMaterialSrc = new Material(clippedShader);
            t.clippedMaterialSrc.SetTexture("_MainTex", sr.sprite.texture);
            t.clippedMaterialSrc.SetInt("_StencilRef", stencilId);
            t.clippedMaterialSrc.SetInt("_StencilComp", 6); // NotEqual - 传送门区域外才渲染

            t.clippedMaterialDst = new Material(clippedShader);
            t.clippedMaterialDst.SetTexture("_MainTex", sr.sprite.texture);
            t.clippedMaterialDst.SetInt("_StencilRef", targetPortal.stencilId);
            t.clippedMaterialDst.SetInt("_StencilComp", 3); // Equal - 只在目标传送门区域内渲染

            // 应用材质
            sr.material = t.clippedMaterialSrc;
            t.cloneRenderer.material = t.clippedMaterialDst;
        }

        travellers[other] = t;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (travellers.TryGetValue(other, out PortalTraveller t))
        {
            CleanupTraveller(other, t);
        }
    }

    void LateUpdate()
    {
        // 每帧更新所有正在穿越的物体
        List<Collider2D> toRemove = null;

        foreach (var kvp in travellers)
        {
            Collider2D col = kvp.Key;
            PortalTraveller t = kvp.Value;

            if (col == null || t.original == null)
            {
                if (toRemove == null) toRemove = new List<Collider2D>();
                toRemove.Add(col);
                continue;
            }

            // 更新克隆体的位置（镜像映射到目标传送门）
            UpdateCloneTransform(t);

            // 同步精灵状态
            SyncSprite(t);

            // 检测物体是否穿过了传送门中线
            int currentSide = GetSide(t.original.transform.position);
            if (currentSide != 0 && t.previousSide != 0 && currentSide != t.previousSide)
            {
                // 物体穿过了传送门！执行传送
                DoTeleport(t);
                if (toRemove == null) toRemove = new List<Collider2D>();
                toRemove.Add(col);
            }
            else if (currentSide != 0)
            {
                t.previousSide = currentSide;
            }
        }

        if (toRemove != null)
        {
            foreach (var col in toRemove)
            {
                if (travellers.TryGetValue(col, out PortalTraveller t))
                {
                    CleanupTraveller(col, t);
                }
            }
        }
    }

    /// <summary>
    /// 将物体的位置从源传送门映射到目标传送门
    /// </summary>
    void UpdateCloneTransform(PortalTraveller t)
    {
        if (t.clone == null || targetPortal == null) return;

        // 计算物体相对于本传送门的局部坐标
        Vector3 localPos = transform.InverseTransformPoint(t.original.transform.position);

        // 旋转180度（从传送门另一边出来）
        // 对于2D，我们翻转法线方向的分量
        localPos.x = -localPos.x;

        // 转换到目标传送门的世界坐标
        t.clone.transform.position = targetPortal.transform.TransformPoint(localPos);
        t.clone.transform.localScale = t.original.transform.localScale;
    }

    /// <summary>
    /// 同步克隆体的精灵状态
    /// </summary>
    void SyncSprite(PortalTraveller t)
    {
        if (t.cloneRenderer == null || t.originalRenderer == null) return;

        t.cloneRenderer.sprite = t.originalRenderer.sprite;
        t.cloneRenderer.flipX = t.originalRenderer.flipX;
        t.cloneRenderer.flipY = t.originalRenderer.flipY;
        t.cloneRenderer.sortingLayerID = t.originalRenderer.sortingLayerID;
        t.cloneRenderer.sortingOrder = t.originalRenderer.sortingOrder;
    }

    /// <summary>
    /// 判断位置在传送门的哪一侧
    /// 返回 1=正面（portalNormal 方向），-1=背面
    /// </summary>
    int GetSide(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        // portalNormal 在局部空间中默认是 right (1,0)
        // 所以我们检查 localPos.x 的符号
        Vector2 localNormal = portalNormal.normalized;
        float dot = localPos.x * localNormal.x + localPos.y * localNormal.y;
        return dot >= 0 ? 1 : -1;
    }

    /// <summary>
    /// 执行传送：将物体移动到目标传送门对应位置
    /// </summary>
    void DoTeleport(PortalTraveller t)
    {
        if (t.original == null || targetPortal == null) return;

        // 计算物体相对于本传送门的局部坐标
        Vector3 localPos = transform.InverseTransformPoint(t.original.transform.position);
        localPos.x = -localPos.x;
        Vector3 newWorldPos = targetPortal.transform.TransformPoint(localPos);

        // 传送物体
        t.original.transform.position = newWorldPos;

        // 如果有刚体，转换速度方向
        if (t.rb != null)
        {
            // 将速度从源传送门局部空间转换到目标传送门局部空间
            Vector2 localVel = transform.InverseTransformDirection(t.rb.velocity);
            localVel.x = -localVel.x; // 翻转法线方向的速度分量
            t.rb.velocity = targetPortal.transform.TransformDirection(localVel);
        }

        Debug.Log($"Portal2D: {t.original.name} 从 {gameObject.name} 传送到 {targetPortal.gameObject.name}");
    }

    /// <summary>
    /// 清理穿越者数据，恢复原始材质，销毁克隆体
    /// </summary>
    void CleanupTraveller(Collider2D col, PortalTraveller t)
    {
        // 恢复原始材质
        if (t.originalRenderer != null && t.originalMaterial != null)
        {
            t.originalRenderer.material = t.originalMaterial;
        }

        // 销毁克隆体
        if (t.clone != null)
        {
            Destroy(t.clone);
        }

        // 销毁临时材质
        if (t.clippedMaterialSrc != null) Destroy(t.clippedMaterialSrc);
        if (t.clippedMaterialDst != null) Destroy(t.clippedMaterialDst);

        travellers.Remove(col);
    }

    void CopySpriteRenderer(SpriteRenderer source, SpriteRenderer dest)
    {
        dest.sprite = source.sprite;
        dest.color = source.color;
        dest.flipX = source.flipX;
        dest.flipY = source.flipY;
        dest.sortingLayerID = source.sortingLayerID;
        dest.sortingOrder = source.sortingOrder;
    }

    void OnDestroy()
    {
        // 清理所有穿越者
        foreach (var kvp in travellers)
        {
            if (kvp.Value.clone != null) Destroy(kvp.Value.clone);
            if (kvp.Value.clippedMaterialSrc != null) Destroy(kvp.Value.clippedMaterialSrc);
            if (kvp.Value.clippedMaterialDst != null) Destroy(kvp.Value.clippedMaterialDst);
        }
        travellers.Clear();

        if (stencilMaskMaterial != null) Destroy(stencilMaskMaterial);
    }

    void OnDrawGizmos()
    {
        // 绘制传送门连接
        if (targetPortal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPortal.transform.position);
        }

        // 绘制法线方向
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, (Vector3)(portalNormal.normalized) * 1.5f);

        // 绘制传送门平面
        Gizmos.color = Color.magenta;
        Vector2 tangent = new Vector2(-portalNormal.y, portalNormal.x).normalized;
        Gizmos.DrawLine(
            transform.position + (Vector3)(tangent * 0.5f),
            transform.position - (Vector3)(tangent * 0.5f)
        );
    }
}
