# 从现有资源做到可玩原型

对照当前仓库实况写的落地步骤：美术 P0 已齐，玩法代码为零。目标不是重画，而是用已有图先跑通「能走、能跳、能存、能开门、能拿到能力」的教堂垂直切片，再按主路线扩五区。

相关文档：

- 技能与硬顺序：[Skills and route design.md](./Skills%20and%20route%20design.md)
- 序章房间：[Rooms/cathedral_intro.md](./Rooms/cathedral_intro.md)
- 敌人设定：[Bestiary/README.md](./Bestiary/README.md)
- 后续出图：[Art/asset_checklist.md](./Art/asset_checklist.md)

工程：Unity **6000.3.23f1**，URP 2D，已装 Input System、2D Tilemap、2D Animation。`Assets/` 下没有 `.cs`。

---

## 0. 先认清资源能干什么

### 0.1 已经有的

| 类别 | 路径 | 用途 |
|------|------|------|
| 主角立绘 | `Assets/Art/Characters/protagonist_cursed_pilgrim.png` | 标题/图鉴/过场，**不进关卡** |
| 主角 chibi 待机 | `.../protagonist_cursed_pilgrim_chibi.png` | 关卡占位待机 |
| 主角 chibi 跑姿 | `.../protagonist_cursed_pilgrim_chibi_run.png` | 关卡占位跑动（单帧，不是循环） |
| 五区敌人立绘 | `Assets/Art/Enemies/{biome}/` 各 4 张 | 图鉴/头目过场；关卡里先缩小当占位 |
| 世界剖面 | `map_world_overview.png` | 关卡规划墙图，对照技能文档摆连接 |
| 五区背景 | `map_bg_*.png` | 视差远景 |
| 五区平台块 | `map_tileset_*.png` | **块状平台/墙面**，不是 16px 自动瓦片 |
| 五区样板房间 | `map_room_sample_*.png` | 布局参考画；可当第一间的假背景 |
| 五区装饰 | `Props/{biome}/prop_*.png` | 纯装饰（喷泉/宝箱/拉杆等降级为氛围） |
| 存档点 ×5 | `map_interact_save_*.png` | 未激活 / 已激活 两态 |
| 陷阱 ×5 | `map_hazards_*.png` | 每区 3 种 |
| 门 | `map_doors_common.png`、`map_doors_ability_gates.png` | 普通门/锁门/头目门 + 4 种能力门（关/开） |
| 开关 | `map_interact_switches.png` | 压力板、拉杆、符文墙、曲柄 |
| 宝箱 | `map_interact_chest.png` | 普通关、普通开、稀有关 |
| 拾取 | `map_pickups_consumables.png`、`keys.png`、`abilities.png` | 消耗品 / 钥匙纹章 / 5 个能力圣物 |
| 攀爬 | `map_traversal_common.png` | 石梯、铁链、藤梯（顶/中/底） |
| 移动平台 | `map_platforms_moving.png` | 石台、齿轮台、气囊台（大/中/残） |
| 可破坏 | `map_breakables_common.png` | 圣瓮、木箱、骨瓮（完整/碎） |
| 秘密 | `map_secrets_walls.png` | 裂墙、假地面、藤帘暗门 |

### 0.2 现在没有、不要假装有

- 任何玩法脚本、Prefab、Tilemap 资源、Sorting Layer、自定义 Tag
- 主角动作循环（跳/攻/伤/死）；run 只是一张跑姿
- 敌人侧视 sprite sheet（立绘带背景，不能当行走图）
- 标准格子 tileset（现有「tileset」是插画块，无法 Automatic Tile）
- 水面可平铺条、传送门、NPC、HUD、VFX
- **墙跳**、**时间操控** 的独立圣物图（能力图只有 5 个）
- **污光**、**下砸** 的独立能力门（能力门只有 4 种）

### 0.3 三条使用原则

1. **立绘 ≠ 关卡精灵**。带场景背景的立绘只用于 UI/过场；关卡角色只用 chibi，敌人先把立绘当「会动的纸片」。
2. **平台用块，不用格子**。第一版把 `map_tileset_*` 切成 Sprite，摆 GameObject + `BoxCollider2D`。等比例和手感稳了再考虑重切格子瓦片。
3. **样板房间是布局，不是关卡**。`map_room_sample_cathedral.png` 可当第一天的假房间：铺在后面，前面用透明碰撞盒描地板。

---

## 1. 工程地基（先做，后面所有物体都靠它）

### 1.1 建议目录

```
Assets/
  Art/                          ← 已有，不要挪
  Prefabs/
    Player/
    Enemies/
    Interact/
    Hazards/
    Traversal/
  ScriptableObjects/
    Abilities/
    Items/
    Enemies/
  Scripts/
    Player/
    Combat/
    Interact/
    Enemies/
    World/
    Save/
    Camera/
  Scenes/
    SampleScene.unity           ← 可留作沙盒
    Cathedral_Hub.unity         ← 第一步真正关卡
```

### 1.2 Sorting Layer（Edit → Project Settings → Tags and Layers）

从后到前：

| Layer | 放什么 |
|-------|--------|
| Background | `map_bg_*`、样板房间 |
| Midground | 装饰 props、旗帜、灯 |
| Platforms | tileset 块、移动平台、梯子视觉 |
| Hazards | 刺、毒池、锯片 |
| Entities | 主角、敌人、宝箱、门、存档、拾取 |
| Foreground | 前景碎石、垂藤、栏杆 |
| UI | 以后 HUD |

### 1.3 Tag 与 Physics Layer

Tag：`Player`、`Enemy`、`Hazard`、`Pickup`、`Interact`、`SavePoint`、`Breakable`。

Physics 2D Layer（可与 Tag 同名）：`Player`、`Ground`、`Hazard`、`Enemy`、`Interact`、`Pickup`。

碰撞矩阵起步：

- Player ↔ Ground、Hazard、Enemy、Interact、Pickup
- Enemy ↔ Ground、Player
- Hazard 不跟 Ground 互撞

### 1.4 输入

`Assets/Settings/InputSystem_Actions.inputactions` 已有 Player 表：

| 已有 | 绑定用途 |
|------|----------|
| Move | 走 / 爬梯上下 |
| Jump | 跳 / 墙跳 |
| Attack | 近战 |
| Interact | 存档、开门、拉杆、对话（建议去掉 Hold，改成 Press） |
| Crouch | 蹲（可后做） |
| Sprint | **改成 Dash**，或另加 `Dash` 绑 Left Shift / 手柄东键 |

本期还要用的：Interact 必须是点按。Dash 必须独立，不要和 Sprint 抢。

### 1.5 导入默认值（所有黑底 PNG 同一套）

选中 `Assets/Art` 下 png → Inspector：

| 项 | 值 | 原因 |
|----|----|------|
| Texture Type | Sprite (2D and UI) | |
| Sprite Mode | **Multiple**（单角色图用 Single） | sheet 要切 |
| Pixels Per Unit | **256** | 全项目锁死，角色和平台同一套 |
| Filter Mode | Bilinear | 插画不是像素风 |
| Compression | None（或 High Quality） | 轮廓别糊 |
| Alpha Is Transparency | 勾 | 黑底切完要透明 |
| Mesh Type | Tight | 按 alpha 生成网格 |
| Pivot | 见下表 | |

切完后角色世界高度目标 **≈ 1.6～2.0**。若 chibi 太大，只改 Transform Scale，不要改 PPU。

Pivot 约定：

- 角色 / 敌人占位：Bottom
- 平台块 / 梯子段：Bottom
- 拾取物 / 钥匙：Center
- 门 / 存档 / 宝箱：Bottom
- 吊刺 / 吊灯 / 吊棺：Top

---

## 2. 切图（按文件对照，切完再摆关）

Sprite Editor → Slice → **Automatic**（黑底可用）。切完立刻改名字，Prefab 才稳。

### 2.1 角色（Single）

| 文件 | Sprite Mode | 关卡用法 |
|------|-------------|----------|
| `protagonist_cursed_pilgrim.png` | Single | 不用进场景 |
| `protagonist_cursed_pilgrim_chibi.png` | Single | Idle |
| `protagonist_cursed_pilgrim_chibi_run.png` | Single | Run 占位（面朝右） |

### 2.2 能力圣物 `map_pickups_abilities.png`（左→右）

| 切出名 | 绑定能力 | 设计文档获取地 |
|--------|----------|----------------|
| `ability_double_jump` | 二段跳 | 洞窟深层（潜水之后） |
| `ability_dash` | 冲刺 | 教堂中段 |
| `ability_ground_slam` | 下砸 | 墓穴 |
| `ability_dive` | 潜水 | 洞窟中段 |
| `ability_stained_light` | 污光 | 森林 |

缺图的两个能力：

- **墙跳**：教堂主 Boss 掉落，过场用伪圣主教立绘，地上可用金色水晶 `Props/cathedral/prop_cathedral_crystal` 当圣物占位
- **时间操控**：钟塔 Boss 掉落，地上可用残钟面 `Props/clocktower/prop_clocktower_clock_face`

### 2.3 能力门 `map_doors_ability_gates.png`

上排关、下排开：

| 切出名 | 对应能力 | 主摆位置 |
|--------|----------|----------|
| `gate_walljump_closed` / `_open` | 墙跳（金翼人像） | 教堂→森林高墙 |
| `gate_dash_closed` / `_open` | 冲刺（星裂） | 教堂裂隙房 |
| `gate_dive_closed` / `_open` | 潜水（水密舱） | 森林崖道底 / 湖底 |
| `gate_chrono_closed` / `_open` | 时间（钟面） | 钟塔黄铜门、钟塔→墓穴捷径 |

污光门：没有独立图。用教堂金水晶柱 + 半透明金光 Quad 做「净光障壁」，摆在教堂→墓穴中央楼梯。

下砸门：没有独立图。用 `map_secrets_walls` 的假地面当晶壳封板。

### 2.4 普通门 `map_doors_common.png`（左→右）

`door_basic_closed`、`door_basic_open`、`door_locked`、`door_boss`

### 2.5 钥匙 `map_pickups_keys.png`

| 切出名 | 用途 |
|--------|------|
| `key_common` | 普通锁门 |
| `key_boss` | 头目大门 |
| `crest_cathedral` | 金，三叉/权杖纹 |
| `crest_forest` | 绿，枝叶 |
| `crest_crypt` | 灰，翼剑 |
| `crest_cavern` | 青，三叉戟 |
| `crest_clocktower` | 铜，时仪纹 |

一期可以只用 `key_common` + `key_boss`，五枚纹章留给区域收集。

### 2.6 消耗品 `map_pickups_consumables.png`（左→右）

`pickup_blood_vial`（回血）、`pickup_soul_shard`（货币）、`pickup_map_fragment`、`pickup_mana_crystal`

### 2.7 存档（每张两态，左灭右亮）

`save_{biome}_off`、`save_{biome}_on`

biome = `cathedral` / `forest` / `crypt` / `cavern` / `clocktower`

### 2.8 开关 `map_interact_switches.png`

| 行 | 切出名 |
|----|--------|
| 压力板 | `switch_plate_up`、`switch_plate_down` |
| 拉杆 | `switch_lever_left`、`switch_lever_right` |
| 符文墙 | `switch_rune_off`、`switch_rune_on` |
| 曲柄 | `switch_crank_a`、`switch_crank_b` |

### 2.9 宝箱 / 破坏物 / 秘密

宝箱：`chest_closed`、`chest_open`、`chest_rare_closed`

破坏物三行：`urn_holy`/`_broken`、`crate`/`_broken`、`urn_bone`/`_broken`

秘密：`secret_wall_crack`、`secret_wall_open`、`secret_floor_crack`、`secret_floor_broken`、`secret_vine_curtain`、`secret_vine_open`

### 2.10 攀爬 `map_traversal_common.png`

三列：石梯、铁链、藤梯。每列切 `*_top`、`*_mid`、`*_bottom`。中段必须能竖直平铺。

### 2.11 移动平台 `map_platforms_moving.png`

三行：石质、齿轮、气囊。每行大/中/残各一。切名 `platform_stone_lg` 这类即可。碰撞只盖上表面。

### 2.12 陷阱（每区三种，按图从左到右）

| 文件 | 切出名 |
|------|--------|
| `map_hazards_cathedral` | `spike_floor_cathedral`、`spike_hang_cathedral`、`chandelier_drop` |
| `map_hazards_forest` | `spike_thorn`、`pool_spore`、`vine_hang` |
| `map_hazards_crypt` | `spike_bone`、`coffin_pendulum`、`pool_poison` |
| `map_hazards_cavern` | `spike_coral`、`pool_electric`、`vent_bubble` |
| `map_hazards_clocktower` | `saw_idle`/`_spin`/`_spark`、`steam_off`/`_on`、`gear_crush_a/b/c` |

静态危险（地刺、毒池）一个 Trigger 即可。钟塔锯/蒸汽用多帧做简单循环。

### 2.13 平台块 `map_tileset_*.png`

Automatic 切完后，给每块加 `BoxCollider2D`（或 Composite）。只要「能站的面」。拱门、柱、窗那些是装饰，不要整块当碰撞。

教堂块优先切出：地板条、残平台、左右转角、独柱。样板房间里的楼梯用若干短平台叠出来，不必等楼梯瓦片。

### 2.14 背景与样板房间

`map_bg_*`、`map_room_sample_*`、`map_world_overview`：Sprite Mode = Single，PPU 可单独设成 128（图很大）。只做视觉，不加碰撞。

---

## 3. 主角白模（第一天必须能跑能跳）

### 3.1 Prefab `Prefabs/Player/Player.prefab`

- `SpriteRenderer`：chibi idle，Sorting Layer = Entities
- `Rigidbody2D`：Dynamic，Freeze Rotation Z，Gravity Scale 3～4
- `CapsuleCollider2D`：只包躯干，不要包翅膀和裙摆
- 子物体 `GroundCheck`：脚底短盒，Layer = Ground
- 子物体 `Hurtbox`：Trigger
- 子物体 `AttackHitbox`：默认关，攻击时开
- 组件（自己写，见附录脚本清单）：`PlayerController`、`PlayerHealth`、`AbilityInventory`

### 3.2 第一版手感（先锁数字，再调）

| 项 | 起步值 |
|----|--------|
| 移速 | 6 |
| 跳跃初速 | 12 |
| 落地缓冲 | 0.1 s |
| 土狼时间 | 0.1 s |
| 最大坠落速度 | 20 |

动画：速度 > 0.1 显示 run 单帧，否则 idle。翻转 `localScale.x`。跳/攻暂时仍用 idle，缺帧不要卡进度。

### 3.3 验收

空场景铺两块教堂地板：能左右跑、跳上平台、落地不抖、不会穿地。过了再摆房间。

---

## 4. 大教堂第一间（垂直切片的壳）

新建 `Scenes/Cathedral_Hub.unity`。

### 4.1 最快能「走进画里」的搭法

1. 放入 `map_room_sample_cathedral.png`，Scale 调到房间宽约 30～40 单位。Sorting = Background。
2. 按画里的地板、楼梯、浮台，用空物体 + `BoxCollider2D` 描碰撞（Layer = Ground）。
3. 再铺 `map_bg_cathedral.png` 在更后面，略放大，做一点视差。
4. 主角出生在底层中央左侧 **Intro 侧厅**（getup 后空手；同场景子区，见 [Rooms/cathedral_intro.md](./Rooms/cathedral_intro.md)）。
5. 相机：`Cinemachine` 或简单跟随，Orthographic Size 约 6～8，只跟 X/Y，别旋转。

这间的职责：证明比例。chibi 站在样板房间的地板上应该像「人站在大厅里」，而不是蚂蚁或巨人。

### 4.2 用平台块替换假碰撞

比例对了之后，用 `map_tileset_cathedral` 的地板条、残台、柱顶替换透明盒。样板房间可以留着当氛围，或关掉 SpriteRenderer 只留碰撞对照。

楼梯：3～5 块短平台错层，不要斜碰撞。

### 4.3 装饰（不参与玩法）

从 `Props/cathedral/` 摆：跪天使、倒十字、残柱、吊灯、破长椅、金水晶、铁栅、垂旗。Sorting = Midground。金水晶以后可复用成墙跳圣物占位。

### 4.4 本间必摆的玩法物件（用第 5～7 步的 Prefab）

对照世界剖面「教堂为枢纽」：

| 物件 | 资源 | 作用 |
|------|------|------|
| 出生点旁存档 | `save_cathedral_off/on` | 教交互（SV1） |
| 残誓没收架（序章） | 交互物 + 概念图占位 | 解锁近战（`HasOathblade`）；空手段无战斗怪 |
| 拾剑后存档 | `save_cathedral_off/on` | 出口前第二存档（SV2） |
| 底层地刺 | `spike_floor_cathedral` | 教伤害/击退 |
| 一侧石梯 | `ladder_stone_*` | 教垂直（中殿侧，非 Intro） |
| 普通门（开） | `door_basic_open` | 房间过渡占位 |
| 锁门 | `door_locked` + `key_common` | 教钥匙 |
| 裂隙门（关） | `gate_dash_closed` | 先看见，拿到冲刺再开 |
| 能力圣物：冲刺 | `ability_dash` | 教堂中段发放 |
| 头目门 | `door_boss` | 通向伪圣主教占位房 |
| 净光障壁 | 金水晶 + 光墙 | 通向墓穴，要污光 |
| 高墙/金翼门 | `gate_walljump_*` | 通向森林，要墙跳 |
| 断桥视觉 | 残平台悬空 | 通向钟塔，要二段跳（长期悬念） |

---

## 5. 交互 Prefab（做一套，五区换皮）

每个交互物同一套脚本，只换 Sprite。

### 5.1 存档 `SaveShrine`

- 默认 `save_*_off`
- 玩家进 Trigger 按 Interact：切 `_on`、回满血、写存档（场景名 + 坐标 + 能力 + 钥匙）
- 死亡读档传送回最近圣龛

五区各做一个 Prefab 变体，脚本共用。

### 5.2 门 `Door`

类型枚举：`Open` / `LockedKey` / `Boss` / `AbilityGate`。

- `Open`：进 Trigger 淡出，加载目标房间或传送到门后坐标（一期用同场景传送点即可）
- `LockedKey`：显示 `door_locked`，有 `key_common` 才切 `door_basic_open`
- `Boss`：要 `key_boss` 或区域内条件
- `AbilityGate`：检查 `AbilityInventory`，通过后切下排「破开」图，并关掉阻挡碰撞

### 5.3 开关 `Switch`

压力板：玩家站上 = down，离开可弹回或保持（按关卡勾 `latch`）。

拉杆 / 符文 / 曲柄：Interact 切换两态。发 UnityEvent：开门、动平台、灭刺。

### 5.4 宝箱 `Chest`

关态显示。Interact 后切开态，生成 1 个拾取（钥匙或魂屑），写入「已开」ID，读档不再刷。

### 5.5 拾取 `Pickup`

Trigger 碰到 Player 即收入囊。能力拾取要弹一次「获得 XXX」占位 UI（Text 即可）。

### 5.6 破坏物 `Breakable`

受玩家攻击命中：切碎裂图，掉 0～1 魂屑，关掉碰撞。

### 5.7 秘密墙 / 假地

裂墙：被攻击若干次 → 切开洞图，去掉阻挡。  
假地面：被**下砸**命中才碎（没下砸时要能站上去，让玩家「先看见」）。  
藤帘：走进去可穿过，或攻击揭开。

---

## 6. 危险、梯子、移动平台

### 6.1 伤害源 `Hazard`

Trigger + 伤害值 + 击退。玩家无敌帧 0.6～1.0 s。地刺、毒池、电水、孢池都用这一套。

教堂吊灯：可先当装饰；要砸落时加刚体，玩家走过触发坠落。

钟塔锯：三帧循环。蒸汽：关/喷两态，喷的时候开伤害盒。齿轮碾压：三帧当开合缝。

### 6.2 梯子 `Ladder`

中段精灵平铺，整根加 Trigger。玩家在 Trigger 内按上/下：关掉重力、沿梯子滑动。顶端要能走出去，底端要能落到地板。

石梯 → 教堂/墓穴；藤梯 → 森林；铁链 → 钟塔/墓穴竖井。

### 6.3 移动平台 `MovingPlatform`

两点巡逻（或开关驱动）。玩家站上去要变成平台子物体，或用 `Rigidbody2D` 平台效应，避免滑落。气囊台给洞窟，齿轮台给钟塔。

---

## 7. 能力系统（严格按已拍板的硬顺序）

获取顺序不可打乱：

**冲刺 → 墙跳 → 污光 → 下砸 → 潜水 → 二段跳 → 时间**

### 7.1 `AbilityId`

```
None, Dash, WallJump, StainedLight, GroundSlam, Dive, DoubleJump, Chrono
```

`AbilityInventory` 用 Flags 存。存档只存这一份。

### 7.2 能力 ↔ 现有图

| 能力 | 地上圣物 | 门/封 | 第一次真正用到 |
|------|----------|-------|----------------|
| 冲刺 | `ability_dash` | `gate_dash_*` | 教堂裂隙、全图尖刺缝 |
| 墙跳 | 金水晶占位 | `gate_walljump_*` | 教堂→森林入口 |
| 污光 | `ability_stained_light` | 净光障壁（自搭） | 教堂中央楼梯→墓穴 |
| 下砸 | `ability_ground_slam` | `secret_floor_*` | 墓穴→洞窟竖井 |
| 潜水 | `ability_dive` | `gate_dive_*` | 森林↔洞窟湖峡 |
| 二段跳 | `ability_double_jump` | 教堂高架断桥 | 教堂→钟塔 |
| 时间 | 残钟面占位 | `gate_chrono_*` | 钟塔黄铜门 / 结局 |

### 7.3 实现顺序（和发技能的顺序一致）

1. **冲刺**：短位移 + 0.1～0.15 s 无敌；能穿过宽度小于冲刺距离的刺缝；`gate_dash` 检查此旗。
2. **墙跳**：贴垂直墙面短时滑行，按跳向外弹。金翼门后的竖井用它爬。
3. **污光**：开启后允许穿过 Tag=`HolyBarrier` 的墙。未开时障壁可见、不可过。
4. **下砸**：空中按下+攻（或专用键）快速落地，命中 `BreakableFloor`。
5. **潜水**：进入 Water 层时不溺；未学会则快速掉血或推回岸上。水下密封门看此旗。
6. **二段跳**：空中再跳一次。务必放在「必须潜水才能到达」的房间，避免跳过硬顺序。
7. **时间**：对 Tag=`ChronoObject` 的锯/摆锤/齿轮暂停 2～3 秒；黄铜门看此旗。

一期不要做能力升级。

---

## 8. 战斗占位（能打即可）

### 8.1 敌人不要等 sheet

把立绘 Sprite Mode 保持 Single，Prefab 上 **Scale ≈ 0.15～0.25**，用 Capsule 当身体。看起来像纸片也没关系。图鉴路径：

| 区 | 先做（普通） | 后做（精英/飞） | 头目 |
|----|--------------|-----------------|------|
| 教堂 | 碎誓侍僧 | 泣血执旗者、石喉守卫 | 伪圣主教 |
| 森林 | 苔缚潜伏者 | 残碑守林人、孢翼幽蛾 | 古根祭主 |
| 墓穴 | 棺生遗卒 | 铁闸守灵、青焰魂灯 | 骨冠司仪 |
| 洞窟 | 溺潮遗者 | 晶脉岩卫、幽光水母灵 | 潮晶巫女 |
| 钟塔 | 铆钉机兵 | 摆锤铁卫、黄铜时翼 | 时仪宗主 |

每区第一只普通怪：巡逻 + 近战挥击 + 受击闪白 + 死亡隐藏。数值以后再填，行为按对应 `Docs/Bestiary/*.md`。

飞行单位（石喉、魂灯、水母、时翼、幽蛾）：先做「悬停 + 俯冲」，不要一上来做全 AI。

头目：第一版只做「有碰撞的大纸片 + 两段血 + 召唤小怪」。伪圣主教房用来发墙跳。

### 8.2 玩家攻击

没有攻击帧：出招时打开 `AttackHitbox` 0.15 s，精灵闪一下或前移一点。命中敌人扣血、短击退。够用。

取得残誓前：`HasOathblade == false` 时忽略 Attack 输入。拾取交互设 flag 后才启用；不必另做空手攻击动画。

---

## 9. 按主路线扩地图

世界剖面与技能文档已对齐，房间不要重新发明连接。

```
森林 ←墙跳─ 教堂 ─二段跳→ 钟塔
                │污光          │时间
                ▼              ▼
               墓穴 ─下砸→ 洞窟
                ▲               │潜水
                └──── 捷径 ─────┘
```

### 9.1 建议场景切分（一期）

| 场景 | 内容 | 用哪套图 |
|------|------|----------|
| `Cathedral_Hub` | 出生、存档、刺、梯、锁门、裂隙门、断桥景观、中央障壁 | cathedral 全套 |
| `Cathedral_Dash` | 冲刺教学房 + `ability_dash` | 同上 |
| `Cathedral_Boss` | 伪圣主教占位 → 墙跳 | 头目门、祭坛 props |
| `Forest_Entry` | 墙跳井、藤梯、苔刺、存档 | forest tileset/props/hazards |
| `Forest_Boss` | 古根祭主 → 污光 | 同上 |
| `Crypt_Entry` | 净光门后、骨刺、棺、铁闸 | crypt |
| `Crypt_Boss` | 骨冠司仪 → 下砸 | 假地面通向竖井 |
| `Cavern_Shore` | 浅滩、电水、未开的潜水门 | cavern |
| `Cavern_Dive` | `ability_dive` + 水下门 | 暂用平台块 + 半透明水盒 |
| `Cavern_Boss` | 潮晶巫女 → 二段跳 | 必须在潜水之后 |
| `Clocktower_Bridge` | 断桥 + 二段跳进入 | clocktower |
| `Clocktower_Boss` | 时仪宗主 → 时间 → 黄铜门 | 锯、蒸汽、齿轮 |

场景切换：门 Fade → 加载 → 出生在对应 Door 的 spawn 点。不要一开始就做无缝大世界。

### 9.2 每间房间的最低配置

- 1 个本区存档（或能走回上一存档）
- 1 种本区地刺
- 若干本区平台块 + 1～2 件 props
- 0～2 个普通怪占位
- 若是连接房：对应能力门或「先见后至」景观（看得见过不去）

### 9.3 「先见后至」必须摆出来的锁（开局教堂就能看见）

1. 第一扇裂隙门（缺冲刺）
2. 通往森林的高墙/金翼门（缺墙跳）
3. 中央楼梯净光障壁（缺污光）
4. 高架断桥（缺二段跳，一直留到洞窟后）

---

## 10. 推荐工作顺序（按完成定义推进）

不要按「把五区美术全摆完」推进，按能玩的闭环推进。

| 步 | 完成定义 | 用到的现有资源 |
|----|----------|----------------|
| **A** | 主角在空平台上跑跳 | chibi idle/run、任意 tileset 地板 |
| **B** | 走进教堂样板房，比例正确 | `map_room_sample_cathedral`、`map_bg_cathedral` |
| **C** | 存档、掉刺里死、读档复活 | `save_cathedral_*`、`spike_floor_cathedral` |
| **D** | 爬石梯、开普通门传送 | `ladder_stone_*`、`door_basic_*` |
| **E** | 捡钥匙开门 | `key_common`、`door_locked` |
| **F** | 看见裂隙门过不去；捡冲刺后穿过 | `gate_dash_*`、`ability_dash` |
| **G** | 侍僧占位能砍死 | `enemy_cathedral_broken_acolyte` |
| **H** | 伪圣主教房拿到墙跳，金翼门进森林样板房 | 主教立绘、`gate_walljump_*`、forest 样板 |
| **I** | 森林拿到污光，回教堂下墓穴 | `ability_stained_light`、净光墙、crypt 样板 |
| **J** | 墓穴拿到下砸，砸穿假地进洞窟 | `ability_ground_slam`、`secret_floor_*` |
| **K** | 洞窟拿到潜水再拿到二段跳，回教堂过断桥 | `ability_dive`、`ability_double_jump`、`gate_dive_*` |
| **L** | 钟塔拿到时间，开黄铜门，打通墓穴捷径 | 残钟面、`gate_chrono_*`、锯/蒸汽 |

A～F 是最小可玩。G～H 是教堂切片。I～L 是把技能文档跑成可演示循环。

---

## 11. 脚本清单（按步 A→F 先写这些）

放在 `Assets/Scripts/`，名字可改，职责不要混。

| 脚本 | 步 | 职责 |
|------|----|------|
| `PlayerController` | A | 跑、跳、土狼、朝向、梯子、冲刺/墙跳/二段跳/下砸接口 |
| `PlayerHealth` | C | 血量、无敌、死亡 |
| `AbilityInventory` | F | 能力旗、事件 |
| `GameSave` | C | JSON 存场景、位置、能力、钥匙、已开箱 |
| `SaveShrine` | C | 交互存档、切图 |
| `Hazard` | C | 触发伤害 |
| `Ladder` | D | 攀爬区 |
| `Door` | D | 传送 / 钥匙 / 能力门 |
| `Pickup` | E | 钥匙、消耗品、能力 |
| `Switch` | 之后 | UnityEvent |
| `Chest` | 之后 | 开箱 |
| `Breakable` | 之后 | 破坏 |
| `MovingPlatform` | 之后 | 巡逻 |
| `CameraFollow` | B | 跟随 |
| `EnemyDummy` | G | 巡逻、受伤、死亡 |
| `HolyBarrier` | I | 无污光则挡 |
| `WaterVolume` | K | 无潜水则罚 |

不要在第一步写技能树 UI、地图 UI、对话系统。

---

## 12. 还缺的资源（做完 A～F 再画）

对照 [Art/asset_checklist.md](./Art/asset_checklist.md) 的 P1/P2，和本步骤冲突的以「先能玩」为准。

| 优先级 | 缺什么 | 现在怎么撑 |
|--------|--------|------------|
| 高 | 主角 jump/attack/hurt/death 帧 | idle/run 单帧 |
| 高 | 五区各 1 个普通怪侧视 sheet | 立绘缩小 |
| 中 | 墙跳、时间的圣物图标 | 金水晶、残钟面 |
| 中 | 污光障壁专用图 | 金光 Quad |
| 中 | 水面线 / 水下填充瓦片 | 半透明盒 |
| 中 | 简单刀光、落地尘 | 闪白 |
| 低 | HUD 血槽、货币图标 | UGUI 色块 |
| 低 | NPC、传送门 | 不做 |

出动画时锁定 chibi 的脸、双色发、破白袍、刺环、羽披，动作条面朝右、脚底对齐。敌人 sheet 放 `Assets/Art/Enemies/{biome}/sprites/`，立绘继续留在图鉴。

---

## 13. 每步自检

- [ ] 全项目 PPU=256，角色高约 2 单位
- [ ] 黑底 sheet 已切、已命名、Pivot 按类型
- [ ] 主角不会被翅膀/裙摆碰撞卡住
- [ ] 存档两态切换正确，死亡能读回
- [ ] 能力门在未持有时过不去，持有后切「破开」图
- [ ] 硬顺序无法跳过（没有二段跳就上不了钟塔断桥）
- [ ] 五区换皮只换 Sprite，不复制脚本
- [ ] 立绘没有被当成行走图直接拖进 Tilemap

做完第 10 节的 **F**，现有资源就已经变成一个可玩原型，而不是资源文件夹。
