using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// 「这个碰撞体是不是学员的手」的统一判定。
    ///
    /// 原本只存在于 CPRPressDetector 里（私有方法）。新增意识确认的肩部检测时
    /// 需要同一套判定 —— 复制一份必然随时间走样，所以抽到这里，
    /// CPRPressDetector 改成调用它，两处永远一致。
    ///
    /// 反面教材是 FlipInteractionDetector：它的 OnTriggerEnter 里什么都不检查，
    /// 任何碰撞体进来都算学员触碰，基本等于常触发。新代码不要那样写。
    /// </summary>
    public static class HandColliderUtil
    {
        /// <param name="handLayerMask">手部专用 Layer。-1 表示不按层过滤。</param>
        public static bool IsHandCollider(Collider col, LayerMask handLayerMask)
        {
            if (col == null)
                return false;

            // 检查层级（若设置了手部专用 Layer）
            if (handLayerMask != -1 && ((1 << col.gameObject.layer) & handLayerMask) == 0)
                return false;

            // Meta/OVR：碰撞体在 LeftHandAnchor / RightHandAnchor 下即视为手
            Transform t = col.transform;
            while (t != null)
            {
                string p = t.name;
                if (p.Contains("LeftHandAnchor") || p.Contains("RightHandAnchor") ||
                    p.Contains("HandAnchor") || p.Contains("HandTracking"))
                    return true;
                t = t.parent;
            }

            // 通过名称或 tag 判断
            string name = col.name.ToLower();
            if (name.Contains("hand") || name.Contains("palm") || name.Contains("hands") ||
                name.Contains("finger") || name.Contains("poke") || name.Contains("wrist") ||
                name.Contains("bone") || name.Contains("customhand"))
                return true;
            if (col.CompareTag("Hand") || col.CompareTag("Player"))
                return true;

            return false;
        }
    }
}
