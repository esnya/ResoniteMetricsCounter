using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using System;

namespace ResoniteMetricsCounter.Patch;

[HarmonyPatchCategory(Category.CORE)]
[HarmonyPatch(typeof(DevCreateNewForm), nameof(DevCreateNewForm.OpenCategory))]
internal static class DevCreateNewForm_OpenCategory_Patch
{
    static void Postfix(DevCreateNewForm __instance, string path)
    {
        if (path != "/Editor") return;

        try
        {
            var uiBuilder = new UIBuilder(__instance.Slot.FindChildInHierarchy("Scroll Area").FindChild("Content"));

            uiBuilder.Style.ButtonColor = RadiantUI_Constants.BUTTON_COLOR;
            uiBuilder.Style.MinHeight = RadiantUI_Constants.GRID_CELL_SIZE;
            uiBuilder.Style.TextColor = RadiantUI_Constants.TEXT_COLOR;
            uiBuilder.Style.ButtonTextAlignment = Alignment.MiddleLeft;
            uiBuilder.Style.ButtonSprite = RadiantUI_Constants.GetButtonSprite(__instance.World);
            uiBuilder.Style.ButtonTextPadding = 4;

            var button = uiBuilder.Button("Start Metrics Counter (mod)", RadiantUI_Constants.BUTTON_COLOR);
            button.IsPressed.Changed += (_) =>
            {
                if (button.IsPressed)
                {
                    ResoniteMetricsCounterMod.Start();
                }
            };
        }
        catch (Exception e)
        {
            ResoniteMetricsCounterMod.Error($"Failed to add Resonite Profiler button");
            ResoniteMetricsCounterMod.Error(e);
        }
    }
}
