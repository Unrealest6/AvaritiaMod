# AvaritiaMod

**English** · [中文说明](#中文说明)

An end-game content mod for Terraria (tModLoader **1.4.4.9**), inspired by the Minecraft mod *Avaritia*.
It adds infinity-tier gear, a singularity compression chain, powerful multi-block crafting stations,
and AOE mining tools that collect everything they break into a single "matter cluster" item.

* **Compression chain** — Neutron Collector → Neutronium Compressor → singularities → Infinity Catalyst.
* **Crafting stations** — compressed / double-compressed / extreme crafting tables whose contents travel
  with the item, plus a UI for browsing the recipes of the table you are standing at.
* **AOE tools** — World Breaker (pickaxe), Planet Eater (shovel) and Nature Ruin (axe). Switching to their
  second form (`Shift + right click`) breaks a 28×28 area; drops (including chest contents, machine
  contents, furniture and walls) are packed into matter clusters instead of being scattered.
* **Cosmic effects** — cosmic sphere, warp shader, screen shake and related visuals.

Requires **EternalLib** (the shared library — build/install it alongside this mod).
Build with `dotnet build AvaritiaMod.csproj -c Debug` from this folder. Licensed under the MIT license.

---

## 中文说明

无尽贪婪：面向泰拉瑞亚终局内容的附属模组（tModLoader **1.4.4.9**），灵感源自我的世界模组 *Avaritia*。
加入无尽层级装备、奇点压缩体系、专属合成工作站，以及会把挖到的一切收进物质团的**范围挖掘工具**。

* **压缩体系**：中子态素收集器 → 中子态素压缩机 → 各色奇点 → 无尽催化剂。
* **合成工作站**：压缩 / 二重压缩 / 终极工作台，内容物随物品一起保存与同步；可打开配方面板浏览配方。
* **范围挖掘**：世界崩解之镐、星球吞噬之铲、自然毁灭之镐。`Shift + 右键` 切到形态 1 后一次挖 28×28，
  掉落（含箱子内容物、机器内物品、家具、墙体）统一打包成**物质团**，不会散落在地上。
* **视觉与特效**：宇宙球体、跃迁着色器、屏幕震动等。

### 依赖与安装

| 项目 | 说明 |
| --- | --- |
| 运行环境 | tModLoader 1.4.4.9（v2026.07 及以上） |
| 前置模组 | **EternalLib** —— 共用基础库，必须一起安装 |
| 安装 | 创意工坊订阅，或把 `AvaritiaMod.tmod` 与 `EternalLib.tmod` 放进 `Documents/My Games/Terraria/tModLoader/Mods` |

### 玩法要点

* **形态切换**：手持工具 `Shift + 右键` 切换形态；形态 0 是普通挖掘，形态 1 是范围挖掘。
* **物质团**：范围挖掘的产物。物品按**实例**保存（工作台内容物、被改过堆叠上限的物品都不会丢），右键倒出取回。
* **箱子 / 工作台**：范围挖掘会取出内容物并把本体一起收进物质团；普通挖掘时箱子保持原版规则（有物品挖不动）。
* **多人**：范围挖掘的掉落、抹墙、箱子内容物由服务端权威结算，客户端不重复生成。

### 代码结构

```
AvaritiaMod/
├─ AvaritiaMod.cs          模组入口（内容注册、全局钩子）
├─ AvaritiaNet.cs          模组自有网络消息（工作台 / 机器 / 击杀 / 宇宙球体）
├─ AvaritiaRecipe.cs       自定义配方注册与匹配缓存
├─ Common/
│  ├─ Systems/             客户端系统（UI、着色器、屏幕震动、序列帧）
│  ├─ Players/             模组玩家数据（宇宙球体、无尽套装）
│  ├─ UI/                  工作台 / 收集器 / 压缩机的界面与槽位
│  ├─ AvaritiaUtils/       工具类（物质团打包、拖拽、抖动、几何生成）
│  ├─ GlobalNPCs | GlobalTiles | GlobalWalls/   全局钩子
├─ Content/
│  ├─ Items/               物品（工具、护甲、奇点、可放置物、消耗品）
│  ├─ Tiles/               方块（工作台、收集器、压缩机、材料块）
│  ├─ TileEntities/        方块实体（保存内容物与进度）
│  └─ Projectiles/         弹幕
└─ Localization/           本地化文本（zh-Hans / en-US 等）
```

### 开发

```powershell
# 需要与 EternalLib 放在同一个 ModSources 目录下（本项目引用它）
dotnet build AvaritiaMod.csproj -c Debug
```

* 范围挖掘的通用逻辑在 EternalLib（`BreakHelper.AoeSwing`）：本模组只需让工具实现 `IAoeMiningTool`
  （额外掉落表 + 掉落交付），见 `Content/Items/Tools/AoeToolItem.cs`。
* 物质团打包在本模组：`Common/AvaritiaUtils/AvaritiaBreakHelper.cs`。

### 许可与致谢

MIT 协议开源。主创 / 程序 / 设计：Unrealistically；美术：WP、Pulastara、Unrealistically；
技术支持：洛谔谔、yiyang233、Star_KZ、DeepSeek、Kimi。

* 主页：<https://space.bilibili.com/1108947907>
* 仓库：<https://github.com/Unrealest6/AvaritiaMod>
