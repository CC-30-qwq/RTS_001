# RTS_001

基于 Unity 2022.3（URP）开发的 **RTS（即时战略）玩法原型**，实现了网格建造、单位索敌与多策略战斗三块核心系统。

- 建筑放置走**网格吸附 + 占位校验**；
- 大规模索敌使用 **Unity Burst + Job System** 并行计算；
- 单位战斗行为通过**策略模式**（`ICombatStrategy`）解耦，近战 / 远程 / 法师可自由组合。

## 技术栈

| 分类 | 技术 |
|---|---|
| 引擎 | Unity 2022.3.62f1c1（URP 14.0.12） |
| 高性能计算 | Burst 1.8.28 + Job System |
| 美术资源 | Mini Legions Fantasy Polyart |
| UI | UGUI |

## 核心特性

### 网格建造
- `GridManager`：世界坐标与网格坐标互转、格子占用状态维护。
- `BuildingPlacement`：鼠标指向格子、合法性校验（是否越界／已占用）、放置与取消。
- `BuildingData`：建筑尺寸、占格与属性配置。

### 索敌（Burst Job）
- `FindEnemyJob`：以 Burst 编译的 `IJobParallelFor` 批量计算单位与候选敌人的距离，返回最近目标。
- `CharacterDatas` / `TargetManager`：单位数据数组的收集、Job 调度与结果回写。
- `CharacterObj` / `CharacterStatus`：单位实体与运行时状态（血量、目标、阵营）。

### 战斗策略
- `ICombatStrategy` 定义统一战斗接口，`AttackLogic` 负责调度。
- 具体策略：
  - `MeleeCombatStrategy` — 近战
  - `RangedCombatStrategy` — 远程
  - `MageCombatStrategy` — 法师
  - `Melee_RangedStrategy` — 远近混合
- `HitCollider.cs` 命中判定、`Projectile.cs` + `ProjectilePool.cs` 投射物与对象池（避免频繁 Instantiate 造成 GC）。

### 玩家控制
- `CameraController`：RTS 典型视角控制（平移 / 缩放 / 旋转）。
- `CursorController`：光标与地面拾取，用于建造与选中。

## 项目结构

```
Assets/
├── Scripts/
│   ├── Buildings/                      # 网格建造
│   │   ├── GridManager.cs
│   │   ├── BuildingPlacement.cs
│   │   └── BuildingData.cs
│   ├── Characters/
│   │   ├── CharacterUtilty/            # CharacterObj / CharacterStatus
│   │   ├── FindEnemyUtility/           # FindEnemyJob / TargetManager / CharacterDatas
│   │   └── ICombatStrategy/
│   │       └── CombatStrategy/
│   │           ├── AttackLogic.cs
│   │           ├── ICombatStrategy.cs
│   │           └── Strategies/         # 近战 / 远程 / 法师 / 远近混合
│   │               └── Methods/        # HitCollider / Projectile / ProjectilePool
│   └── Player/                         # CameraController / CursorController
├── ArtResources/                       # Mini Legions Fantasy Polyart 美术资源
├── Scenes/
│   ├── SampleScene.unity
│   └── Scene.unity                     # 主要测试场景
├── Prefabs/ · Animators/
```

## 快速开始

1. 使用 **Unity 2022.3.62f1c1** 打开项目，等待 Package 还原（Burst 需编译，首次打开稍慢）。
2. 打开 `Assets/Scenes/Scene.unity` 运行。
3. 场景中通过光标的建造模式放置建筑，单位会自动索敌并按各自策略作战。

## 备注

`FindEnemyJob` 依赖 Burst 编译，需确保 **Jobs → Burst → Enable Compilation** 处于开启状态（默认开启），否则会回退到托管执行、失去性能收益。
