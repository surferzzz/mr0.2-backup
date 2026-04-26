# CPR 按压检测脚本设置说明（CPRPressDetector）

挂载在 **CPR_ChestDetector** 上，用于检测手部按压胸部深度与频率。在 Unity Inspector 中选中该物体即可调整以下项。

---

## 检测模式

| 参数 | 说明 | 推荐 |
|------|------|------|
| **Use Collision Detection** | 用手部碰撞体进入触发器检测 | 勾选（true） |

---

## 引用设置

| 参数 | 说明 | 推荐 |
|------|------|------|
| **Chest Surface** | 胸部参考位置（用于算基准高度和深度） | 由 CPRTrainingController 自动设为检测区自身 Transform，一般无需改 |
| **Hand Palm** | 手掌 Transform（仅在不使用碰撞检测时用） | 不勾选碰撞检测时可从 HandTrackingAdapter 自动获得 |

---

## 碰撞检测设置

| 参数 | 说明 | 推荐 |
|------|------|------|
| **Detection Area Size** | 盒型触发器大小 (x,y,z) 米 | (0.15, 0.1, 0.15) |
| **Hand Layer Mask** | 只把指定 Layer 当作手；-1 = 全部 | -1（Everything） |

---

## 距离备用（无碰撞时用手掌距离）

| 参数 | 说明 | 推荐 |
|------|------|------|
| **Use Distance Fallback** | 碰撞无触发时，用手与胸部距离驱动深度/UI/触觉 | 勾选（true） |
| **Palm Distance Threshold** | 手与胸部距离小于此值（米）视为「在按压」 | 0.4 |

---

## 调试

| 参数 | 说明 | 推荐 |
|------|------|------|
| **Debug Log Triggers** | 每次有物体进触发器时在控制台打印名称与是否识别为手 | 排查无检测时勾选，平时关闭 |

---

## 检测参数

| 参数 | 说明 | 推荐 |
|------|------|------|
| **Min Detection Depth Cm** | 深度大于此值（cm）才计为「一次按压」 | 1 |
| **Smoothing** | 手部位置平滑，越大越稳、反应越慢 | 0.1 |

---

## 医学标准（用于判定过浅/合适/过深、过慢/合适/过快）

| 参数 | 说明 | 推荐 |
|------|------|------|
| **Min Good Depth Cm** | 达标最小深度 (cm) | 5 |
| **Max Good Depth Cm** | 达标最大深度 (cm) | 6 |
| **Min Good Rate BPM** | 达标最慢频率 (次/分) | 100 |
| **Max Good Rate BPM** | 达标最快频率 (次/分) | 120 |

---

## 实时数据（只读）

Inspector 中会显示当前：深度(cm)、频率(BPM)、按压次数、是否手在接触。不可编辑，仅用于查看。

---

## 常见问题

- **一直没有检测**：勾选 **Debug Log Triggers** 看是否有物体进触发器；确保 **Use Distance Fallback** 为 true，且场景中有 **HandTrackingAdapter**（一般由控制器自动添加）。
- **检测区位置不对**：检测区应由 **CPRTrainingController** 在进入 CPR 时创建并挂到患者胸部骨骼下，无需在场景里手动摆；若场景里已有旧的 CPR_ChestDetector，可删掉让流程重新创建。
- **深度/频率判定太严或太松**：调整 **医学标准** 四个参数（Min/Max Good Depth Cm、Min/Max Good Rate BPM）。
