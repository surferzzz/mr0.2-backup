using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRFirstAid.Equipment
{
    /// <summary>
    /// 设备基类 - 所有医疗设备的基础类
    /// </summary>
    public abstract class EquipmentBase : XRGrabInteractable
    {
        [Header("Equipment Info")]
        [SerializeField] protected string equipmentName = "Equipment";
        [SerializeField] protected string description = "Medical equipment";

        [Header("State")]
        [SerializeField] protected bool isInUse = false;
        [SerializeField] protected bool isActivated = false;

        [Header("Feedback")]
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected AudioClip useSound;
        [SerializeField] protected AudioClip activateSound;

        protected override void Awake()
        {
            base.Awake();
            InitializeEquipment();
        }

        protected virtual void InitializeEquipment()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        /// <summary>
        /// 使用设备
        /// </summary>
        public virtual void UseEquipment()
        {
            if (isInUse) return;

            isInUse = true;
            OnEquipmentUsed();

            if (useSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(useSound);
            }
        }

        /// <summary>
        /// 激活设备
        /// </summary>
        public virtual void ActivateEquipment()
        {
            if (isActivated) return;

            isActivated = true;
            OnEquipmentActivated();

            if (activateSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(activateSound);
            }
        }

        /// <summary>
        /// 停用设备
        /// </summary>
        public virtual void DeactivateEquipment()
        {
            isActivated = false;
            OnEquipmentDeactivated();
        }

        /// <summary>
        /// 重置设备
        /// </summary>
        public virtual void ResetEquipment()
        {
            isInUse = false;
            isActivated = false;
            OnEquipmentReset();
        }

        protected abstract void OnEquipmentUsed();
        protected abstract void OnEquipmentActivated();
        protected virtual void OnEquipmentDeactivated() { }
        protected virtual void OnEquipmentReset() { }

        public bool IsInUse => isInUse;
        public bool IsActivated => isActivated;
        public string EquipmentName => equipmentName;
    }
}
