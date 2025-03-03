using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngineInternal.Input;

namespace Scoops.patches
{
    public static class InputActionSpongePatches
    {
        public static PlayerActions Actions;

        private static bool enabled = false;

        public static void Init()
        {
            Actions = new PlayerActions();

            // These may not help, but they also may help, so they can stay for now
            InputSystem.settings.SetInternalFeatureFlag("USE_OPTIMIZED_CONTROLS", true);
            InputSystem.settings.SetInternalFeatureFlag("USE_READ_VALUE_CACHING", true);
        }

        public static void Enable()
        {
            if (!enabled && Actions != null)
            {
                Actions.Enable();
            }
        }

        [HarmonyPatch(typeof(InputActionRebindingExtensions.DeferBindingResolutionWrapper))]
        [HarmonyPatch("Dispose")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> DeferBindingResolutionWrapper_Dispose_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 2; i++)
            {
                if (code[i].LoadsField(AccessTools.Field(typeof(InputActionMap), "s_DeferBindingResolution")) && code[i + 1].opcode == OpCodes.Brtrue_S && code[i + 2].Calls(AccessTools.Method(typeof(InputActionState), "DeferredResolutionOfBindings")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveRange(insertionIndex, 3);
            }

            return code;
        }

        // This could be a transpiler, but I'd rather rework the whole system honestly. I don't like how often this is called.
        [HarmonyPatch(typeof(PlayerControllerB))]
        [HarmonyPatch("Look_performed")]
        [HarmonyPrefix]
        private static bool PlayerControllerB_Look_performed(ref PlayerControllerB __instance, ref InputAction.CallbackContext context)
        {
            if ((!__instance.IsOwner || !__instance.isPlayerControlled || (__instance.IsServer && !__instance.isHostPlayerObject)) && !__instance.isTestingPlayer)
            {
                return false;
            }
            if (__instance.quickMenuManager.isMenuOpen || __instance.inSpecialMenu)
            {
                if (context.ReadValue<Vector2>().magnitude > 0.001f)
                {
                    Cursor.visible = true;
                }
                return false;
            }
            StartOfRound.Instance.localPlayerUsingController = !InputControlPath.MatchesPrefix("<Mouse>", context.control);
            return false;
        }

        [HarmonyPatch(typeof(PlayerControllerB))]
        [HarmonyPatch("Awake")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PlayerControllerB_Awake_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode == OpCodes.Newobj && code[i + 1].StoresField(AccessTools.Field(typeof(PlayerControllerB), "playerActions")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveAt(insertionIndex);
                code.Insert(insertionIndex, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(InputActionSpongePatches), "Actions")));
            }

            return code;
        }

        [HarmonyPatch(typeof(PlayerControllerB))]
        [HarmonyPatch("OnDisable")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PlayerControllerB_OnDisable_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            // Remove the Enable call
            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 6; i++)
            {
                if (code[i].opcode == OpCodes.Ldarg_0 && code[i + 1].LoadsField(AccessTools.Field(typeof(PlayerControllerB), "playerActions"))
                    && code[i + 2].opcode == OpCodes.Callvirt && code[i + 3].opcode == OpCodes.Stloc_1 && code[i + 4].opcode == OpCodes.Ldloca_S
                    && code[i + 5].Calls(AccessTools.Method(typeof(PlayerActions.MovementActions), "Enable")))
                {
                    insertionIndex = i;
                    break;
                }
            }
            
            if (insertionIndex != -1)
            {
                code.RemoveRange(insertionIndex, 6);
            }

            // Remove the Disable call
            insertionIndex = -1;
            for (int i = 0; i < code.Count - 6; i++)
            {
                if (code[i].opcode == OpCodes.Ldarg_0 && code[i + 1].LoadsField(AccessTools.Field(typeof(PlayerControllerB), "playerActions")) 
                    && code[i + 2].opcode == OpCodes.Callvirt && code[i + 3].opcode == OpCodes.Stloc_1 && code[i + 4].opcode == OpCodes.Ldloca_S 
                    && code[i + 5].Calls(AccessTools.Method(typeof(PlayerActions.MovementActions), "Disable")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                List<Label> labels = code[insertionIndex].labels;
                code.RemoveRange(insertionIndex, 6);
                code[insertionIndex].labels.AddRange(labels);
            }

            return code;
        }

        // I could use a transpiler for this, but it's one line anyway
        [HarmonyPatch(typeof(DisableMouseInMenu))]
        [HarmonyPatch("Awake")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> DisableMouseInMenu_Awake_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode == OpCodes.Newobj && code[i + 1].StoresField(AccessTools.Field(typeof(DisableMouseInMenu), "actions")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveAt(insertionIndex);
                code.Insert(insertionIndex, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(InputActionSpongePatches), "Actions")));
            }

            return code;
        }
        
        [HarmonyPatch(typeof(HUDManager))]
        [HarmonyPatch("Awake")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> HUDManager_Awake_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode == OpCodes.Newobj && code[i + 1].StoresField(AccessTools.Field(typeof(HUDManager), "playerActions")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveAt(insertionIndex);
                code.Insert(insertionIndex, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(InputActionSpongePatches), "Actions")));
            }

            return code;
        }

        [HarmonyPatch(typeof(HUDManager))]
        [HarmonyPatch("OnDisable")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> HUDManager_OnDisable_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 6; i++)
            {
                if (code[i].opcode == OpCodes.Ldarg_0 && code[i + 1].LoadsField(AccessTools.Field(typeof(HUDManager), "playerActions"))
                    && code[i + 2].opcode == OpCodes.Callvirt && code[i + 3].opcode == OpCodes.Stloc_0 && code[i + 4].opcode == OpCodes.Ldloca_S
                    && code[i + 5].Calls(AccessTools.Method(typeof(PlayerActions.MovementActions), "Disable")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveRange(insertionIndex, 6);
            }

            return code;
        }

        [HarmonyPatch(typeof(InitializeGame))]
        [HarmonyPatch("Awake")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> InitializeGame_Awake_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode == OpCodes.Newobj && code[i + 1].StoresField(AccessTools.Field(typeof(InitializeGame), "playerActions")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveAt(insertionIndex);
                code.Insert(insertionIndex, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(InputActionSpongePatches), "Actions")));
            }

            return code;
        }

        [HarmonyPatch(typeof(InitializeGame))]
        [HarmonyPatch("OnDisable")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> InitializeGame_OnDisable_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 6; i++)
            {
                if (code[i].opcode == OpCodes.Ldarg_0 && code[i + 1].LoadsField(AccessTools.Field(typeof(InitializeGame), "playerActions"))
                    && code[i + 2].opcode == OpCodes.Callvirt && code[i + 3].opcode == OpCodes.Stloc_0 && code[i + 4].opcode == OpCodes.Ldloca_S
                    && code[i + 5].Calls(AccessTools.Method(typeof(PlayerActions.MovementActions), "Disable")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveRange(insertionIndex, 6);
            }

            return code;
        }

        [HarmonyPatch(typeof(ShipBuildModeManager))]
        [HarmonyPatch("Awake")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ShipBuildModeManager_Awake_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode == OpCodes.Newobj && code[i + 1].StoresField(AccessTools.Field(typeof(ShipBuildModeManager), "playerActions")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveAt(insertionIndex);
                code.Insert(insertionIndex, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(InputActionSpongePatches), "Actions")));
            }

            return code;
        }

        [HarmonyPatch(typeof(ShipBuildModeManager))]
        [HarmonyPatch("OnDisable")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ShipBuildModeManager_OnDisable_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 6; i++)
            {
                if (code[i].opcode == OpCodes.Ldarg_0 && code[i + 1].LoadsField(AccessTools.Field(typeof(ShipBuildModeManager), "playerActions"))
                    && code[i + 2].opcode == OpCodes.Callvirt && code[i + 3].opcode == OpCodes.Stloc_0 && code[i + 4].opcode == OpCodes.Ldloca_S
                    && code[i + 5].Calls(AccessTools.Method(typeof(PlayerActions.MovementActions), "Disable")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveRange(insertionIndex, 6);
            }

            return code;
        }

        [HarmonyPatch(typeof(Terminal))]
        [HarmonyPatch("Awake")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Terminal_Awake_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode == OpCodes.Newobj && code[i + 1].StoresField(AccessTools.Field(typeof(Terminal), "playerActions")))
                {
                    insertionIndex = i;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.RemoveAt(insertionIndex);
                code.Insert(insertionIndex, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(InputActionSpongePatches), "Actions")));
            }

            return code;
        }
    }
}
