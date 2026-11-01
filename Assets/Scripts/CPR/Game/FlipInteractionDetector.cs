using UnityEngine;

namespace CPRSimulation.Game
{
    /// <summary>
    /// 检测玩家手部触碰患者身体，触发翻转
    /// </summary>
    public class FlipInteractionDetector : MonoBehaviour
    {
        public System.Action onInteracted;

        [Tooltip("手部专用 Layer；-1 表示不按层过滤，交给名称/层级关系判定。")]
        public LayerMask handLayerMask = -1;

        private void OnTriggerEnter(Collider other)
        {
            // 必须先确认是手。以前这里什么都不检查，患者自己的肢体碰撞体、
            // 地面、任何东西进来都算翻转，基本等于常触发。
            // 现在走和按压检测同一套判定。
            if (!CPRSimulation.HandColliderUtil.IsHandCollider(other, handLayerMask))
                return;

            Debug.Log($"[FlipDetector] 手部触碰检测: {other.gameObject.name}");
            onInteracted?.Invoke();
        }

        private void OnMouseDown()
        {
            // PC 鼠标点击也可以触发
            Debug.Log("[FlipDetector] 鼠标点击触发翻转");
            onInteracted?.Invoke();
        }
    }
}
