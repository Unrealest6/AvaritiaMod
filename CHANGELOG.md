# 变更记录

## 0.50

### 🔧 玩家可见修复
| 问题 | 修复 |
|------|------|
| 配方列表高亮不取消 | 高亮状态由面板统一管理，同一时刻仅一个配方高亮 |
| Shift+点击合成槽/物品格无反应 | 拖拽不再吞掉停在子元素上的修饰键点击 |
| 普通模式挖带物品的箱子 | 恢复原版行为：箱内有物品时无法破坏 |
| NatureRuin 越界/蔓藤/草皮 | 修复数组越界、蔓藤循环死区、草皮客户端打不掉 |
| HeavenArrow 联机发包过多 | 仅拥有者在速度随机帧与螺旋纠偏时同步，削减 90% 网络包 |
| AvaritiaMarkProj 重复传送/生成 | 仅拥有者执行传送与无尽剑生成 |
| 手持形态不同步 | 形态绑定到**物品实例**（而非玩家全局），支持多人同步 |
| 范围挖掘箱子打不掉/反复爆物质团 | 修复 `DestroyChest` 在有物品时返回 false 导致的破坏失败；同一箱子去重 |
| 多个物质团互相清空内容 | 物质团内容由 ModItem 单例改为按**物品实例**存储 |
| 未打开过的箱子被破坏后物品丢失 | 多人下由服务端权威处理箱子破坏与掉落，客户端不再提前清空 |
| 工作台放置后其他端打开为空 | 内容物直接写入 TileEntity 并同步，消除单例字段隐患 |
| 背包/世界掉落不跟随形态 | 修复 `PreDrawInInventory`/`PreDrawInWorld` 绘制逻辑 |

### 🎮 新增与改进
- **AvaritiaNet 网络层统一**：17 个消息类型全部集中管理，消除手写包字段错位导致的静默故障；所有发送点统一为 `AvaritiaNet.RequestXxx(...)`
- **配方匹配缓存**：以槽位内容快照为键缓存匹配结果，避免每帧全量遍历配方
- **批量破坏同步**：NatureRuin 范围挖掘由每格一个包改为每 400 格分片（约 4 包/次挥动）
- **着色器资源管理**：WarpSystem / CosmicSphereSystem 绘制资源改为加载时缓存，消除每帧 `new BlendState` / `Request<Effect>` 分配
- **UI 拖拽保护**：拖拽期间自动阻断子元素交互（`IgnoresMouseInteraction`），防止拖面板时物品被分堆到路过的槽位

### ⚠️ 破坏性变更
- `AvaritiaMod` 主类瘦身，网络处理（约 400 行）全部下沉至 `AvaritiaNet`
- 删除 `FrameItem` 所有 IL 相关钩子，持握绘制迁移至官方 `ModifyItemDraw`
- `WorldBreaker` / `PlanetEater` 改为继承 `AvaritiaModeItem`，删除各自 `HoldItem` 与 `Mode` 赋值
- `ColorGradient.Gradients`：可写字典 → `IReadOnlyDictionary`
- `FrameTextureSystem.Register*`：返回类型 `FrameTexture?` → `FrameTexture`（非空）
- `Player.ResetVelocity` 重命名为 `ClampVelocity`（旧名标记 `[Obsolete]`）

### 🛠️ 内部重构
- **AvaritiaBreakHelper**：统一方块破坏、掉落获取、箱子处理、物质团生成；缓存反射 `MethodInfo`，消除循环内反复 `GetMethod`
- **箱子破坏流程**：修正坐标推断（`GetMultiTileOrigin` 归一化到左上角）；破坏前真正清空 `chest.item[]`；多人新增 `RequestChestBreak` 消息
- **形态系统**：`AvaritiaModeGlobalItem`（`CloneNewInstances`）把形态存在物品实例上，实现 `SaveData` / `LoadData` / `NetSend` / `NetReceive`
- **NeutronCollector**：服务端广播由每 tick 改为"物品变化立即同步 + 进度每 30 tick 同步"；修复存档键名不一致导致进度归零
- **物质团合并**：`SpawnAsClusters` 公共化，采用"逐团回填剩余"策略，防止死循环与物品丢失
- 移除 `Mono.Cecil` / `MonoMod` / `System.Reflection` 全局 using（无 IL 补丁）