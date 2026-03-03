using UnityEngine;

namespace CPRSimulation.Game
{
    /// <summary>
    /// 让检测区域有脉冲发光效果，提示玩家按压位置
    /// </summary>
    public class DetectionZonePulse : MonoBehaviour
    {
        private Renderer rend;
        private Color baseColor = new Color(1f, 0.15f, 0.15f, 0.6f);
        private float pulseSpeed = 2f;

        private void Start()
        {
            rend = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (rend == null) return;

            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.3f, 0.8f, pulse);
            float brightness = Mathf.Lerp(0.8f, 1.2f, pulse);

            rend.material.color = new Color(
                baseColor.r * brightness,
                baseColor.g * brightness * 0.5f,
                baseColor.b * brightness * 0.5f,
                alpha
            );
        }
    }
}
