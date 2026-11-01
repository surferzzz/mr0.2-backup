using System.Collections;
using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// Quest 3 注视点渲染（Fixed Foveated Rendering）。
    ///
    /// 在 OpenXR 里勾上 MetaXRFoveationFeature 只是「允许」这项功能，
    /// 级别默认仍是 Off —— 必须在运行时设置才真正生效。
    ///
    /// 而且不能在 Start() 里直接设：MetaXRFoveationFeature 的 setter 依赖
    /// OnSessionCreate() 拿到的 _xrSession，会话还没建立时它是 0，
    /// 调用会静默失败。所以这里轮询 OVRManager.fixedFoveatedRenderingSupported
    /// （OpenXR 路径下它会检查所需扩展是否已启用），就绪后设置并回读校验。
    ///
    /// 用 RuntimeInitializeOnLoadMethod 自举，不需要往场景里挂东西。
    /// </summary>
    [AddComponentMenu("CPR/Quest Foveated Rendering")]
    public class QuestFoveatedRendering : MonoBehaviour
    {
        private const string RuntimeRootName = "QuestFoveatedRendering";

        [Tooltip("注视点渲染级别。Quest 3 上 High 通常能省下可观的 GPU 时间，" +
                 "画质损失集中在视野边缘，VR 里基本感知不到。")]
        public OVRManager.FoveatedRenderingLevel level = OVRManager.FoveatedRenderingLevel.High;

        [Tooltip("动态注视点渲染：按 GPU 负载自动升降级别，负载高时更不容易掉帧。")]
        public bool dynamic = true;

        [Tooltip("等待 XR 会话就绪的最长时间（秒）。")]
        public float sessionTimeout = 15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<QuestFoveatedRendering>() != null)
            {
                return;
            }

            var go = new GameObject(RuntimeRootName);
            go.AddComponent<QuestFoveatedRendering>();
            DontDestroyOnLoad(go);   // 注视点渲染是全局渲染设置，一个应用只需设一次
        }

        private IEnumerator Start()
        {
            float deadline = Time.unscaledTime + sessionTimeout;

            while (Time.unscaledTime < deadline && !OVRManager.fixedFoveatedRenderingSupported)
            {
                yield return null;
            }

            if (!OVRManager.fixedFoveatedRenderingSupported)
            {
                // 编辑器里没连头显时会走到这里，属正常。
                Debug.Log("[QuestFoveatedRendering] 注视点渲染不可用：" +
                          "设备/运行时不支持，或 OpenXR 的 MetaXRFoveationFeature 未启用。" +
                          "（编辑器中未连接头显时属正常）");
                yield break;
            }

            OVRManager.useDynamicFoveatedRendering = dynamic;
            OVRManager.foveatedRenderingLevel = level;

            yield return null;   // 等一帧再回读，确认真的写进去了

            var readbackLevel = OVRManager.foveatedRenderingLevel;
            var readbackDynamic = OVRManager.useDynamicFoveatedRendering;

            if (readbackLevel != level)
            {
                Debug.LogWarning($"[QuestFoveatedRendering] 设置为 {level}，但回读是 {readbackLevel}。" +
                                 "HighTop 在 OpenXR 路径下会被映射成 High，属预期；其余情况需排查。");
            }
            else
            {
                Debug.Log($"[QuestFoveatedRendering] 已生效：level={readbackLevel} dynamic={readbackDynamic}");
            }
        }
    }
}
