using UnityEngine;
using UnityEngine.Rendering;

namespace VRFirstAid.Core
{
    /// <summary>
    /// 性能优化器 - 针对Quest平台的性能优化设置
    /// </summary>
    public class PerformanceOptimizer : MonoBehaviour
    {
        [Header("Target Frame Rate")]
        [SerializeField] private int targetFrameRate = 72; // Quest 2/3 目标帧率

        [Header("Quality Settings")]
        [SerializeField] private bool enableVSync = true;
        [SerializeField] private int maxLODLevel = 1; // 限制LOD级别

        [Header("Rendering")]
        [SerializeField] private bool useSinglePassInstanced = true; // 单通道实例化渲染
        [SerializeField] private bool enableOcclusionCulling = true;

        [Header("Audio")]
        [SerializeField] private int maxAudioSources = 32; // 限制音频源数量

        private void Start()
        {
            // 延迟一帧执行，避免 VR 启动时 Camera.main 尚未就绪导致异常
            StartCoroutine(DeferredOptimize());
        }

        private System.Collections.IEnumerator DeferredOptimize()
        {
            yield return null;
            OptimizeForQuest();
        }

        /// <summary>
        /// 针对Quest平台优化
        /// </summary>
        private void OptimizeForQuest()
        {
            // 设置目标帧率
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = enableVSync ? 1 : 0;

            // 设置LOD级别
            QualitySettings.maximumLODLevel = maxLODLevel;

            // 启用遮挡剔除（VR 下 Camera.main 可能为 null，需判空）
            if (enableOcclusionCulling)
            {
                var cam = Camera.main;
                if (cam != null)
                    cam.useOcclusionCulling = true;
            }

            // 音频缓冲：SetDSPBufferSize 在 Android 上已弃用，可能卡住启动，此处不再调用

            // 优化渲染设置
            if (useSinglePassInstanced)
            {
                // 单通道实例化渲染已在XR设置中配置
                Debug.Log("Single Pass Instanced rendering enabled");
            }

            // 禁用不必要的功能
            QualitySettings.shadows = ShadowQuality.HardOnly; // 只使用硬阴影
            QualitySettings.shadowDistance = 20f; // 减少阴影距离
            QualitySettings.shadowResolution = ShadowResolution.Low;

            // 优化纹理设置
            QualitySettings.globalTextureMipmapLimit = 1; // 限制纹理Mipmap级别

            Debug.Log($"Performance optimized for Quest - Target FPS: {targetFrameRate}");
        }

        /// <summary>
        /// 动态调整质量设置
        /// </summary>
        public void AdjustQualityBasedOnPerformance(float currentFPS)
        {
            if (currentFPS < targetFrameRate * 0.9f) // 如果帧率低于目标的90%
            {
                // 降低质量
                QualitySettings.maximumLODLevel = Mathf.Min(QualitySettings.maximumLODLevel + 1, 2);
                QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance - 5f, 10f);
                Debug.Log("Performance degraded - reducing quality");
            }
            else if (currentFPS >= targetFrameRate * 1.1f) // 如果帧率高于目标的110%
            {
                // 可以提高质量
                QualitySettings.maximumLODLevel = Mathf.Max(QualitySettings.maximumLODLevel - 1, 0);
                Debug.Log("Performance good - maintaining quality");
            }
        }

        /// <summary>
        /// 限制对象数量（对象池管理）
        /// </summary>
        public void LimitObjectCount(GameObject[] objects, int maxCount)
        {
            if (objects.Length > maxCount)
            {
                for (int i = maxCount; i < objects.Length; i++)
                {
                    if (objects[i] != null)
                    {
                        Destroy(objects[i]);
                    }
                }
            }
        }
    }
}
