using System;
using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// 意识确认（反応の確認 / 両肩を叩く）的动作检测。
    ///
    /// 指南要求轻拍**两侧**肩膀并呼唤。这里把「两肩都被碰到」作为通过条件，
    /// 而不是「拍够 N 下」——前者更贴流程，也更难蒙混：
    /// 单手在一个地方乱晃凑不出两侧接触。
    ///
    /// 两条检测通路，和 CPRPressDetector 一样，因为手部碰撞体不一定存在
    /// （取决于 OVR 手部物理胶囊有没有启用）：
    ///   1) 碰撞体：肩上的 trigger 球被手碰到。判定走 HandColliderUtil，
    ///      不是「任何碰撞体都算」。
    ///   2) 距离兜底：HandTrackingAdapter 的左右手坐标与肩部的距离。
    ///      必须按手区分，否则分不清拍的是哪一侧。
    ///
    /// 由 CPRTrainingController 在患者倒地后创建，确认完成或跳过后销毁。
    /// </summary>
    public class ResponseCheckDetector : MonoBehaviour
    {
        [Tooltip("肩部检测球半径（米）")]
        public float zoneRadius = 0.12f;

        [Tooltip("距离兜底阈值（米）。手心到肩部中心小于此值算碰到。")]
        public float touchDistance = 0.18f;

        [Tooltip("手部专用 Layer；-1 表示不按层过滤，交给名称/层级关系判定。")]
        public LayerMask handLayerMask = -1;

        [Tooltip("显示检测球，调试用。")]
        public bool showZones = false;

        public bool LeftTouched { get; private set; }
        public bool RightTouched { get; private set; }
        public bool BothTouched => LeftTouched && RightTouched;

        /// <summary>两肩都被碰到时触发一次。</summary>
        public event Action OnBothShouldersTouched;

        private Transform _leftShoulder;
        private Transform _rightShoulder;
        private HandTrackingAdapter _adapter;
        private ShoulderTouchZone _leftZone;
        private ShoulderTouchZone _rightZone;
        private bool _reported;

        /// <summary>
        /// 绑定肩部骨骼并建好检测球。肩骨为空时该侧直接算不可检测 ——
        /// 与其偷偷放行，不如让调用方能看出绑定失败（日志里会写）。
        /// </summary>
        public void Initialize(Transform leftShoulder, Transform rightShoulder, HandTrackingAdapter adapter)
        {
            _leftShoulder = leftShoulder;
            _rightShoulder = rightShoulder;
            _adapter = adapter != null ? adapter : FindObjectOfType<HandTrackingAdapter>();

            LeftTouched = false;
            RightTouched = false;
            _reported = false;

            _leftZone = CreateZone("ResponseCheck_LeftShoulder", _leftShoulder, true);
            _rightZone = CreateZone("ResponseCheck_RightShoulder", _rightShoulder, false);

            if (_leftShoulder == null || _rightShoulder == null)
            {
                Debug.LogWarning("[ResponseCheck] 肩部骨骼没找全" +
                                 $"（左:{(_leftShoulder != null ? "OK" : "缺")} " +
                                 $"右:{(_rightShoulder != null ? "OK" : "缺")}），" +
                                 "该侧无法检测，学员只能走超时跳过。");
            }
        }

        private ShoulderTouchZone CreateZone(string zoneName, Transform shoulder, bool isLeft)
        {
            if (shoulder == null)
                return null;

            var zoneObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            zoneObject.name = zoneName;
            zoneObject.transform.SetParent(shoulder, false);
            zoneObject.transform.localPosition = Vector3.zero;
            zoneObject.transform.localScale = Vector3.one * (zoneRadius * 2f);

            var col = zoneObject.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            var renderer = zoneObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (showZones)
                    renderer.material.color = new Color(0.2f, 0.9f, 1f, 0.35f);
                else
                    renderer.enabled = false;   // 只关渲染，trigger 照常工作
            }

            var zone = zoneObject.AddComponent<ShoulderTouchZone>();
            zone.Bind(this, isLeft);
            return zone;
        }

        private void Update()
        {
            UpdateDistanceFallback();

            if (!_reported && BothTouched)
            {
                _reported = true;
                Debug.Log("[ResponseCheck] 两肩均已触碰，意识确认动作完成");
                OnBothShouldersTouched?.Invoke();
            }
        }

        /// <summary>手部碰撞体不存在时的兜底：直接量左右手到左右肩的距离。</summary>
        private void UpdateDistanceFallback()
        {
            if (_adapter == null)
                return;

            bool hasLeftHand = _adapter.TryGetLeftHandPosition(out Vector3 leftHand);
            bool hasRightHand = _adapter.TryGetRightHandPosition(out Vector3 rightHand);

            // 哪只手碰哪个肩都算 —— 现实里学员可能双手交叉拍，
            // 强制「左手拍左肩」只会制造无意义的失败。
            if (_leftShoulder != null && !LeftTouched)
            {
                if ((hasLeftHand && IsWithinTouch(leftHand, _leftShoulder)) ||
                    (hasRightHand && IsWithinTouch(rightHand, _leftShoulder)))
                {
                    ReportTouch(true);
                }
            }

            if (_rightShoulder != null && !RightTouched)
            {
                if ((hasLeftHand && IsWithinTouch(leftHand, _rightShoulder)) ||
                    (hasRightHand && IsWithinTouch(rightHand, _rightShoulder)))
                {
                    ReportTouch(false);
                }
            }
        }

        private bool IsWithinTouch(Vector3 handPosition, Transform shoulder)
        {
            return Vector3.Distance(handPosition, shoulder.position) <= touchDistance;
        }

        internal void ReportTouch(bool isLeft)
        {
            if (isLeft)
            {
                if (LeftTouched) return;
                LeftTouched = true;
                Debug.Log("[ResponseCheck] 左肩已触碰");
            }
            else
            {
                if (RightTouched) return;
                RightTouched = true;
                Debug.Log("[ResponseCheck] 右肩已触碰");
            }
        }

        private void OnDestroy()
        {
            // 检测球是挂在患者骨骼下的，销毁本组件时必须跟着清掉，
            // 否则「再来一次」会在肩上越堆越多。
            DestroyZone(_leftZone);
            DestroyZone(_rightZone);
        }

        private static void DestroyZone(ShoulderTouchZone zone)
        {
            if (zone != null && zone.gameObject != null)
                Destroy(zone.gameObject);
        }

        /// <summary>挂在肩部检测球上，把碰撞转发回 ResponseCheckDetector。</summary>
        internal class ShoulderTouchZone : MonoBehaviour
        {
            private ResponseCheckDetector _owner;
            private bool _isLeft;

            public void Bind(ResponseCheckDetector owner, bool isLeft)
            {
                _owner = owner;
                _isLeft = isLeft;
            }

            private void OnTriggerEnter(Collider other)
            {
                TryReport(other);
            }

            private void OnTriggerStay(Collider other)
            {
                TryReport(other);
            }

            private void TryReport(Collider other)
            {
                if (_owner == null)
                    return;

                if (!HandColliderUtil.IsHandCollider(other, _owner.handLayerMask))
                    return;

                _owner.ReportTouch(_isLeft);
            }
        }
    }
}
