# 敌人图鉴

按区域记录敌人设定、战斗定位与立绘资源。数值与技能可在实装时再补。

## 区域索引

| 顺序 | 区域 | 图鉴 | 立绘目录 |
|------|------|------|----------|
| 1 | 大教堂 | [cathedral.md](./cathedral.md) | `Assets/Art/Enemies/cathedral/` |
| 2 | 蔓生废墟森林 | [forest.md](./forest.md) | `Assets/Art/Enemies/forest/` |
| 3 | 地下墓穴 | [crypt.md](./crypt.md) | `Assets/Art/Enemies/crypt/` |
| 4 | 水淹洞窟 | [cavern.md](./cavern.md) | `Assets/Art/Enemies/cavern/` |
| 5 | 机械钟塔 | [clocktower.md](./clocktower.md) | `Assets/Art/Enemies/clocktower/` |

## 条目模板

新建区域文件时，每个敌人建议包含：

- **ID**：英文键名（与文件名一致）
- **名称**：中文显示名
- **定位**：普通 / 精英 / 飞行 / 区域头目 等
- **外观**：与地图色板、主角气质对齐的视觉要点
- **习性**：巡逻、伏击、召唤等行为印象
- **战斗提示**：玩家应对思路（非正式数值）
- **立绘**：相对项目根目录的资源路径
