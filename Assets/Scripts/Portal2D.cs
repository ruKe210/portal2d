using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D Portal —— 物体穿过传送门时逐渐消失，从另一个门逐渐出现。
/// 
/// 裁剪原理：用 clip plane shader 按传送门所在的线裁剪像素。
/// 跟传送门宽度无关，哪怕传送门只有 0.01 宽也能正确裁剪。
/// 
/// 传送门需要 BoxCollider2D (IsTrigger=true)。
/// 互相指定 targetPortal。
/// </summary>
public class Portal2D : MonoBehaviour
{
    [Header("传送门设置")]
    public Portal2D targetPortal;

    [Tooltip("传送门法线方向（物体从正面进入）")]
    public Vector2 portalNormal = Vector2.right;

    [Tooltip("传送冷却时间（秒），0 表示无冷却")]
    public float cooldown = 0f;

    /// <summary>
    /// 传送门依附的墙壁碰撞体，由 Player.SpawnPortal 设置。
    /// 玩家进入传送门时会临时忽略与此碰撞体的碰撞。
    /// </summary>
    [HideInInspector] public Collider2D wallCollider;

    private float lastTeleportTime = -999f;
    private Dictionary<Collider2D, Traveller> travellers
        = new Dictionary<Collider2D, Traveller>();

    private class Traveller
    {
        public GameObject original;
        public GameObject clone;
        public SpriteRenderer originalRenderer;
        public SpriteRenderer cloneRenderer;
        public Material savedMaterial;
        public Material clipMatSrc;  // 原始物体的裁剪材质
        public Material clipMatDst;  // 克隆体的裁剪材质
        public Rigidbody2D rb;
        public int enteredSide;
        public Collider2D ignoredWall;       // 源门的墙
        public Collider2D ignoredWallTarget; // 目标门的墙
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (targetPortal == null) return;
        if (travellers.ContainsKey(other)) return;
        if (other.GetComponent<Bullet>() != null) return;
        if (Time.time < lastTeleportTime + cooldown) return;
        SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Traveller t = new Traveller();
        t.original = other.gameObject;
        t.originalRenderer = sr;
        t.rb = other.GetComponent<Rigidbody2D>();
        t.savedMaterial = new Material(sr.material);
        t.enteredSide = GetSide(other.transform.position);

        // 克隆体
        t.clone = new GameObject(other.name + "_Clone");
        t.cloneRenderer = t.clone.AddComponent<SpriteRenderer>();
        CopySpriteRenderer(sr, t.cloneRenderer);

        // 创建裁剪材质
        Shader clipShader = Shader.Find("Portal/ClipPlane");
        if (clipShader != null)
        {
            Vector2 n = portalNormal.normalized;
            Vector2 tn = targetPortal.portalNormal.normalized;
            Vector3 pos = transform.position;
            Vector3 tpos = targetPortal.transform.position;

            // 原始物体：保留法线正面（还没进门的部分）
            t.clipMatSrc = new Material(clipShader);
            t.clipMatSrc.SetTexture("_MainTex", sr.sprite.texture);
            t.clipMatSrc.SetVector("_ClipPos", pos);
            t.clipMatSrc.SetVector("_ClipNormal", n);
            t.clipMatSrc.SetFloat("_ClipSide", 1f);

            // 克隆体：保留目标门法线的背面（从门里冒出来的部分）
            // 目标门法线指向外面，克隆体要显示的是"已经出来的部分"
            // 即在目标门法线正面的部分
            t.clipMatDst = new Material(clipShader);
            t.clipMatDst.SetTexture("_MainTex", sr.sprite.texture);
            t.clipMatDst.SetVector("_ClipPos", tpos);
            t.clipMatDst.SetVector("_ClipNormal", tn);
            t.clipMatDst.SetFloat("_ClipSide", 1f);

            sr.material = t.clipMatSrc;
            t.cloneRenderer.material = t.clipMatDst;
        }
        travellers[other] = t;

        // 临时忽略两个传送门的墙壁碰撞
        if (wallCollider != null)
        {
            Physics2D.IgnoreCollision(other, wallCollider, true);
            t.ignoredWall = wallCollider;
        }
        if (targetPortal.wallCollider != null)
        {
            Physics2D.IgnoreCollision(other, targetPortal.wallCollider, true);
            t.ignoredWallTarget = targetPortal.wallCollider;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!travellers.TryGetValue(other, out Traveller t)) return;

        int exitSide = GetSide(other.transform.position);
        if (exitSide != 0 && t.enteredSide != 0 && exitSide != t.enteredSide)
        {
            Teleport(t);
        }
        Cleanup(other, t);
    }

    void LateUpdate()
    {
        List<Collider2D> toRemove = null;
        foreach (var kvp in travellers)
        {
            Collider2D col = kvp.Key;
            Traveller t = kvp.Value;
            if (col == null || t.original == null)
            {
                if (toRemove == null) toRemove = new List<Collider2D>();
                toRemove.Add(col);
                continue;
            }
            UpdateClone(t);
            SyncSprite(t);
        }
        if (toRemove != null)
            foreach (var col in toRemove)
                if (travellers.TryGetValue(col, out Traveller t))
                    Cleanup(col, t);
    }

    void UpdateClone(Traveller t)
    {
        if (t.clone == null || targetPortal == null) return;
        Vector3 lp = transform.InverseTransformPoint(t.original.transform.position);
        t.clone.transform.position = targetPortal.transform.TransformPoint(lp);
        t.clone.transform.localScale = t.original.transform.localScale;
    }

    void SyncSprite(Traveller t)
    {
        if (t.cloneRenderer == null || t.originalRenderer == null) return;
        t.cloneRenderer.sprite = t.originalRenderer.sprite;
        t.cloneRenderer.flipX = t.originalRenderer.flipX;
        t.cloneRenderer.flipY = t.originalRenderer.flipY;
        t.cloneRenderer.sortingLayerID = t.originalRenderer.sortingLayerID;
        t.cloneRenderer.sortingOrder = t.originalRenderer.sortingOrder;
        // 同步 texture（sprite 可能变了）
        if (t.clipMatDst != null && t.originalRenderer.sprite != null)
            t.clipMatDst.SetTexture("_MainTex", t.originalRenderer.sprite.texture);
        if (t.clipMatSrc != null && t.originalRenderer.sprite != null)
            t.clipMatSrc.SetTexture("_MainTex", t.originalRenderer.sprite.texture);
    }

    int GetSide(Vector3 worldPos)
    {
        Vector2 diff = (Vector2)worldPos - (Vector2)transform.position;
        float dot = Vector2.Dot(diff, portalNormal.normalized);
        return dot >= 0 ? 1 : -1;
    }

    void Teleport(Traveller t)
    {
        if (t.original == null || targetPortal == null) return;

        Vector3 lp = transform.InverseTransformPoint(t.original.transform.position);
        t.original.transform.position = targetPortal.transform.TransformPoint(lp);

        if (t.rb != null)
        {
            Vector2 lv = transform.InverseTransformDirection(t.rb.velocity);
            t.rb.velocity = targetPortal.transform.TransformDirection(lv);
        }

        lastTeleportTime = Time.time;
        targetPortal.lastTeleportTime = Time.time;
    }

    void Cleanup(Collider2D col, Traveller t)
    {
        if (t.originalRenderer != null && t.savedMaterial != null)
            t.originalRenderer.material = t.savedMaterial;
        if (t.clone != null) Destroy(t.clone);
        if (t.clipMatSrc != null) Destroy(t.clipMatSrc);
        if (t.clipMatDst != null) Destroy(t.clipMatDst);

        // 恢复墙壁碰撞
        if (col != null)
        {
            if (t.ignoredWall != null)
                Physics2D.IgnoreCollision(col, t.ignoredWall, false);
            if (t.ignoredWallTarget != null)
                Physics2D.IgnoreCollision(col, t.ignoredWallTarget, false);
        }

        travellers.Remove(col);
    }

    void CopySpriteRenderer(SpriteRenderer src, SpriteRenderer dst)
    {
        dst.sprite = src.sprite;
        dst.color = src.color;
        dst.flipX = src.flipX;
        dst.flipY = src.flipY;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder = src.sortingOrder;
    }

    void OnDestroy()
    {
        foreach (var kvp in travellers)
        {
            if (kvp.Value.clone != null) Destroy(kvp.Value.clone);
            if (kvp.Value.clipMatSrc != null) Destroy(kvp.Value.clipMatSrc);
            if (kvp.Value.clipMatDst != null) Destroy(kvp.Value.clipMatDst);
        }
        travellers.Clear();
    }

    void OnDrawGizmos()
    {
        if (targetPortal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPortal.transform.position);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, (Vector3)(portalNormal.normalized) * 1.5f);
        // 画传送门线
        Gizmos.color = Color.magenta;
        Vector2 tangent = new Vector2(-portalNormal.y, portalNormal.x).normalized;
        Gizmos.DrawLine(
            transform.position + (Vector3)(tangent * 0.5f),
            transform.position - (Vector3)(tangent * 0.5f));
    }
}
