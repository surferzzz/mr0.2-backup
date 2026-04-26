using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRFirstAid.Core;
using UnityEditor.SceneManagement;

public static class EarthquakeRebuild
{
    [MenuItem("Tools/CPR/Rebuild Earthquake Scene", priority = 0)]
    public static void Rebuild()
    {
        // Clean leftover problematic prefabs
        var badNames = new[] { "OVRInteractionComprehensive", "ISDKExampleMenu" };
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            foreach (var n in badNames)
            {
                if (go.name.Contains(n))
                {
                    Object.DestroyImmediate(go);
                    break;
                }
            }
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        // Camera
        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 1.6f, -2f);
        }

        // Floor
        var floor = GameObject.Find("Floor") ?? GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(10, 1, 10);

        // Test bodies (cube & sphere) with Rigidbody
        var cube = GameObject.Find("TestCube") ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "TestCube";
        cube.transform.position = new Vector3(0, 1f, 2f);
        AddBody(cube);

        var sphere = GameObject.Find("TestSphere") ?? GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "TestSphere";
        sphere.transform.position = new Vector3(1.5f, 1.2f, 2f);
        AddBody(sphere);

        // EarthquakeController
        var eqGO = GameObject.Find("EarthquakeController") ?? new GameObject("EarthquakeController");
        var eq = eqGO.GetComponent<EarthquakeController>();
        if (eq == null) eq = eqGO.AddComponent<EarthquakeController>();
        eq.targetCamera = cam.transform;
        eq.affectedBodies = new[] { cube.GetComponent<Rigidbody>(), sphere.GetComponent<Rigidbody>() };

        // UI Canvas
        var ui = GameObject.Find("EarthquakeUI");
        if (ui == null)
        {
            ui = new GameObject("EarthquakeUI");
            var canvas = ui.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = ui.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 300);
            ui.transform.position = new Vector3(0, 1.6f, 1.5f);
            ui.transform.rotation = Quaternion.Euler(0, 180f, 0);
            ui.transform.localScale = Vector3.one * 0.001f;
            ui.AddComponent<CanvasScaler>();
            ui.AddComponent<GraphicRaycaster>();
        }

        // Button
        var buttonObj = ui.transform.Find("TriggerButton")?.gameObject ?? CreateButton(ui.transform);
        var button = buttonObj.GetComponent<Button>();
        var buttonLabel = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        buttonLabel.text = "Start Earthquake";

        // Instruction text
        var instrObj = ui.transform.Find("InstructionText")?.gameObject ?? CreateText(ui.transform, "InstructionText", "Click button to start earthquake");
        var instr = instrObj.GetComponent<TextMeshProUGUI>();

        // Scene manager
        var mgr = GameObject.Find("EarthquakeSceneManager") ?? new GameObject("EarthquakeSceneManager");
        var sceneCtrl = mgr.GetComponent<EarthquakeSceneController>();
        if (sceneCtrl == null) sceneCtrl = mgr.AddComponent<EarthquakeSceneController>();
        sceneCtrl.earthquakeController = eq;
        sceneCtrl.triggerButton = button;
        sceneCtrl.instructionText = instr;
        sceneCtrl.autoStartCPR = false;

        // Wire button click
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(sceneCtrl.StartEarthquake);

        // Style
        var styler = ui.GetComponent<VRFirstAid.UI.EarthquakeUIStyler>() ?? ui.AddComponent<VRFirstAid.UI.EarthquakeUIStyler>();
        styler.triggerButton = button;
        styler.instructionText = instr;

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = mgr;
        Debug.Log("Rebuilt EarthquakeTraining scene with basic objects and references.");
    }

    private static void AddBody(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.mass = 10f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private static GameObject CreateText(Transform parent, string name, string text)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 120);
        rt.anchoredPosition = new Vector2(0, 80);
        return go;
    }

    private static GameObject CreateButton(Transform parent)
    {
        var go = new GameObject("TriggerButton", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.52f, 0.96f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 120);
        rt.anchoredPosition = new Vector2(0, -80);

        var label = new GameObject("Text", typeof(RectTransform));
        label.transform.SetParent(go.transform, false);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = "Start Earthquake";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36;
        var lrt = label.GetComponent<RectTransform>();
        lrt.sizeDelta = rt.sizeDelta;
        return go;
    }
}
