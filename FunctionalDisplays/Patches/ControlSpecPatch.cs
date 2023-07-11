using FunctionalDisplays.Items;
using DV.CabControls.Spec;
using HarmonyLib;

namespace FunctionalDisplays.Patches;

[HarmonyPatch(typeof(ControlSpec), "Awake")]
public static class ControlSpec_Awake_Patch
{
    private static void Postfix(ControlSpec __instance)
    {
        InventoryItemSpec spec = __instance.GetComponent<InventoryItemSpec>();
        if (spec == null)
            return;
        if (DisplayableItem.DisplayItems.TryGetValue(spec.ItemPrefabName, out DisplayableItem displayItem))
            displayItem.Setup(spec.transform);
    }
}
