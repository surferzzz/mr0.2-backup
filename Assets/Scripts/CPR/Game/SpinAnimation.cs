using UnityEngine;

namespace CPRSimulation.Game
{
    /// <summary>
    /// 旋转动画 + 上下浮动，用于翻转提示箭头
    /// </summary>
    public class SpinAnimation : MonoBehaviour
    {
        public float spinSpeed = 90f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.1f;

        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPos + Vector3.up * yOffset;
        }
    }
}
