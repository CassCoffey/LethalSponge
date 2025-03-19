using HarmonyLib;
using MoreCompany.Cosmetics;
using Scoops.service;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Scoops.compatibility
{
    internal static class MoreCompanyCompat
    {
        public static bool Enabled =>
            BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("me.swipez.melonloader.morecompany");

        [HarmonyPatch(typeof(CosmeticRegistry))]
        [HarmonyPatch("LoadCosmeticsFromBundle")]
        [HarmonyPostfix]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void CosmeticRegistry_LoadCosmeticsFromBundle()
        {
            DecimateAllCosmetics();
        }

        [HarmonyPatch(typeof(CosmeticRegistry))]
        [HarmonyPatch("LoadCosmeticsFromAssembly")]
        [HarmonyPostfix]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void CosmeticRegistry_LoadCosmeticsFromAssembly(CosmeticGeneric __instance)
        {
            DecimateAllCosmetics();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void DecimateAllCosmetics()
        {
            foreach (CosmeticInstance instance in CosmeticRegistry.cosmeticInstances.Values)
            {
                MeshService.DecimateAllMeshes(instance.gameObject);
            }
        }
    }
}
