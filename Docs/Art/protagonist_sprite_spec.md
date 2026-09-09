# 主角侧视动作出图规范（诅咒朝圣者 chibi）

`asset_checklist.md` 管「画哪些动作」；本文管「怎么画才能直接进 Unity、切换时不缩水/不跳脚」。

目录：`Assets/Art/Characters/Animations/{action}/`（每帧一张 PNG）

---

## 1. 锁定基准

| 项 | 值 |
|----|-----|
| 基准图 | `Animations/idle/idle_0.png` |
| 站立内容高 | 约 **872px**（刺环顶 → 脚底不透明像素） |
| 朝向 | 统一 **面朝右**（3/4 或侧视与 idle 一致） |
| 外观 | 头、白发缕、刺环、碎翼披风、胸甲十字、护臂护胫与 idle 一致 |

**尺度怎么对：**

- 用 **头宽 / 站立剪影高** 对齐 idle，不要用画布框高。
- 蹲、躺等姿势总高度本来更矮，这是正常的；只要头身比例与 idle 一致即可。
- 相对 idle 站立内容高，偏差应在 **±5%** 内。
- **禁止**在代码里对某一套动作单独改 `localScale` 做补偿。

参考实测（同 PPU、同 Visual scale 下）：

| 动作 | 站立/典型内容高 | 相对 idle |
|------|-----------------|-----------|
| idle | ~872px | 100%（基准） |
| run | ~771px | ~88%（历史略小；新图尽量按 idle） |
| jump（对齐后） | 升空帧 ~872px | 100% |

新动作一律按 **对齐 idle** 出图；不要再对齐 run。

---

## 2. Canvas / 单帧布局

- **每帧一张 PNG**，按动作放在子目录：`Animations/{action}/{action}_N.png`。
- 单帧画布建议：**宽 600～780 × 高 ~900**（idle 约 `614×898`）。
- **同动作全帧脚踩同一底线**（脚底对齐）；画布尺寸尽量一致。
- 空中动作也画在底线上：跳跃高度由游戏 Transform 负责，**不要**在画布里把角色画得「飘在格子上半」。
- 导出：**RGBA 透明底**。环境 props 清单里的「纯黑底」不适用于主角动作；黑底进引擎会被当成实体像素。
- 帧名：`{action}_0`、`{action}_1`… 按播放顺序编号。

### AI 出图 / 抠图流程（Cursor）

Cursor 图像生成**没有真正 alpha**，不要直接要「透明底」。标准流程：

1. 生成在纯绿幕 `#00FF00` 上（勿用黑底/奶油白底；黑发与白碎翼会被误抠）。
2. 按帧用 Python `rembg` 抠透明，模型优先 **`birefnet-general-lite`**；本角色勿用 `isnet-anime`（易掏空）。
3. 去绿边溢色后写出单帧：全帧脚底共线；站立/伸展帧内容高锁 idle（±5%）。
4. 写入 `Animations/{action}/`；需要时可另存绿幕源图在 `Tools/` 下。

细则见 `.cursor/rules/character-sprite-cutout.mdc`。

### Unity 导入

| 项 | 值 |
|----|-----|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | **Single** |
| Pixels Per Unit | **256** |
| Pivot | **Bottom**，`(0.5, 0)` |
| Mesh Type | Tight 可；与现有 idle/run 一致即可 |
| Alpha Is Transparency | 开启 |

可用 `Tools/install_anim_frames.py` 从拆帧目录批量写入 meta。

---

## 3. 各动作帧内容

| 动作 | 目录 | 最少帧 | 顺序 / 要点 |
|------|------|--------|-------------|
| idle | `Animations/idle/` | 4 | 呼吸循环；脚几乎不动；**空手**（取残誓前） |
| idle（持刃） | `Animations/idle_oathblade/` | 4 | 同呼吸循环；右手垂持残誓；尺度锁 idle；帧名 `idle_oathblade_0…3` |
| run | `Animations/run/` | 8 | 循环跑；可略前倾；尺度锁 idle |
| jump | `Animations/jump/` | 4 | `0` 起跳蹲 → `1` 升空 → `2` 顶点 → `3` 下落 |
| getup | `Animations/getup/` | 6～7 | 躺 → 撑起 → 站起；末帧尽量贴近 idle_0 |
| attack | `Animations/attack/` | 3～5 | 持 **污光断剑·残誓** 挥刃/刺击；造型与帧顺序见 [protagonist_weapon_spec.md](./protagonist_weapon_spec.md)；刀光优先另出 VFX |
| hurt | `Animations/hurt/` | 1～2 | 受击后仰/缩 |
| death | `Animations/death/` | 2～4 | 倒地 / 消散 |
| crouch | `Animations/crouch/` | 可选 | 蹲或滑；头宽仍锁 idle |

### Jump 补充

- 引擎按竖直速度切帧：短时起跳蹲 → 上升 → 顶点 → 下落。
- 蹲帧总高低没关系；**头宽必须与 idle 同级**，否则会像整个人缩小。
- 次要运动：刺环延迟跟随；碎翼/长发上升时下拖、下落时上扬；甲靴落地要有重量感。

### 次要运动（全动作通用）

- **刺环**：相对头略延迟，勿焊死在同一像素偏移。
- **碎翼披风 / 长发**：跟随速度方向拖曳，空中比待机更散开。
- **护甲**：肢体可张开维持平衡，避免软飘。

---

## 4. 出图自检

- [ ] 面朝右；脸与服装可叠在 idle 上对得上
- [ ] 站立或升空等「伸展」帧，内容高相对 idle 在 ±5% 内
- [ ] 全帧脚底共线；透明底（边角 alpha = 0）
- [ ] 帧名 `{action}_0…n` 顺序正确
- [ ] Unity pivot 在脚底；与 idle 切换无突然缩小、无脚底打滑
- [ ] 未用代码 scale 补偿本套动作

---

## 5. 与清单 / 代码的关系

| 文档或代码 | 职责 |
|------------|------|
| `Docs/Art/asset_checklist.md` | 还缺哪些文件、优先级 |
| 本文 | 主角动作像素尺度与导入约定 |
| [protagonist_weapon_spec.md](./protagonist_weapon_spec.md) | 主武器「残誓」造型、持刃帧与刀光 VFX 边界 |
| `PlayerAnimationLoader` | 从 `Animations/{action}/` 加载单帧 |
| `StepAPlayground` / `CathedralIntroZoneA` | 调用 loader 并把帧交给玩家 |
| `PlayerSpriteAnimator` | idle / run / jump / getup 播放逻辑 |

历史说明：早期 jump 原稿约只有 idle 一半像素高，进游戏会明显缩水；已按本文放大对齐 idle。后续 attack 等勿再犯同一问题。
