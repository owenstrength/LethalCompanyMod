﻿using BepInEx;
using GameNetcodeStuff;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System;
using static MolesterLootBug.Plugin;
using System.Reflection;

namespace MolesterLootBug
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        private const string PLUGIN_GUID = "EvilLootBugByOwen.1.10";
        private const string PLUGIN_NAME = "Molester Loot Bug";
        private const string PLUGIN_VERSION = "0.2";

        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);

        // Plugin startup logic
        //Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        private static Plugin Instance;

        internal static ManualLogSource mls;
        internal static System.Random random = new System.Random();

        private static float bugHoldTime = 8.0f;

        void Awake()
        {
            // entry point of mod

            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_GUID);

            mls.LogInfo("Mod Awake OWEN GOATED");

            harmony.PatchAll(typeof(Plugin));

        }

        public class BeingHeldData : MonoBehaviour
        {
            public bool isHeld = false;
            public HoarderBugAI HoarderInstance = null;
            public PlayerControllerB player = null;
            public float holdTimeRemaining = bugHoldTime;
        }

        public class HoldingData : MonoBehaviour
        {
            public bool isHoldingPlayer = false;
            public PlayerControllerB HeldPlayer = null;
            public float CanHoldTime = bugHoldTime;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Awake")]
        [HarmonyPostfix]
        static void AwakeUpdate(PlayerControllerB __instance)
        {
            Plugin.mls.LogInfo("Attempting to Load instance Data, Player Awake Patch");

            __instance.gameObject.AddComponent<BeingHeldData>();
            var isHeldData = __instance.gameObject.GetComponent<BeingHeldData>();
            isHeldData.player = __instance;
            Plugin.mls.LogInfo($"Is Being Held ON AWAKE? {isHeldData.isHeld}");
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        static void PlayerUpdate(PlayerControllerB __instance, ref Vector3 ___serverPlayerPosition, ref bool ___snapToServerPosition)
        {
            var isHeldData = __instance.gameObject.GetComponent<BeingHeldData>();

            if (isHeldData.isHeld)
            {
                // Move the player based on the Bug's position LEGACY
                // This was super choppy
                //___serverPlayerPosition = isHeldData.HoarderInstance.serverPosition;
                //__instance.thisController.transform.localPosition = isHeldData.HoarderInstance.serverPosition;
                //___snapToServerPosition = true;

                // A better way to move to the bug would be to find the Vector distance from the lootbug
                // use simple move method
                
                Vector3 bugPos = isHeldData.HoarderInstance.serverPosition;
                Vector3 playPos = __instance.gameObject.transform.position;
                Vector3 dest = bugPos - playPos;
                __instance.thisController.SimpleMove(dest * 6.0f);

                // log data
                Plugin.mls.LogInfo($"PlayerServer Pos at {__instance.serverPlayerPosition}");
                Plugin.mls.LogInfo($"Local Pos at {__instance.serverPlayerPosition}");
                Plugin.mls.LogInfo($"Loot Pos at {isHeldData.HoarderInstance.serverPosition}");
                Plugin.mls.LogInfo($"Bug Holding Player. Time left: {isHeldData.holdTimeRemaining}");

                // Reduce the hold time remaining
                isHeldData.holdTimeRemaining -= Time.deltaTime;

                Plugin.mls.LogInfo($"Is Being Held? {isHeldData.isHeld}");

                // Check if the hold time has expired
                if (isHeldData.holdTimeRemaining <= 0 || __instance.isPlayerDead || isHeldData.HoarderInstance.isEnemyDead)
                {
                    // Release the player
                    isHeldData.isHeld = false;
                    isHeldData.holdTimeRemaining = bugHoldTime;
                    isHeldData.HoarderInstance.gameObject.GetComponent<HoldingData>().CanHoldTime = bugHoldTime;

                    var isHoldingData = isHeldData.HoarderInstance.gameObject.GetComponent<HoldingData>();
                    if (isHoldingData != null)
                    {
                        isHoldingData.isHoldingPlayer = false;
                    }else
                    {
                        return;
                    }

                }
            }

        }

        [HarmonyPatch(typeof(HoarderBugAI), "Start")]
        [HarmonyPostfix]
        static void BugStart(HoarderBugAI __instance)
        {
            __instance.gameObject.AddComponent<HoldingData>();
        }

        [HarmonyPatch(typeof(HoarderBugAI), "OnCollideWithPlayer")]
        [HarmonyPostfix]
        static void BugOnCollide(HoarderBugAI __instance, Collider other)
        {
            var isHoldingData = __instance.gameObject.GetComponent<HoldingData>();

            if (isHoldingData.isHoldingPlayer == false && Plugin.random.Next(0, 2) == 0 && !__instance.isEnemyDead && isHoldingData.CanHoldTime <= 0)
            {
                var playerControllerB = other.GetComponent<PlayerControllerB>();

                if (playerControllerB != null)
                {
                    playerControllerB.gameObject.GetComponent<BeingHeldData>().isHeld = true;
                    playerControllerB.gameObject.GetComponent<BeingHeldData>().HoarderInstance = __instance;
                    __instance.gameObject.GetComponent<HoldingData>().isHoldingPlayer = true;
                    __instance.gameObject.GetComponent<HoldingData>().HeldPlayer = playerControllerB;
                }
            }
        }

        [HarmonyPatch(typeof(HoarderBugAI), "Update")]
        [HarmonyPostfix]
        static void BugUpdate(HoarderBugAI __instance)
        {
            var isHoldingData = __instance.gameObject.GetComponent<HoldingData>();

            if (isHoldingData.isHoldingPlayer)
            {
                Plugin.mls.LogInfo("Bug Holding Player.");

                __instance.angryTimer -= 2 * Time.deltaTime;
                // isAngry is private
                __instance.angryAtPlayer = null;

                __instance.timeSinceHittingPlayer = -5.0f;

                if (__instance.isEnemyDead)
                {
                    isHoldingData.isHoldingPlayer = false;
                    var heldPlayerData = isHoldingData.HeldPlayer.gameObject.GetComponent<BeingHeldData>();
                    heldPlayerData.isHeld = false;
                }

                
            } else
            {
                isHoldingData.CanHoldTime -= Time.deltaTime;
            }
        }


    }
}
