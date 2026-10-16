using System;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace LunarShopPicker;

/// <summary>
/// 把时之隙集市（Bazaar Between Time）月轮商品（Lunar Buds）的购买掉落改成
/// Command 神器式的选择方块。
///
/// 游戏原始流程：货架在进店时预抽一件月轮物品（ShopTerminalBehavior.pickup），
/// 购买时 DropPickup 掉出这件固定物品。
///
/// 本补丁在 DropPickup 前拦截：若当前场景为时之隙集市（bazaar）且商品品阶为
/// 月轮（Lunar）——只有集市里的 Lunar Buds 满足，多商店/打印机/装备终端/净化池、
/// 以及其他地图可能出现的单个月轮终端都不满足——则复刻原版的账目与掉落逻辑，
/// 但给掉落信息打上 Command 标记。掉落物落地时，游戏自己的
/// PickupDropletController.CreateCommandCube 路径会把它变成选择方块，
/// 选项列表由原版 SetOptionsFromPickupForCommandArtifact 生成：
/// 该品阶（月轮）的全部物品，未解锁的显示为不可选——与货架本身的解锁规则一致。
/// 全程不新建 UI / 网络逻辑，多人游戏天然同步。
/// </summary>
[HarmonyPatch]
internal static class ShopPickerPatches
{
    [HarmonyPatch(typeof(ShopTerminalBehavior), nameof(ShopTerminalBehavior.DropPickup), new[] { typeof(bool) })]
    [HarmonyPrefix]
    private static bool ReplaceLunarDropWithChoiceCube(ShopTerminalBehavior __instance, bool isDuplicated)
    {
        if (!LunarShopPickerPlugin.Enabled.Value || !NetworkServer.active || !IsInBazaar())
        {
            return true;
        }

        // 注意：ShopTerminalBehavior.pickup 字段在运行时程序集里是 private
        //（GameLibs 编译包做了公开化，不能直接访问），用 public 的 CurrentPickup() 读。
        UniquePickup pickup = __instance.CurrentPickup();
        PickupDef pickupDef = PickupCatalog.GetPickupDef(pickup.pickupIndex);
        if (pickupDef == null || pickupDef.itemTier != ItemTier.Lunar)
        {
            return true;
        }

        // 复刻原版 DropPickup 的账目逻辑（月轮商品 debt/dropAmount 均为 1，
        // 这里按一般形式处理以保持稳妥），但掉落带 Command 标记。
        if (__instance.dropAmount <= 0)
        {
            __instance.dropAmount = 1;
        }
        if (__instance.debt <= 0)
        {
            __instance.debt = 1;
        }
        __instance.SetHasBeenPurchased(newHasBeenPurchased: true);

        UniquePickup dropPickup = pickup;
        switch (__instance.storageType)
        {
            case ItemStorageType.Rental:
                dropPickup.isRental = true;
                break;
            case ItemStorageType.Temporary:
                dropPickup.decayValue = 1f;
                break;
        }

        Vector3 position = (__instance.dropTransform ? __instance.dropTransform : __instance.transform).position;
        Vector3 velocity = __instance.transform.TransformVector(__instance.dropVelocity);
        for (int i = 0; i < __instance.debt; i++)
        {
            for (int j = 0; j < __instance.dropAmount; j++)
            {
                GenericPickupController.CreatePickupInfo pickupInfo = default(GenericPickupController.CreatePickupInfo);
                pickupInfo.rotation = Quaternion.identity;
                pickupInfo.pickup = dropPickup;
                pickupInfo.position = position;
                pickupInfo.duplicated = isDuplicated;
                pickupInfo.artifactFlag |= GenericPickupController.PickupArtifactFlag.Command;
                PickupDropletController.CreatePickupDroplet(pickupInfo, position, velocity);
            }
        }
        __instance.debt = 0;

        LunarShopPickerPlugin.Log.LogInfo(
            $"Lunar bud purchase converted to a choice cube: {pickupDef.nameToken} offered as full lunar selection.");
        return false;
    }

    // 只在时之隙集市（bazaar）生效；其他地图可能存在的单个月轮终端不受影响。
    private static bool IsInBazaar()
    {
        SceneDef sceneDef = SceneInfo.instance ? SceneInfo.instance.sceneDef : null;
        return sceneDef != null
            && string.Equals(sceneDef.baseSceneName, "bazaar", StringComparison.OrdinalIgnoreCase);
    }
}
