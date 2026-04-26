using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键把 EarthquakeController 放进当前场景，并自动绑定摄像机与可震动物体。
/// 菜单：Tools/CPR/Add Earthquake Controller
/// </summary>
public static class EarthquakeAutoSetup
{
    [MenuItem("Tools/CPR/Add Earthquake Controller")]
    public static void AddEarthquakeController()
    {
        // 找或建控制器物体
        var existing = Object.FindObjectOfType<EarthquakeController>();
        var root = existing ? existing.gameObject : new GameObject("EarthquakeController");
        if (!existing) Undo.RegisterCreatedObjectUndo(root, "Create EarthquakeController");

        var ctrl = root.GetComponent<EarthquakeController>() ?? Undo.AddComponent<EarthquakeController>(root);

        // 绑定摄像机：优先 MainCamera，其次 CenterEyeAnchor
        if (ctrl.targetCamera == null)
        {
            var mainCam = Camera.main ? Camera.main.transform : null;
            if (!mainCam)
            {
                var centerEye = Object.FindObjectsOfType<Transform>()
                    .FirstOrDefault(t => t.name.Equals("CenterEyeAnchor"));
                mainCam = centerEye;
            }
            ctrl.targetCamera = mainCam;
        }

        // 自动搜集可震动刚体：找场景中带 MeshRenderer 但无 Rigidbody 的对象，限 50 个以内
        var candidates = Object.FindObjectsOfType<MeshRenderer>()
            .Select(r => r.GetComponent<Rigidbody>() ? null : r)
            .Where(r => r != null)
            .Take(50)
            .ToArray();

        var rbList = candidates
            .Select(r =>
            {
                var rb = Undo.AddComponent<Rigidbody>(r.gameObject);
                rb.mass = 10f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                return rb;
            })
            .ToList();

        ctrl.affectedBodies = rbList.ToArray();

        Selection.activeObject = root;
        EditorUtility.DisplayDialog("Earthquake", "已创建 EarthquakeController 并绑定摄像机/刚体。\n在 Play 模式选中它，点击 Inspector 上的 Play() 即可触发地震。", "OK");
    }
}
