using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CPRSimulation.Editor
{
    public static class RealisticEmergencySceneBuilder
    {
        private const string RootName = "Realistic_Emergency_Scene";
        private const string MaterialFolder = "Assets/Materials/Medical/RealisticScene";
        private const string PrimaryScenePath = "Assets/0.7/mr0.7.unity";

        [MenuItem("Tools/CPR/0.7/Open 0.7 And Build Realistic Emergency Room", false, -60)]
        public static void OpenPrimarySceneAndBuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(PrimaryScenePath, OpenSceneMode.Single);
            BuildRealisticEmergencyRoom();
            EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("Tools/CPR/0.7/Build Realistic Emergency Room In Current Scene", false, -59)]
        public static void BuildRealisticEmergencyRoom()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build realistic emergency room",
                    "This will replace the generated Realistic_Emergency_Scene root in the current scene, then add a low-poly emergency room around the CPR/AED training area.",
                    "Build",
                    "Cancel"))
            {
                return;
            }

            EnsureMaterialFolder();

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build realistic emergency room");

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                Undo.DestroyObjectImmediate(oldRoot);
            }

            Vector3 center = ResolveTrainingCenter();
            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create realistic emergency room");
            root.transform.position = center;

            SceneMaterials materials = CreateMaterials();
            HideSimpleGroundIfPresent();

            BuildRoomShell(root.transform, materials);
            BuildPatientCareZone(root.transform, materials);
            BuildWallDetails(root.transform, materials);
            BuildAedCabinet(root.transform, materials);
            BuildMedicalTrolley(root.transform, materials);
            BuildSideStretcher(root.transform, materials);
            BuildMonitorStation(root.transform, materials);
            BuildOxygenStation(root.transform, materials);
            BuildPrivacyCurtain(root.transform, materials);
            BuildWaitingCorner(root.transform, materials);
            BuildLighting(root.transform, materials);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "Realistic room built",
                "A realistic CPR/AED emergency room has been added. The scene is dirty; save it when the placement looks right.",
                "OK");
        }

        private static Vector3 ResolveTrainingCenter()
        {
            GameObject chestDetector = GameObject.Find("CPR_ChestDetector");
            if (chestDetector != null)
            {
                Vector3 pos = chestDetector.transform.position;
                return new Vector3(pos.x, 0f, pos.z);
            }

            string[] patientNames =
            {
                "Standing React Death Backward",
                "XBot_Patient",
                "Patient",
                "Remy@Laying Seizure",
                "X Bot"
            };

            foreach (string patientName in patientNames)
            {
                GameObject patient = GameObject.Find(patientName);
                if (patient != null)
                {
                    Vector3 pos = patient.transform.position;
                    return new Vector3(pos.x, 0f, pos.z);
                }
            }

            return new Vector3(0f, 0f, 1f);
        }

        private static void HideSimpleGroundIfPresent()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null || ground.transform.parent != null)
            {
                return;
            }

            MeshFilter filter = ground.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            string meshName = filter.sharedMesh.name.ToLowerInvariant();
            if (!meshName.Contains("plane") && !meshName.Contains("quad"))
            {
                return;
            }

            Undo.RecordObject(ground, "Hide simple ground");
            ground.SetActive(false);
        }

        private static void BuildRoomShell(Transform root, SceneMaterials materials)
        {
            CreateCube("Floor_Tile_Base", root, new Vector3(0f, -0.03f, 0.25f), new Vector3(7.4f, 0.06f, 6.4f), materials.Floor, true);
            CreateCube("Back_Wall", root, new Vector3(0f, 1.45f, 3.45f), new Vector3(7.4f, 2.9f, 0.12f), materials.Wall, true);
            CreateCube("Left_Wall", root, new Vector3(-3.65f, 1.45f, 0.25f), new Vector3(0.12f, 2.9f, 6.4f), materials.Wall, true);
            CreateCube("Right_Wall", root, new Vector3(3.65f, 1.45f, 0.25f), new Vector3(0.12f, 2.9f, 6.4f), materials.Wall, true);
            CreateCube("Ceiling_Panel", root, new Vector3(0f, 2.92f, 0.25f), new Vector3(7.4f, 0.08f, 6.4f), materials.Ceiling, true);

            CreateCube("Front_Wall_Left_Return", root, new Vector3(-2.75f, 1.35f, -2.95f), new Vector3(1.8f, 2.7f, 0.12f), materials.Wall, true);
            CreateCube("Front_Wall_Right_Return", root, new Vector3(2.75f, 1.35f, -2.95f), new Vector3(1.8f, 2.7f, 0.12f), materials.Wall, true);
            CreateCube("Entrance_Header", root, new Vector3(0f, 2.55f, -2.95f), new Vector3(1.9f, 0.35f, 0.12f), materials.Wall, true);

            CreateBaseboard(root, materials, new Vector3(0f, 0.08f, 3.36f), new Vector3(7.25f, 0.16f, 0.08f), "Back");
            CreateBaseboard(root, materials, new Vector3(-3.56f, 0.08f, 0.25f), new Vector3(0.08f, 0.16f, 6.25f), "Left");
            CreateBaseboard(root, materials, new Vector3(3.56f, 0.08f, 0.25f), new Vector3(0.08f, 0.16f, 6.25f), "Right");

            BuildFloorGrout(root, materials);
        }

        private static void CreateBaseboard(Transform root, SceneMaterials materials, Vector3 pos, Vector3 scale, string side)
        {
            CreateCube("Baseboard_" + side, root, pos, scale, materials.Trim, true);
        }

        private static void BuildFloorGrout(Transform root, SceneMaterials materials)
        {
            for (int i = -7; i <= 7; i++)
            {
                float x = i * 0.5f;
                CreateCube("Floor_Grout_X_" + i, root, new Vector3(x, 0.003f, 0.25f), new Vector3(0.012f, 0.006f, 6.35f), materials.Grout, false);
            }

            for (int i = -6; i <= 7; i++)
            {
                float z = -2.75f + i * 0.5f;
                CreateCube("Floor_Grout_Z_" + i, root, new Vector3(0f, 0.004f, z), new Vector3(7.25f, 0.006f, 0.012f), materials.Grout, false);
            }
        }

        private static void BuildPatientCareZone(Transform root, SceneMaterials materials)
        {
            CreateCube("Patient_Treatment_Mat", root, new Vector3(0f, 0.012f, 0f), new Vector3(1.85f, 0.018f, 2.25f), materials.Mat, false);
            CreateCube("Patient_Mat_Yellow_Left", root, new Vector3(-0.98f, 0.026f, 0f), new Vector3(0.05f, 0.012f, 2.35f), materials.WarningYellow, false);
            CreateCube("Patient_Mat_Yellow_Right", root, new Vector3(0.98f, 0.026f, 0f), new Vector3(0.05f, 0.012f, 2.35f), materials.WarningYellow, false);
            CreateCube("Patient_Mat_Yellow_Back", root, new Vector3(0f, 0.026f, 1.15f), new Vector3(2f, 0.012f, 0.05f), materials.WarningYellow, false);
            CreateCube("Patient_Mat_Yellow_Front", root, new Vector3(0f, 0.026f, -1.15f), new Vector3(2f, 0.012f, 0.05f), materials.WarningYellow, false);

            CreateWorldText(
                "Floor_Label_CPR_Zone",
                root,
                "CPR ZONE",
                new Vector3(0f, 0.04f, -1.38f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector2(420f, 90f),
                42,
                materials.TextDark);
        }

        private static void BuildWallDetails(Transform root, SceneMaterials materials)
        {
            CreateWorldText(
                "Back_Wall_Emergency_Sign",
                root,
                "EMERGENCY CARE BAY",
                new Vector3(0f, 2.35f, 3.37f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector2(780f, 110f),
                44,
                materials.TextDark);

            CreateWorldText(
                "Back_Wall_Checklist",
                root,
                "CHECK RESPONSE\nCALL 119\nSTART CPR\nUSE AED",
                new Vector3(-2.95f, 1.65f, 3.29f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector2(350f, 260f),
                26,
                materials.TextDark);

            CreateCube("Checklist_Backplate", root, new Vector3(-2.95f, 1.65f, 3.34f), new Vector3(0.75f, 0.62f, 0.04f), materials.SignWhite, false);
            CreateClock(root, materials);
        }

        private static void BuildAedCabinet(Transform root, SceneMaterials materials)
        {
            CreateCube("AED_Wall_Cabinet_Back", root, new Vector3(-2.08f, 1.2f, 3.33f), new Vector3(0.78f, 0.82f, 0.12f), materials.AedRed, false);
            CreateCube("AED_Wall_Cabinet_Door", root, new Vector3(-2.08f, 1.2f, 3.25f), new Vector3(0.66f, 0.68f, 0.04f), materials.GlassBlue, false);
            CreateCube("AED_Cabinet_Handle", root, new Vector3(-1.72f, 1.2f, 3.18f), new Vector3(0.04f, 0.36f, 0.04f), materials.Metal, false);
            CreateCube("AED_Status_Light", root, new Vector3(-2.38f, 1.56f, 3.17f), new Vector3(0.08f, 0.08f, 0.04f), materials.Green, false);

            CreateWorldText(
                "AED_Cabinet_Label",
                root,
                "AED",
                new Vector3(-2.08f, 1.2f, 3.15f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector2(260f, 180f),
                70,
                materials.TextRed);
        }

        private static void BuildMedicalTrolley(Transform root, SceneMaterials materials)
        {
            Vector3 origin = new Vector3(2.5f, 0f, 1.4f);
            CreateCube("Trolley_Top_Tray", root, origin + new Vector3(0f, 0.8f, 0f), new Vector3(0.95f, 0.06f, 0.55f), materials.Metal, false);
            CreateCube("Trolley_Middle_Tray", root, origin + new Vector3(0f, 0.48f, 0f), new Vector3(0.95f, 0.05f, 0.55f), materials.Metal, false);
            CreateCube("Trolley_Bottom_Tray", root, origin + new Vector3(0f, 0.18f, 0f), new Vector3(0.95f, 0.05f, 0.55f), materials.Metal, false);

            CreateCube("Trolley_Left_Post_Front", root, origin + new Vector3(-0.43f, 0.45f, -0.23f), new Vector3(0.05f, 0.82f, 0.05f), materials.Metal, false);
            CreateCube("Trolley_Right_Post_Front", root, origin + new Vector3(0.43f, 0.45f, -0.23f), new Vector3(0.05f, 0.82f, 0.05f), materials.Metal, false);
            CreateCube("Trolley_Left_Post_Back", root, origin + new Vector3(-0.43f, 0.45f, 0.23f), new Vector3(0.05f, 0.82f, 0.05f), materials.Metal, false);
            CreateCube("Trolley_Right_Post_Back", root, origin + new Vector3(0.43f, 0.45f, 0.23f), new Vector3(0.05f, 0.82f, 0.05f), materials.Metal, false);

            CreateCylinder("Trolley_Wheel_FL", root, origin + new Vector3(-0.42f, 0.08f, -0.23f), new Vector3(0.09f, 0.035f, 0.09f), Quaternion.Euler(90f, 0f, 0f), materials.DarkRubber, false);
            CreateCylinder("Trolley_Wheel_FR", root, origin + new Vector3(0.42f, 0.08f, -0.23f), new Vector3(0.09f, 0.035f, 0.09f), Quaternion.Euler(90f, 0f, 0f), materials.DarkRubber, false);
            CreateCylinder("Trolley_Wheel_BL", root, origin + new Vector3(-0.42f, 0.08f, 0.23f), new Vector3(0.09f, 0.035f, 0.09f), Quaternion.Euler(90f, 0f, 0f), materials.DarkRubber, false);
            CreateCylinder("Trolley_Wheel_BR", root, origin + new Vector3(0.42f, 0.08f, 0.23f), new Vector3(0.09f, 0.035f, 0.09f), Quaternion.Euler(90f, 0f, 0f), materials.DarkRubber, false);

            CreateCube("First_Aid_Box", root, origin + new Vector3(-0.18f, 0.88f, 0f), new Vector3(0.34f, 0.18f, 0.24f), materials.WhitePlastic, false);
            CreateCube("First_Aid_Cross_Vertical", root, origin + new Vector3(-0.18f, 0.89f, -0.125f), new Vector3(0.045f, 0.14f, 0.02f), materials.AedRed, false);
            CreateCube("First_Aid_Cross_Horizontal", root, origin + new Vector3(-0.18f, 0.89f, -0.125f), new Vector3(0.14f, 0.045f, 0.02f), materials.AedRed, false);
            CreateCube("Folded_Blanket", root, origin + new Vector3(0.2f, 0.89f, 0.04f), new Vector3(0.34f, 0.08f, 0.28f), materials.BlanketBlue, false);
            CreateCylinder("Supply_Bottle", root, origin + new Vector3(0.36f, 0.94f, -0.12f), new Vector3(0.07f, 0.16f, 0.07f), Quaternion.identity, materials.Green, false);
        }

        private static void BuildSideStretcher(Transform root, SceneMaterials materials)
        {
            Vector3 origin = new Vector3(-2.65f, 0f, -0.5f);
            CreateCube("Side_Stretcher_Frame", root, origin + new Vector3(0f, 0.52f, 0f), new Vector3(1.65f, 0.08f, 0.72f), materials.Metal, false);
            CreateCube("Side_Stretcher_Mattress", root, origin + new Vector3(0f, 0.62f, 0f), new Vector3(1.52f, 0.18f, 0.62f), materials.MattressBlue, false);
            CreateCube("Side_Stretcher_Pillow", root, origin + new Vector3(-0.55f, 0.75f, 0f), new Vector3(0.36f, 0.12f, 0.5f), materials.WhitePlastic, false);
            CreateCube("Side_Stretcher_Rail_Left", root, origin + new Vector3(0f, 0.82f, -0.38f), new Vector3(1.55f, 0.07f, 0.05f), materials.Metal, false);
            CreateCube("Side_Stretcher_Rail_Right", root, origin + new Vector3(0f, 0.82f, 0.38f), new Vector3(1.55f, 0.07f, 0.05f), materials.Metal, false);
            CreateCube("Side_Stretcher_Leg_1", root, origin + new Vector3(-0.65f, 0.26f, -0.25f), new Vector3(0.05f, 0.48f, 0.05f), materials.Metal, false);
            CreateCube("Side_Stretcher_Leg_2", root, origin + new Vector3(0.65f, 0.26f, -0.25f), new Vector3(0.05f, 0.48f, 0.05f), materials.Metal, false);
            CreateCube("Side_Stretcher_Leg_3", root, origin + new Vector3(-0.65f, 0.26f, 0.25f), new Vector3(0.05f, 0.48f, 0.05f), materials.Metal, false);
            CreateCube("Side_Stretcher_Leg_4", root, origin + new Vector3(0.65f, 0.26f, 0.25f), new Vector3(0.05f, 0.48f, 0.05f), materials.Metal, false);
        }

        private static void BuildMonitorStation(Transform root, SceneMaterials materials)
        {
            CreateCube("Wall_Monitor_Backplate", root, new Vector3(1.45f, 1.62f, 3.35f), new Vector3(1.08f, 0.68f, 0.07f), materials.ScreenFrame, false);
            CreateCube("Wall_Monitor_Screen", root, new Vector3(1.45f, 1.62f, 3.29f), new Vector3(0.92f, 0.48f, 0.035f), materials.Screen, false);
            CreateCube("ECG_Line_1", root, new Vector3(1.12f, 1.61f, 3.255f), new Vector3(0.22f, 0.025f, 0.02f), materials.Green, false);
            CreateCube("ECG_Line_2", root, new Vector3(1.28f, 1.69f, 3.255f), new Vector3(0.035f, 0.18f, 0.02f), materials.Green, false);
            CreateCube("ECG_Line_3", root, new Vector3(1.43f, 1.58f, 3.255f), new Vector3(0.26f, 0.025f, 0.02f), materials.Green, false);
            CreateCube("ECG_Line_4", root, new Vector3(1.63f, 1.71f, 3.255f), new Vector3(0.035f, 0.22f, 0.02f), materials.Green, false);
            CreateCube("ECG_Line_5", root, new Vector3(1.82f, 1.59f, 3.255f), new Vector3(0.25f, 0.025f, 0.02f), materials.Green, false);

            CreateWorldText(
                "Monitor_Text",
                root,
                "TRAINING MONITOR\nCPR FEEDBACK READY",
                new Vector3(1.45f, 1.37f, 3.235f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector2(520f, 120f),
                22,
                materials.TextGreen);
        }

        private static void BuildOxygenStation(Transform root, SceneMaterials materials)
        {
            Vector3 origin = new Vector3(-3.05f, 0f, 2.45f);
            CreateCylinder("Oxygen_Cylinder", root, origin + new Vector3(0f, 0.58f, 0f), new Vector3(0.16f, 0.58f, 0.16f), Quaternion.identity, materials.OxygenGreen, false);
            CreateCylinder("Oxygen_Regulator", root, origin + new Vector3(0f, 1.2f, 0f), new Vector3(0.12f, 0.055f, 0.12f), Quaternion.identity, materials.Metal, false);
            CreateCube("Oxygen_Trolley_Back", root, origin + new Vector3(0f, 0.56f, 0.17f), new Vector3(0.34f, 0.9f, 0.05f), materials.Metal, false);
            CreateCylinder("Oxygen_Wheel_L", root, origin + new Vector3(-0.13f, 0.11f, 0.18f), new Vector3(0.08f, 0.035f, 0.08f), Quaternion.Euler(90f, 0f, 0f), materials.DarkRubber, false);
            CreateCylinder("Oxygen_Wheel_R", root, origin + new Vector3(0.13f, 0.11f, 0.18f), new Vector3(0.08f, 0.035f, 0.08f), Quaternion.Euler(90f, 0f, 0f), materials.DarkRubber, false);
            CreateWorldText(
                "Oxygen_Label",
                root,
                "O2",
                origin + new Vector3(0f, 0.62f, -0.17f),
                Quaternion.Euler(0f, 0f, 0f),
                new Vector2(150f, 90f),
                32,
                materials.TextWhite);
        }

        private static void BuildPrivacyCurtain(Transform root, SceneMaterials materials)
        {
            CreateCube("Curtain_Rail_Back", root, new Vector3(-1.7f, 2.18f, 1.65f), new Vector3(2.2f, 0.045f, 0.045f), materials.Metal, false);
            CreateCube("Curtain_Rail_Side", root, new Vector3(-2.78f, 2.18f, 0.75f), new Vector3(0.045f, 0.045f, 1.8f), materials.Metal, false);

            for (int i = 0; i < 6; i++)
            {
                float x = -2.65f + i * 0.22f;
                CreateCube("Curtain_Fold_Back_" + i, root, new Vector3(x, 1.25f, 1.67f), new Vector3(0.08f, 1.65f, 0.035f), materials.Curtain, false);
            }

            for (int i = 0; i < 5; i++)
            {
                float z = 0.0f + i * 0.28f;
                CreateCube("Curtain_Fold_Side_" + i, root, new Vector3(-2.8f, 1.25f, z), new Vector3(0.035f, 1.65f, 0.08f), materials.Curtain, false);
            }
        }

        private static void BuildWaitingCorner(Transform root, SceneMaterials materials)
        {
            BuildChair(root, materials, new Vector3(2.55f, 0f, -1.65f), "Waiting_Chair_A");
            BuildChair(root, materials, new Vector3(3.05f, 0f, -1.65f), "Waiting_Chair_B");
            CreateCube("Small_Waiting_Table", root, new Vector3(2.8f, 0.36f, -0.9f), new Vector3(0.62f, 0.06f, 0.44f), materials.Wood, false);
            CreateCube("Clipboard", root, new Vector3(2.72f, 0.42f, -0.9f), new Vector3(0.28f, 0.025f, 0.2f), materials.SignWhite, false);
            CreateCube("Clipboard_Clip", root, new Vector3(2.72f, 0.445f, -1.0f), new Vector3(0.16f, 0.02f, 0.035f), materials.Metal, false);
        }

        private static void BuildChair(Transform root, SceneMaterials materials, Vector3 origin, string prefix)
        {
            CreateCube(prefix + "_Seat", root, origin + new Vector3(0f, 0.42f, 0f), new Vector3(0.42f, 0.08f, 0.42f), materials.ChairBlue, false);
            CreateCube(prefix + "_Back", root, origin + new Vector3(0f, 0.76f, 0.18f), new Vector3(0.42f, 0.62f, 0.08f), materials.ChairBlue, false);
            CreateCube(prefix + "_Leg_FL", root, origin + new Vector3(-0.16f, 0.22f, -0.16f), new Vector3(0.045f, 0.4f, 0.045f), materials.Metal, false);
            CreateCube(prefix + "_Leg_FR", root, origin + new Vector3(0.16f, 0.22f, -0.16f), new Vector3(0.045f, 0.4f, 0.045f), materials.Metal, false);
            CreateCube(prefix + "_Leg_BL", root, origin + new Vector3(-0.16f, 0.22f, 0.16f), new Vector3(0.045f, 0.4f, 0.045f), materials.Metal, false);
            CreateCube(prefix + "_Leg_BR", root, origin + new Vector3(0.16f, 0.22f, 0.16f), new Vector3(0.045f, 0.4f, 0.045f), materials.Metal, false);
        }

        private static void BuildLighting(Transform root, SceneMaterials materials)
        {
            CreateCube("Ceiling_Light_Panel_A", root, new Vector3(-1.9f, 2.86f, 0.4f), new Vector3(1.1f, 0.035f, 0.45f), materials.LightPanel, false);
            CreateCube("Ceiling_Light_Panel_B", root, new Vector3(1.9f, 2.86f, 0.4f), new Vector3(1.1f, 0.035f, 0.45f), materials.LightPanel, false);
            CreateCube("Emergency_Red_Indicator", root, new Vector3(3.42f, 2.28f, -2.4f), new Vector3(0.12f, 0.12f, 0.08f), materials.AedRed, false);

            Light existingLight = Object.FindObjectOfType<Light>();
            if (existingLight != null && existingLight.type == LightType.Directional)
            {
                Undo.RecordObject(existingLight, "Tune directional light");
                existingLight.intensity = 0.35f;
                existingLight.shadows = LightShadows.None;
                existingLight.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            }

            GameObject lightA = new GameObject("Realistic_Room_Light_A");
            lightA.transform.SetParent(root, false);
            lightA.transform.localPosition = new Vector3(-1.9f, 2.55f, 0.4f);
            Light compA = lightA.AddComponent<Light>();
            compA.type = LightType.Point;
            compA.intensity = 1.1f;
            compA.range = 4.2f;
            compA.shadows = LightShadows.None;
            Undo.RegisterCreatedObjectUndo(lightA, "Create room light");

            GameObject lightB = new GameObject("Realistic_Room_Light_B");
            lightB.transform.SetParent(root, false);
            lightB.transform.localPosition = new Vector3(1.9f, 2.55f, 0.4f);
            Light compB = lightB.AddComponent<Light>();
            compB.type = LightType.Point;
            compB.intensity = 1.0f;
            compB.range = 4.2f;
            compB.shadows = LightShadows.None;
            Undo.RegisterCreatedObjectUndo(lightB, "Create room light");
        }

        private static void CreateClock(Transform root, SceneMaterials materials)
        {
            CreateCylinder("Wall_Clock_Face", root, new Vector3(0.2f, 2.08f, 3.35f), new Vector3(0.26f, 0.025f, 0.26f), Quaternion.Euler(90f, 0f, 0f), materials.SignWhite, false);
            CreateCube("Wall_Clock_Hand_Minute", root, new Vector3(0.2f, 2.1f, 3.315f), new Vector3(0.025f, 0.18f, 0.02f), materials.TextDark, false);
            CreateCube("Wall_Clock_Hand_Hour", root, new Vector3(0.27f, 2.08f, 3.312f), new Vector3(0.14f, 0.022f, 0.02f), materials.TextDark, false);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool markStatic)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            SetMaterial(go, material);
            RemoveColliderUnlessNeeded(go, markStatic);

            if (markStatic)
            {
                GameObjectUtility.SetStaticEditorFlags(
                    go,
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic);
            }

            return go;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material, bool markStatic)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            SetMaterial(go, material);
            RemoveColliderUnlessNeeded(go, markStatic);

            if (markStatic)
            {
                GameObjectUtility.SetStaticEditorFlags(
                    go,
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic);
            }

            return go;
        }

        private static void RemoveColliderUnlessNeeded(GameObject go, bool keepCollider)
        {
            if (keepCollider)
            {
                return;
            }

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static GameObject CreateWorldText(
            string name,
            Transform parent,
            string text,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector2 size,
            int fontSize,
            Material textMaterial)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localRotation = localRotation;
            canvasObject.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = size;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text label = textObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(10, fontSize - 10);
            label.resizeTextMaxSize = fontSize;
            label.color = textMaterial != null ? textMaterial.color : Color.black;

            return canvasObject;
        }

        private static void SetMaterial(GameObject go, Material material)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static SceneMaterials CreateMaterials()
        {
            return new SceneMaterials
            {
                Floor = CreateOrUpdateMaterial("Realistic_Floor_Warm_Tile", new Color(0.62f, 0.64f, 0.62f), 0f, 0.38f),
                Grout = CreateOrUpdateMaterial("Realistic_Floor_Grout", new Color(0.35f, 0.36f, 0.35f), 0f, 0.2f),
                Wall = CreateOrUpdateMaterial("Realistic_Wall_Off_White", new Color(0.82f, 0.85f, 0.82f), 0f, 0.25f),
                Ceiling = CreateOrUpdateMaterial("Realistic_Ceiling_Panel", new Color(0.74f, 0.77f, 0.76f), 0f, 0.18f),
                Trim = CreateOrUpdateMaterial("Realistic_Baseboard_Trim", new Color(0.45f, 0.48f, 0.48f), 0f, 0.3f),
                Mat = CreateOrUpdateMaterial("Realistic_Patient_Treatment_Mat", new Color(0.05f, 0.17f, 0.22f), 0f, 0.35f),
                WarningYellow = CreateOrUpdateMaterial("Realistic_Warning_Yellow", new Color(1f, 0.78f, 0.12f), 0f, 0.22f),
                AedRed = CreateOrUpdateMaterial("Realistic_AED_Red", new Color(0.78f, 0.08f, 0.06f), 0f, 0.42f),
                GlassBlue = CreateOrUpdateMaterial("Realistic_Glass_Blue", new Color(0.28f, 0.48f, 0.68f), 0f, 0.65f),
                Metal = CreateOrUpdateMaterial("Realistic_Brushed_Metal", new Color(0.56f, 0.58f, 0.58f), 0.25f, 0.55f),
                Green = CreateOrUpdateMaterial("Realistic_Green_Indicator", new Color(0.1f, 0.85f, 0.28f), 0f, 0.45f),
                WhitePlastic = CreateOrUpdateMaterial("Realistic_White_Plastic", new Color(0.94f, 0.95f, 0.92f), 0f, 0.36f),
                BlanketBlue = CreateOrUpdateMaterial("Realistic_Blanket_Blue", new Color(0.12f, 0.28f, 0.48f), 0f, 0.28f),
                DarkRubber = CreateOrUpdateMaterial("Realistic_Dark_Rubber", new Color(0.03f, 0.035f, 0.035f), 0f, 0.12f),
                MattressBlue = CreateOrUpdateMaterial("Realistic_Mattress_Blue", new Color(0.12f, 0.32f, 0.55f), 0f, 0.3f),
                ScreenFrame = CreateOrUpdateMaterial("Realistic_Monitor_Frame", new Color(0.04f, 0.05f, 0.06f), 0f, 0.44f),
                Screen = CreateOrUpdateMaterial("Realistic_Monitor_Screen", new Color(0.02f, 0.08f, 0.07f), 0f, 0.65f),
                OxygenGreen = CreateOrUpdateMaterial("Realistic_Oxygen_Green", new Color(0.12f, 0.62f, 0.42f), 0f, 0.42f),
                Curtain = CreateOrUpdateMaterial("Realistic_Curtain_Blue", new Color(0.26f, 0.48f, 0.66f), 0f, 0.25f),
                ChairBlue = CreateOrUpdateMaterial("Realistic_Chair_Blue", new Color(0.08f, 0.25f, 0.42f), 0f, 0.28f),
                Wood = CreateOrUpdateMaterial("Realistic_Light_Wood", new Color(0.56f, 0.42f, 0.28f), 0f, 0.28f),
                SignWhite = CreateOrUpdateMaterial("Realistic_Sign_White", new Color(0.96f, 0.96f, 0.9f), 0f, 0.35f),
                LightPanel = CreateOrUpdateMaterial("Realistic_Light_Panel", new Color(0.95f, 0.98f, 0.88f), 0f, 0.75f),
                TextDark = CreateOrUpdateMaterial("Realistic_Text_Dark", new Color(0.02f, 0.025f, 0.025f), 0f, 0.2f),
                TextRed = CreateOrUpdateMaterial("Realistic_Text_Red", new Color(0.9f, 0.04f, 0.04f), 0f, 0.2f),
                TextGreen = CreateOrUpdateMaterial("Realistic_Text_Green", new Color(0.25f, 1f, 0.35f), 0f, 0.2f),
                TextWhite = CreateOrUpdateMaterial("Realistic_Text_White", Color.white, 0f, 0.2f)
            };
        }

        private static Material CreateOrUpdateMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FindLitShader());
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.color = color;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (name.Contains("Light") || name.Contains("Indicator") || name.Contains("Screen") || name.Contains("Text_Green"))
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color * 1.4f);
                }
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            return shader;
        }

        private static void EnsureMaterialFolder()
        {
            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Materials/Medical");
            EnsureFolder(MaterialFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                return;
            }

            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private sealed class SceneMaterials
        {
            public Material Floor;
            public Material Grout;
            public Material Wall;
            public Material Ceiling;
            public Material Trim;
            public Material Mat;
            public Material WarningYellow;
            public Material AedRed;
            public Material GlassBlue;
            public Material Metal;
            public Material Green;
            public Material WhitePlastic;
            public Material BlanketBlue;
            public Material DarkRubber;
            public Material MattressBlue;
            public Material ScreenFrame;
            public Material Screen;
            public Material OxygenGreen;
            public Material Curtain;
            public Material ChairBlue;
            public Material Wood;
            public Material SignWhite;
            public Material LightPanel;
            public Material TextDark;
            public Material TextRed;
            public Material TextGreen;
            public Material TextWhite;
        }
    }
}
