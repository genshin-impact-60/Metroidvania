# 下一批评需出图清单

按优先级补齐类银河恶魔城玩法向素材。命名与现有风格对齐：`Assets/Art/...`，黑底、侧视、哥特灰金气质（诅咒朝圣者线）。

## 已出图（AI 初稿，可再精修）

| 文件 | 状态 |
|------|------|
| `map_interact_save_cathedral.png` | 已生成 |
| `map_interact_save_forest.png` | 已生成 |
| `map_interact_save_crypt.png` | 已生成 |
| `map_interact_save_cavern.png` | 已生成 |
| `map_interact_save_clocktower.png` | 已生成 |
| `map_hazards_cathedral.png` | 已生成 |
| `map_hazards_forest.png` | 已生成 |
| `map_hazards_crypt.png` | 已生成 |
| `map_hazards_cavern.png` | 已生成 |
| `map_hazards_clocktower.png` | 已生成 |
| `map_doors_common.png` | 已生成 |
| `map_doors_ability_gates.png` | 已生成 |
| `map_interact_switches.png` | 已生成 |
| `map_interact_chest.png` | 已生成 |
| `map_pickups_consumables.png` | 已生成 |
| `map_pickups_keys.png` | 已生成 |
| `map_pickups_abilities.png` | 已生成 |
| `map_traversal_common.png` | 已生成 |
| `map_platforms_moving.png` | 已生成 |
| `map_breakables_common.png` | 已生成 |
| `map_secrets_walls.png` | 已生成 |

路径均在 `Assets/Art/Environment/`。

**出图约定（全表通用）**

- 背景：纯黑 `#000000`
- 视角：2D 侧视，与现有 tileset / props 同比例感
- 单张图：同类物件拼一张 sheet（黑底分行排列），或按「一物件一文件」拆分
- 状态：可交互物尽量给 **默认 / 激活或打开 / 损坏** 三态（能画几态画几态）
- 尺寸：props 以现有 `map_props_*` 为参考；陷阱、门、拾取物略偏可读、略夸张轮廓

---

## P0 — 没有就难做关卡（先画）

### 1. 统一存档点（全图通用符号 + 五区换皮）

| 文件 | 内容 | 构图要求 |
|------|------|----------|
| `Assets/Art/Environment/map_interact_save_cathedral.png` | 圣坛存档：残破祭台 + 苍金烛火 / 倒十字微光 | 侧视；未激活（熄）/ 已激活（金焰脉动）两态 |
| `Assets/Art/Environment/map_interact_save_forest.png` | 苔石圣环 / 发光蓝花祭台 | 同上两态；色板对齐森林青绿 |
| `Assets/Art/Environment/map_interact_save_crypt.png` | 骨坛圣灯 / 翼骷髅烛台 | 冷蓝火；两态 |
| `Assets/Art/Environment/map_interact_save_cavern.png` | 潮汐喷泉圣龛（可复用喷泉气质） | 青蓝水体微光；两态 |
| `Assets/Art/Environment/map_interact_save_clocktower.png` | 黄铜座钟圣龛 / 骑士胸像前灯盏 | 暖铜光；两态 |

**识别规则**：五区外形不同，但都有「底座 + 中央发光核」，玩家远距离能认出是同一类设施。

---

### 2. 陷阱包（每区 2～3 种静态危险即可开做）

| 文件 | 内容 | 构图要求 |
|------|------|----------|
| `Assets/Art/Environment/map_hazards_cathedral.png` | ① 地刺（铁尖+石座）② 吊刺（天花板倒刺）③ 坠灯/坠烛台（可砸落暗示） | 刺尖高对比；给 1～2 格宽地板刺条 + 单刺变体 |
| `Assets/Art/Environment/map_hazards_forest.png` | ① 荆棘地刺 ② 毒孢池（地面沼）③ 扎人藤蔓卷须 | 绿/毒色；孢池要有「踩上去会死」的可读轮廓 |
| `Assets/Art/Environment/map_hazards_crypt.png` | ① 骨刺地刺 ② 摆棺暗示（吊棺）③ 毒雾地面（淡紫雾贴片） | 骨白+冷紫；吊棺可单独做摆动锚点 |
| `Assets/Art/Environment/map_hazards_cavern.png` | ① 珊瑚/锈铁刺 ② 电流水面贴片 ③ 间歇气泡喷口（危险版） | 与装饰喷口区分：危险版加电弧/血红提示 |
| `Assets/Art/Environment/map_hazards_clocktower.png` | ① 旋转锯片（侧视圆锯）② 蒸汽喷口 ③ 齿轮碾压缝（半齿轮+齿槽） | 锯片给静止帧即可；蒸汽口分关/喷两态 |

---

### 3. 门与能力封印

| 文件 | 内容 | 构图要求 |
|------|------|----------|
| `Assets/Art/Environment/map_doors_common.png` | ① 普通拱门（开/关）② 钥匙锁门（关+锁孔高亮）③ 头目大门（更厚重、纹章） | 与大教堂拱门气质一致；开/关并排 |
| `Assets/Art/Environment/map_doors_ability_gates.png` | 能力门四件：① 双跳/高墙印记 ② 冲刺裂隙门 ③ 水下密封门 ④ 时间黄铜门 | 每扇门中央有能力图标浮雕；关态为主，可附「已解锁」残破态 |
| `Assets/Art/Environment/map_pickups_keys.png` | ① 普通钥匙 ② 头目钥匙 ③ 区域纹章钥匙×5（可简化为一枚+五色） | 小图标可读；侧视或 3/4 皆可，统一阴影 |

---

### 4. 攀爬与移动结构

| 文件 | 内容 | 构图要求 |
|------|------|----------|
| `Assets/Art/Environment/map_traversal_common.png` | ① 石/铁梯子（竖条可平铺）② 铁链爬绳 ③ 木/藤梯（森林用） | 给出可 tile 的中段 + 顶/底端 |
| `Assets/Art/Environment/map_platforms_moving.png` | ① 石质移动平台 ② 齿轮托盘平台（钟塔）③ 漂木/气囊平台（洞窟） | 侧视平台面清晰；底部可加挂链/活塞暗示运动 |

---

### 5. 可破坏与秘密

| 文件 | 内容 | 构图要求 |
|------|------|----------|
| `Assets/Art/Environment/map_breakables_common.png` | ① 可砸石瓮 ② 木箱/圣物箱 ③ 墓穴骨瓮（复用 crypt 气质） | 完整 / 碎裂两态 |
| `Assets/Art/Environment/map_secrets_walls.png` | ① 裂墙砖（可炸/可砸提示）② 假地面裂纹 ③ 藤蔓遮挡暗门 | 比普通墙「更碎、更空」，但不要画得太跳戏 |

---

### 6. 机关（与门/桥成套）

| 文件 | 内容 | 构图要求 |
|------|------|----------|
| `Assets/Art/Environment/map_interact_switches.png` | ① 地板压力板（起/压下）② 拉杆（左/右或上/下）③ 墙上符文开关（熄/亮）④ 曲柄轮 | 钟塔已有拉杆气质可统一进此表；压力板要够扁、好贴地 |

---

### 7. 拾取物与宝箱

| 文件 | 内容 | 构图要求 |
|------|------|----------|
| `Assets/Art/Environment/map_pickups_consumables.png` | ① 生命瓶/圣血滴 ② 货币魂屑 ③ 地图碎片 ④ 弹药/法力晶 | 小图标；发光描边便于地上辨认 |
| `Assets/Art/Environment/map_pickups_abilities.png` | 能力拾取物 4～6 个（双跳、冲刺、墙跳、游泳/潜水、滑翔等） | 悬浮圣物/徽章造型；比消耗品更大一圈 |
| `Assets/Art/Environment/map_interact_chest.png` | 通用宝箱：关 / 开；可附「稀有箱」变体 | 洞窟箱可作主形，再出灰金圣堂变体一枚 |

---

## P1 — 能打能跑（紧接 P0）

### 8. 主角侧视动作（诅咒朝圣者 chibi）

目录建议：`Assets/Art/Characters/Animations/`

尺度、透明底、PPU、脚底 pivot、验收清单见 **[protagonist_sprite_spec.md](./protagonist_sprite_spec.md)**（对齐 idle 站立 ~872px，勿再按画布框随意缩小）。

| 文件 | 内容 | 构图要求 |
|------|------|----------|
| `protagonist_cursed_pilgrim_chibi_idle.png` | 呼吸待机 4～6 帧（或单帧+注明可后补） | 与现有 chibi 同比例、同朝向（建议统一面朝右） |
| `protagonist_cursed_pilgrim_chibi_run.png` | 已有则可复查；补齐循环帧 | 脚底对齐基准线；尺度锁 idle |
| `protagonist_cursed_pilgrim_chibi_jump.png` | 起跳 / 升空 / 顶点 / 下落 | 头宽锁 idle；空中也脚踩底线 |
| `protagonist_cursed_pilgrim_chibi_attack.png` | 近战 3～5 帧（挥刃或刺击） | 攻击帧带简易刀光也可另出 VFX |
| `protagonist_cursed_pilgrim_chibi_hurt.png` | 受击闪白姿态 | |
| `protagonist_cursed_pilgrim_chibi_death.png` | 倒地 / 消散 2～4 帧 | |
| `protagonist_cursed_pilgrim_chibi_crouch.png` | 蹲/滑（若有滑铲再单出） | 可选，P1 末 |

---

### 9. 杂兵侧视（每区先做 1 个普通怪）

目录：`Assets/Art/Enemies/{biome}/sprites/`

| 文件 | 优先敌人 | 最少帧 |
|------|----------|--------|
| `enemy_cathedral_broken_acolyte_sheet.png` | 碎誓侍僧 | idle / walk / attack / hurt / death |
| `enemy_forest_mossbound_lurker_sheet.png` | 苔缚潜伏者 | 同上 |
| `enemy_crypt_sepulcher_thrall_sheet.png` | 墓仆 | 同上 |
| `enemy_cavern_jelly_wisp_sheet.png` | 水母灵（飞行） | idle 浮游 / 突进 / hurt / death |
| `enemy_clocktower_rivet_automaton_sheet.png` | 铆钉自动机 | idle / walk / attack / hurt / death |

精英与头目可仍用立绘占位，侧视动作放到 P2。

---

## P2 — 更好玩 / 更完整

### 10. 传送与枢纽

| 文件 | 内容 |
|------|------|
| `Assets/Art/Environment/map_interact_warp.png` | 快捷传送门/镜：未发现 / 已激活；可用五区换色小变体 |

### 11. NPC（先 2～3 个）

| 文件 | 内容 |
|------|------|
| `Assets/Art/Characters/npc_merchant_chibi.png` | 商人（站立） |
| `Assets/Art/Characters/npc_blacksmith_chibi.png` | 铁匠 / 修装 |
| `Assets/Art/Characters/npc_lorekeeper_chibi.png` | 叙事 NPC |

### 12. 液体与特殊地形瓦片

| 文件 | 内容 |
|------|------|
| `Assets/Art/Environment/map_tileset_liquid_cavern.png` | 水面线、水下填充、泡沫边、浅滩 |
| `Assets/Art/Environment/map_tileset_hazard_floor.png` | 毒沼/酸/电水面可平铺条 |

### 13. VFX 小表

| 文件 | 内容 |
|------|------|
| `Assets/Art/VFX/vfx_hit_slash.png` | 刀光 / 受击火花 |
| `Assets/Art/VFX/vfx_dust_land.png` | 落地尘 |
| `Assets/Art/VFX/vfx_save_activate.png` | 存档激活光环帧 |
| `Assets/Art/VFX/vfx_pickup_burst.png` | 拾取闪光 |

### 14. UI（匹配灰金哥特）

| 文件 | 内容 |
|------|------|
| `Assets/Art/UI/ui_hud_health.png` | 血槽框 + 填充条 |
| `Assets/Art/UI/ui_hud_currency_icon.png` | 货币图标 |
| `Assets/Art/UI/ui_map_frame.png` | 地图框 / 房间格样式 |
| `Assets/Art/UI/ui_inventory_slot.png` | 物品栏格 |

---

## 推荐出图批次（可按周推进）

| 批次 | 交付文件 | 目的 |
|------|----------|------|
| **Week A** | 存档×5 + 陷阱×5 + 门组 + 钥匙 | 能摆「安全点—危险—门」的白模关 |
| **Week B** | 梯子绳 + 移动平台 + 开关 + 可破坏 + 秘密墙 | 垂直探索与解谜 |
| **Week C** | 拾取物 + 宝箱 + 能力物 | 奖励闭环 |
| **Week D** | 主角动作全套 + 1 区杂兵 sheet | 可试玩战斗 |
| **Week E** | 其余 4 区杂兵 sheet + 传送 + 液体瓦片 | 五区可循环 |
| **Week F** | NPC + VFX + HUD | 打磨与叙事 |

---

## 验收自检（每张图画完对一下）

- [ ] 黑底、侧视、比例与现有 props 不违和
- [ ] 危险物轮廓在缩小到游戏尺寸后仍可辨认
- [ ] 存档点五区不同皮，但同属「底座+光核」
- [ ] 门/开关有明确开闭或亮灭态
- [ ] 文件名与本表路径一致，方便直接丢进 Unity

---

## 与现有资源的关系（避免重画）

| 已有 | 用法 |
|------|------|
| `map_props_*` | 继续当装饰；洞窟喷泉/水晶、墓穴剑台可**降级为氛围**，正式存档以本表 `map_interact_save_*` 为准 |
| `map_props_clocktower` 拉杆/阀门 | 可并入或对照 `map_interact_switches` 统一比例后复用 |
| `map_props_cavern` 宝箱 | 可直接作为 `map_interact_chest` 主形，再补开态 |
| `map_props_crypt` 铁门 | 可作锁门参考，正式门组以 `map_doors_*` 为准 |
| 敌人立绘 | 保留给图鉴/开场；关卡内用 `sprites/*_sheet` |
| `protagonist_cursed_pilgrim_chibi*.png` | 锁定主角外观，动作条严格跟脸与服装 |
