using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// 将训练结果/统计 UI 放在患者胸部旁边的地面上，并始终朝向摄像头。
    /// 挂在结果统计 Canvas 的根物体上（通常与 CPRHUDFollowCamera 共用同一个 Canvas 根）。
    /// </summary>
    public class CPRResultPanelPlacer : MonoBehaviour
    {
        [Header("位置设置")]
        [Tooltip("相对于胸部向旁边偏移的距离（米）")]
        public float sideOffset = 0.5f;

        [Tooltip("地面高度（世界坐标 Y）")]
        public float groundY = 0f;

        [Tooltip("是否始终面向摄像头")]
        public bool faceCamera = true;

        private Transform _chest;
        private Transform _camera;

        private void LateUpdate()
        {
            if (!gameObject.activeInHierarchy) return;

            if (_chest == null)
            {
                var chestGo = GameObject.Find("CPR_ChestDetector");
                if (chestGo != null) _chest = chestGo.transform;
            }

            if (_camera == null)
            {
                if (Camera.main != null) _camera = Camera.main.transform;
            }

            if (_chest == null || _camera == null) return;

            Vector3 chestPos = _chest.position;

            // 从胸部指向摄像头的水平向量
            Vector3 toCam = _camera.position - chestPos;
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f)
            {
                toCam = _camera.right;
            }
            toCam.Normalize();

            // 侧向 = Vector3.up 与 toCam 的叉积
            Vector3 side = Vector3.Cross(Vector3.up, toCam).normalized;

            Vector3 targetPos = chestPos + side * sideOffset;
            targetPos.y = groundY;

            transform.position = targetPos;

            if (faceCamera)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _camera.position);
            }
        }
    }

}
