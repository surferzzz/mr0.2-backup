// 反射版：一键配置手柄射线 + UI，不依赖编译时的 Input System/XRI 类型
// 菜单：CPR训练系统/🛠 配置射线与UI
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public static class XRRayUISetupTool
{
    private const string MenuPath = "CPR训练系统/🛠 配置射线与UI";

    [MenuItem(MenuPath, false, 99)]
    public static void Configure()
    {
        int canvasCount = FixCanvases();
        FixEventSystem();
        int interactorCount = FixRayInteractors();

        Debug.Log($"射线/UI 配置完成：Canvas {canvasCount} 个，手柄 {interactorCount} 个。（如需输入绑定，请确认安装并启用 Input System + XRI）");
    }

    private static int FixCanvases()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        int count = 0;
        foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
        {
            if (canvas.renderMode != RenderMode.WorldSpace) continue;
            Undo.RecordObject(canvas.gameObject, "Configure UI Canvas");
            if (uiLayer != -1) canvas.gameObject.layer = uiLayer;

            if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                Undo.AddComponent<UnityEngine.UI.GraphicRaycaster>(canvas.gameObject);

            var xrRaycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (xrRaycasterType != null && canvas.GetComponent(xrRaycasterType) == null)
                Undo.AddComponent(canvas.gameObject, xrRaycasterType);

            count++;
        }
        return count;
    }

    private static void FixEventSystem()
    {
        var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            es = go.AddComponent<EventSystem>();
        }

        // 移除 StandaloneInputModule，优先加 XRUIInputModule（反射），否则回退 StandaloneInputModule
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null) Undo.DestroyObjectImmediate(legacy);

        var xrModuleType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
        if (xrModuleType != null)
        {
            if (es.GetComponent(xrModuleType) == null)
                Undo.AddComponent(es.gameObject, xrModuleType);
        }
        else
        {
            if (es.GetComponent<StandaloneInputModule>() == null)
                Undo.AddComponent<StandaloneInputModule>(es.gameObject);
            Debug.LogWarning("未找到 XRUIInputModule 类型，请确认已安装 XR Interaction Toolkit。已回退 StandaloneInputModule。");
        }
    }

    private static int FixRayInteractors()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        int count = 0;
        var rayType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRRayInteractor, Unity.XR.Interaction.Toolkit");
        var lineType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual, Unity.XR.Interaction.Toolkit");

        if (rayType == null || lineType == null)
        {
            Debug.LogWarning("未找到 XR Interaction Toolkit 组件，请确认已安装。");
            return count;
        }

        var rays = Resources.FindObjectsOfTypeAll(rayType);
        foreach (var r in rays)
        {
            var go = ((Component)r).gameObject;
            Undo.RecordObject(go, "Configure XR Ray Interactor");

            // 添加 Line Visual
            if (go.GetComponent(lineType) == null)
                Undo.AddComponent(go, lineType);

            // RaycastMask 加 UI
            if (uiLayer != -1)
            {
                var maskProp = rayType.GetProperty("raycastMask");
                if (maskProp != null && maskProp.PropertyType == typeof(LayerMask))
                {
                    LayerMask current = (LayerMask)maskProp.GetValue(r);
                    int newValue = current.value | (1 << uiLayer);
                    maskProp.SetValue(r, (LayerMask)newValue);
                }
            }
            count++;
        }
        return count;
    }
}
#endif
