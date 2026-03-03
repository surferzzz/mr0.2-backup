using UnityEngine;
using UnityEditor;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 创建简单的替代患者模型（用于CPR训练）
    /// </summary>
    public class PatientModelCreator : EditorWindow
    {
        [MenuItem("Tools/CPR/创建简易患者模型", false, 203)]
        public static void CreateSimplePatient()
        {
            // 创建患者根对象
            var patient = new GameObject("Patient_Simple");
            patient.transform.position = new Vector3(0, 0, 0);
            
            // 身体各部分的材质颜色
            Color skinColor = new Color(0.96f, 0.87f, 0.78f); // 肤色
            Color clothColor = new Color(0.3f, 0.5f, 0.7f);   // 衣服蓝色
            
            // === 躯干（胸部和腹部）===
            var torso = CreatePrimitive("Torso", PrimitiveType.Capsule, patient.transform);
            torso.transform.localPosition = new Vector3(0, 0.15f, 0);
            torso.transform.localRotation = Quaternion.Euler(90, 0, 0);
            torso.transform.localScale = new Vector3(0.35f, 0.4f, 0.25f);
            SetColor(torso, clothColor);
            
            // === 胸部标记区域（CPR按压点）===
            var chestMarker = CreatePrimitive("ChestMarker", PrimitiveType.Cylinder, patient.transform);
            chestMarker.transform.localPosition = new Vector3(0, 0.16f, 0.15f);
            chestMarker.transform.localScale = new Vector3(0.12f, 0.02f, 0.12f);
            SetColor(chestMarker, new Color(1f, 0.3f, 0.3f, 0.8f)); // 红色标记
            
            // === 头部 ===
            var head = CreatePrimitive("Head", PrimitiveType.Sphere, patient.transform);
            head.transform.localPosition = new Vector3(0, 0.12f, 0.55f);
            head.transform.localScale = new Vector3(0.2f, 0.22f, 0.22f);
            SetColor(head, skinColor);
            
            // === 颈部 ===
            var neck = CreatePrimitive("Neck", PrimitiveType.Cylinder, patient.transform);
            neck.transform.localPosition = new Vector3(0, 0.12f, 0.42f);
            neck.transform.localRotation = Quaternion.Euler(90, 0, 0);
            neck.transform.localScale = new Vector3(0.08f, 0.06f, 0.08f);
            SetColor(neck, skinColor);
            
            // === 左臂 ===
            var leftArm = CreatePrimitive("LeftArm", PrimitiveType.Capsule, patient.transform);
            leftArm.transform.localPosition = new Vector3(-0.28f, 0.08f, 0.1f);
            leftArm.transform.localRotation = Quaternion.Euler(0, 0, 15);
            leftArm.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
            SetColor(leftArm, skinColor);
            
            // === 右臂 ===
            var rightArm = CreatePrimitive("RightArm", PrimitiveType.Capsule, patient.transform);
            rightArm.transform.localPosition = new Vector3(0.28f, 0.08f, 0.1f);
            rightArm.transform.localRotation = Quaternion.Euler(0, 0, -15);
            rightArm.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
            SetColor(rightArm, skinColor);
            
            // === 左腿 ===
            var leftLeg = CreatePrimitive("LeftLeg", PrimitiveType.Capsule, patient.transform);
            leftLeg.transform.localPosition = new Vector3(-0.1f, 0.1f, -0.45f);
            leftLeg.transform.localRotation = Quaternion.Euler(90, 0, 0);
            leftLeg.transform.localScale = new Vector3(0.1f, 0.35f, 0.1f);
            SetColor(leftLeg, new Color(0.2f, 0.25f, 0.35f)); // 裤子颜色
            
            // === 右腿 ===
            var rightLeg = CreatePrimitive("RightLeg", PrimitiveType.Capsule, patient.transform);
            rightLeg.transform.localPosition = new Vector3(0.1f, 0.1f, -0.45f);
            rightLeg.transform.localRotation = Quaternion.Euler(90, 0, 0);
            rightLeg.transform.localScale = new Vector3(0.1f, 0.35f, 0.1f);
            SetColor(rightLeg, new Color(0.2f, 0.25f, 0.35f));
            
            // === 地面/床垫 ===
            var bed = CreatePrimitive("Bed", PrimitiveType.Cube, patient.transform);
            bed.transform.localPosition = new Vector3(0, -0.03f, 0);
            bed.transform.localScale = new Vector3(0.8f, 0.05f, 1.8f);
            SetColor(bed, new Color(0.9f, 0.9f, 0.85f));
            
            // 移动CPR检测器到胸部位置
            var detector = Object.FindObjectOfType<CPRPressDetector>();
            if (detector != null)
            {
                detector.transform.position = chestMarker.transform.position + Vector3.up * 0.05f;
                detector.chestSurface = chestMarker.transform;
                Debug.Log("[PatientCreator] 已将CPR检测器移动到患者胸部位置");
            }
            
            Selection.activeGameObject = patient;
            Undo.RegisterCreatedObjectUndo(patient, "Create Simple Patient");
            
            Debug.Log("[PatientCreator] 简易患者模型已创建\n" +
                      "- 红色圆盘标记是CPR按压位置\n" +
                      "- 请确保CPR_ChestDetector在胸部标记上方");
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent);
            
            // 移除碰撞体（除了需要交互的部分）
            var collider = obj.GetComponent<Collider>();
            if (collider != null && name != "ChestMarker")
            {
                Object.DestroyImmediate(collider);
            }
            
            return obj;
        }

        private static void SetColor(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                
                // 如果有透明度，设置透明模式
                if (color.a < 1f)
                {
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                }
                
                renderer.material = mat;
            }
        }

        [MenuItem("Tools/CPR/对齐检测器到患者胸部", false, 204)]
        public static void AlignDetectorToChest()
        {
            var detector = Object.FindObjectOfType<CPRPressDetector>();
            var patient = GameObject.Find("Patient_Simple");
            
            if (detector == null)
            {
                Debug.LogWarning("[PatientCreator] 未找到CPR检测器");
                return;
            }
            
            if (patient == null)
            {
                Debug.LogWarning("[PatientCreator] 未找到患者模型");
                return;
            }
            
            var chestMarker = patient.transform.Find("ChestMarker");
            if (chestMarker != null)
            {
                detector.transform.position = chestMarker.position + Vector3.up * 0.05f;
                detector.chestSurface = chestMarker;
                Debug.Log("[PatientCreator] 检测器已对齐到患者胸部");
            }
        }
    }
}
