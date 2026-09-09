using System.Collections.Generic;
using UnityEngine;

namespace VRFirstAid.Equipment
{
    /// <summary>
    /// 急救包物品类型
    /// </summary>
    public enum FirstAidItemType
    {
        Bandage,
        Gauze,
        Antiseptic,
        Gloves,
        Scissors,
        Tape
    }

    /// <summary>
    /// 急救包物品数据
    /// </summary>
    [System.Serializable]
    public class FirstAidItem
    {
        public FirstAidItemType type;
        public string name;
        public GameObject itemPrefab;
        public int quantity;
    }

    /// <summary>
    /// 急救包 - 管理急救物品的存储和使用
    /// </summary>
    public class FirstAidKit : EquipmentBase
    {
        [Header("Kit Contents")]
        [SerializeField] private List<FirstAidItem> items = new List<FirstAidItem>();
        [SerializeField] private Transform itemSpawnPoint;
        [SerializeField] private bool isOpen = false;

        [Header("Visual")]
        [SerializeField] private GameObject kitLid;
        [SerializeField] private Vector3 openLidRotation = new Vector3(-90f, 0f, 0f);

        private Dictionary<FirstAidItemType, int> itemQuantities = new Dictionary<FirstAidItemType, int>();

        protected override void InitializeEquipment()
        {
            base.InitializeEquipment();
            equipmentName = "急救包";
            description = "包含基本急救用品";

            // 初始化物品数量
            foreach (var item in items)
            {
                itemQuantities[item.type] = item.quantity;
            }
        }

        /// <summary>
        /// 打开/关闭急救包
        /// </summary>
        public void ToggleKit()
        {
            isOpen = !isOpen;

            if (kitLid != null)
            {
                if (isOpen)
                {
                    kitLid.transform.localRotation = Quaternion.Euler(openLidRotation);
                }
                else
                {
                    kitLid.transform.localRotation = Quaternion.identity;
                }
            }

            OnKitToggled(isOpen);
        }

        /// <summary>
        /// 取出物品
        /// </summary>
        public GameObject TakeItem(FirstAidItemType itemType)
        {
            if (!isOpen)
            {
                Debug.LogWarning("急救包未打开！");
                return null;
            }

            if (!itemQuantities.ContainsKey(itemType) || itemQuantities[itemType] <= 0)
            {
                Debug.LogWarning($"没有{itemType}了！");
                return null;
            }

            // 查找物品预制体
            FirstAidItem item = items.Find(i => i.type == itemType);
            if (item == null || item.itemPrefab == null)
            {
                Debug.LogWarning($"找不到{itemType}的预制体！");
                return null;
            }

            // 减少数量
            itemQuantities[itemType]--;

            // 生成物品
            Vector3 spawnPosition = itemSpawnPoint != null ? itemSpawnPoint.position : transform.position;
            GameObject spawnedItem = Instantiate(item.itemPrefab, spawnPosition, Quaternion.identity);

            OnItemTaken(itemType, itemQuantities[itemType]);

            return spawnedItem;
        }

        /// <summary>
        /// 检查是否有指定物品
        /// </summary>
        public bool HasItem(FirstAidItemType itemType)
        {
            return itemQuantities.ContainsKey(itemType) && itemQuantities[itemType] > 0;
        }

        /// <summary>
        /// 获取物品数量
        /// </summary>
        public int GetItemQuantity(FirstAidItemType itemType)
        {
            return itemQuantities.ContainsKey(itemType) ? itemQuantities[itemType] : 0;
        }

        /// <summary>
        /// 添加物品
        /// </summary>
        public void AddItem(FirstAidItemType itemType, int quantity)
        {
            if (itemQuantities.ContainsKey(itemType))
            {
                itemQuantities[itemType] += quantity;
            }
            else
            {
                itemQuantities[itemType] = quantity;
            }
        }

        protected override void OnEquipmentUsed()
        {
            ToggleKit();
        }

        protected override void OnEquipmentActivated()
        {
            // 急救包激活逻辑
        }

        protected override void OnEquipmentReset()
        {
            base.OnEquipmentReset();
            isOpen = false;

            if (kitLid != null)
            {
                kitLid.transform.localRotation = Quaternion.identity;
            }

            // 重置物品数量
            foreach (var item in items)
            {
                itemQuantities[item.type] = item.quantity;
            }
        }

        private void OnKitToggled(bool opened)
        {
            Debug.Log($"急救包{(opened ? "已打开" : "已关闭")}");
        }

        private void OnItemTaken(FirstAidItemType itemType, int remaining)
        {
            Debug.Log($"取出{itemType}，剩余{remaining}个");
        }

        public bool IsOpen => isOpen;
    }
}
