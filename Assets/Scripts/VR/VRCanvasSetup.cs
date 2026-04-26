using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.XR;
#endif

namespace CPRSimulation
{
    /// <summary>
    /// Quest/VR 下将 Screen Space Overlay Canvas 转为 World Space，并设置相机，确保 UI 正确显示。
    /// 挂到任意场景（如 MainMenu）的 GameObject 上即可。
    /// </summary>
    public class VRCanvasSetup : MonoBehaviour
    {
        [Tooltip("Canvas 在相机前方的距离（米）")]
        public float canvasDistance = 2f;

        [Tooltip("Canvas 的缩放")]
        public float canvasScale = 0.002f;

        private void Start()
        {
            SetupAllCanvasesForVR(canvasDistance, canvasScale);
        }

        /// <summary>从外部（如 MainMenuUI）调用，将 Screen Space Overlay Canvas 转为 World Space</summary>
        public static void SetupAllCanvasesForVR(float canvasDistance = 2f, float canvasScale = 0.002f)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // 仅在实际 VR 设备上执行
            if (!IsVRActive()) return;
#endif

            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig == null) return;

            Transform centerEye = cameraRig.centerEyeAnchor;

            if (centerEye == null) return;
            Camera cam = centerEye.GetComponent<Camera>();
            if (cam == null) cam = centerEye.GetComponentInChildren<Camera>();
            if (cam == null) return;

            var canvases = FindObjectsOfType<Canvas>(true);
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = cam;
                canvas.planeDistance = 100f;

                var rt = canvas.GetComponent<RectTransform>();
                if (rt != null && centerEye != null)
                {
                    canvas.transform.SetParent(centerEye, false);
                    rt.localPosition = new Vector3(0f, 0f, canvasDistance);
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one * canvasScale;
                    rt.sizeDelta = new Vector2(1920f, 1080f);
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                }

                Debug.Log("[VRCanvasSetup] 已将 Canvas 转为 World Space: " + canvas.gameObject.name);
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool IsVRActive()
        {
            var xrDisplay = XRSettings.loadedDeviceName;
            return !string.IsNullOrEmpty(xrDisplay);
        }
#endif
    }
}
