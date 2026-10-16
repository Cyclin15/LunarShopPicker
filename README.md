# LunarShopPicker

时之隙集市（Bazaar Between Time）月轮商品（Lunar Buds）购买自选的 BepInEx 插件。

## 行为

- 购买月轮商品（2 月轮币的 5 个货架）后，掉出的不再是货架上预抽的那件，
  而是 Command 神器式的选择方块，从**全部已解锁月轮物品**（一页网格全显示）中任选一件。
- 价格、限购一次、刷新祭坛、解锁规则全部原版不变。
- 只在时之隙集市（bazaar 场景）生效；多商店、3D 打印机、装备终端、净化池、
  普通掉落、以及其他地图可能出现的单个月轮终端均不受影响。
- 与 Command 神器兼容（神器开启时游戏本来就是同样行为）。
- 仅主机/单人端生效（服务器权威）。

## 实现（RoR2 1.5.0）

- Harmony prefix 拦截 `ShopTerminalBehavior.DropPickup(bool)`：
  当前场景为 bazaar 且商品品阶为 Lunar 时，复刻原版账目逻辑后给
  `CreatePickupInfo` 打上 `PickupArtifactFlag.Command` 并跳过原版掉落。
- 掉落物落地时游戏原生的 `PickupDropletController.CreateCommandCube` 路径
  把它变成选择方块，选项由原版 `SetOptionsFromPickupForCommandArtifact`
  生成（月轮品阶全部物品，未解锁置灰）。零自定义 UI / 网络逻辑。

## 构建

```
dotnet build -c Release
```

编译后自动部署到 r2modman Default profile 的 `BepInEx/plugins/LunarShopPicker/`。

`recon/` 目录是反编译参考代码，不参与编译。
