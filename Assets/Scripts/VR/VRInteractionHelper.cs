using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRFirstAid.VR
{
    /// <summary>
    /// VR交互辅助类 - 提供常用的VR交互功能
    /// </summary>
    public static class VRInteractionHelper
    {
        /// <summary>
        /// 高亮显示可交互对象
        /// </summary>
        public static void HighlightInteractable(IXRInteractable interactable, bool highlight)
        {
            if (interactable is MonoBehaviour monoBehaviour)
            {
                var renderer = monoBehaviour.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // 可以添加高亮材质或改变颜色
                    if (highlight)
                    {
                        // 设置高亮效果
                        renderer.material.EnableKeyword("_EMISSION");
                        renderer.material.SetColor("_EmissionColor", Color.yellow * 0.5f);
                    }
                    else
                    {
                        // 移除高亮效果
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                }
            }
        }

        /// <summary>
        /// 检查对象是否可以被抓取
        /// </summary>
        public static bool CanGrab(GameObject obj)
        {
            return obj.GetComponent<XRGrabInteractable>() != null;
        }

        /// <summary>
        /// 获取交互管理器
        /// </summary>
        public static XRInteractionManager GetInteractionManager()
        {
            return Object.FindObjectOfType<XRInteractionManager>();
        }

        /// <summary>
        /// 创建可交互对象
        /// </summary>
        public static void MakeInteractable(GameObject obj, bool canGrab = true)
        {
            if (canGrab && obj.GetComponent<XRGrabInteractable>() == null)
            {
                obj.AddComponent<XRGrabInteractable>();
            }
        }
    }
}
