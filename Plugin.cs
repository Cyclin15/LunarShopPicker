using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace LunarShopPicker;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class LunarShopPickerPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.zcode.LunarShopPicker";
    public const string PluginName = "LunarShopPicker";
    public const string PluginVersion = "1.0.0";

    internal static ConfigEntry<bool> Enabled { get; private set; }
    internal static ManualLogSource Log { get; private set; }

    private void Awake()
    {
        Log = Logger;

        Enabled = Config.Bind("Settings", "Enabled", true,
            "总开关。开启后，时之隙集市（Bazaar Between Time）的月轮商品（Lunar Buds）购买后掉出 Command 神器式的选择方块，可从全部已解锁月轮物品中任选一件。其他商店与掉落不受影响。");

        Harmony.CreateAndPatchAll(typeof(ShopPickerPatches));

        RegisterRiskOfOptions();

        Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
    }

    // 仅在 Risk of Options 存在时调用；JIT 按方法惰性编译，缺依赖时不会执行到这里。
    private void RegisterRiskOfOptions()
    {
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
        {
            return;
        }

        RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(Enabled));
    }
}
