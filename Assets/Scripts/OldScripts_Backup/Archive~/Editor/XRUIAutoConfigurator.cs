// 简化版 XR UI 配置工具（无 Input System 依赖）
// 菜单：CPR训练系统/🛠 配置XR UI交互
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public static class XRUIAutoConfigurator
{
    [MenuItem("CPR训练系统/🛠 配置XR UI交互", false, 99)]
    public static void Configure()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer == -1)
        {
            Debug.LogWarning("未找到 UI Layer，请在 Project Settings > Tags and Layers 创建名为 UI 的层。");
        }

        int updatedCanvases = FixCanvases(uiLayer);
        FixEventSystem();
        int updatedInteractors = FixRayInteractors(uiLayer);

        Debug.Log($"XR UI 配置完成：Canvas 处理 {updatedCanvases} 个，RayInteractor 处理 {updatedInteractors} 个。");
    }

    private static int FixCanvases(int uiLayer)
    {
        int count = 0;
        foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
                continue;

            if (uiLayer != -1 && canvas.gameObject.layer != uiLayer)
            {
                Undo.RecordObject(canvas.gameObject, "Set Canvas Layer");
                canvas.gameObject.layer = uiLayer;
            }

            if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                Undo.AddComponent<UnityEngine.UI.GraphicRaycaster>(canvas.gameObject);
            }

            Type xrRaycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (xrRaycasterType != null && canvas.GetComponent(xrRaycasterType) == null)
            {
                Undo.AddComponent(canvas.gameObject, xrRaycasterType);
            }

            count++;
        }
        return count;
    }

    private static void FixEventSystem()
    {
        EventSystem es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (es == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            es = esGO.AddComponent<EventSystem>();
        }

        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null)
        {
            Undo.DestroyObjectImmediate(legacy);
        }

        Type xrModuleType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
        if (xrModuleType != null)
        {
            if (es.GetComponent(xrModuleType) == null)
            {
                Undo.AddComponent(es.gameObject, xrModuleType);
            }
        }
        else
        {
            if (es.GetComponent<StandaloneInputModule>() == null)
            {
                Undo.AddComponent<StandaloneInputModule>(es.gameObject);
            }
            Debug.LogWarning("未找到 XRUIInputModule 类型，请确认已安装 XR Interaction Toolkit。已退回 StandaloneInputModule。");
        }
    }

    private static int FixRayInteractors(int uiLayer)
    {
        int count = 0;
        Type xrRayInteractorType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRRayInteractor, Unity.XR.Interaction.Toolkit");
        if (xrRayInteractorType == null)
        {
            Debug.LogWarning("未找到 XRRayInteractor 类型，请确认已安装 XR Interaction Toolkit。");
            return count;
        }

        foreach (var interactor in Resources.FindObjectsOfTypeAll(xrRayInteractorType))
        {
            var go = ((Component)interactor).gameObject;
            Undo.RecordObject(go, "Configure XRRayInteractor");

            if (uiLayer != -1)
            {
                var maskProp = xrRayInteractorType.GetProperty("raycastMask");
                if (maskProp != null && maskProp.PropertyType == typeof(LayerMask))
                {
                    LayerMask current = (LayerMask)maskProp.GetValue(interactor);
                    int newValue = current.value | (1 << uiLayer);
                    maskProp.SetValue(interactor, (LayerMask)newValue);
                }
            }
            count++;
        }
        return count;
    }
}
#endif
