using UnityEngine;

namespace CPRSimulation.Game
{
    /// <summary>
    /// 检测玩家手部触碰患者身体，触发翻转
    /// </summary>
    public class FlipInteractionDetector : MonoBehaviour
    {
        public System.Action onInteracted;

        private void OnTriggerEnter(Collider other)
        {
            // VR 手部或控制器碰到了患者
            Debug.Log($"[FlipDetector] 触碰检测: {other.gameObject.name}");
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
