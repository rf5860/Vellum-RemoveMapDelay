using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Photon.Pun;
using UnityEngine;

// ReSharper disable InconsistentNaming, UnusedType.Global, UnusedMember.Global, UnusedParameter.Global
namespace RemoveMapDelay
{
    public class RemoveMapDelayMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Remove Chapter Transitions Mod Loaded!");
        }
    }

    // Patch to remove the sketch transition duration
    [HarmonyPatch(typeof(PostFXManager), "Awake")]
    public static class PostFXManager_Awake_Patch
    {
        public static void Postfix(PostFXManager __instance)
        {
            // Set transition duration to 0 to remove the delay
            __instance.SketchTransitionDuration = 0f;
        }
    }

    // Patch to skip the sketch fade in animation
    [HarmonyPatch(typeof(PostFXManager), "ActivateSketch")]
    public static class PostFXManager_ActivateSketch_Patch
    {
        public static bool Prefix(PostFXManager __instance, bool useSFX, bool save)
        {
            // Get the private IsInSketch field
            FieldInfo isInSketchField = AccessTools.Field(typeof(PostFXManager), "IsInSketch");
            bool isInSketch = (bool)isInSketchField.GetValue(__instance);

            if (isInSketch) return false;

            // Skip the sound effect
            // if (useSFX) PageFlip.instance.PlaySketchIntro();

            // Set IsInSketch to true
            isInSketchField.SetValue(__instance, true);

            // Immediately set the sketch effect to full instead of fading
            PostFXManager.UpdateSketch(1f);

            // Save if needed
            if (save && PageFlip.instance != null) PageFlip.instance.Save();

            // Skip the original method and coroutine
            return false;
        }
    }

    // Patch to skip the sketch fade out animation
    [HarmonyPatch(typeof(PostFXManager), "ReleaseSketch")]
    public static class PostFXManager_ReleaseSketch_Patch
    {
        public static bool Prefix(PostFXManager __instance)
        {
            // Get the private IsInSketch field
            FieldInfo isInSketchField = AccessTools.Field(typeof(PostFXManager), "IsInSketch");
            bool isInSketch = (bool)isInSketchField.GetValue(__instance);

            if (!isInSketch) return false;

            // Immediately hide the sketch effect
            PostFXManager.UpdateSketch(0f);

            // Call OnMapChangeFinished immediately
            Action onMapChangeFinished = MapManager.OnMapChangeFinished;
            onMapChangeFinished?.Invoke();

            // Disable the sketch volume
            PostFXManager.instance.SketchVolume.gameObject.SetActive(false);

            // Set IsInSketch to false
            isInSketchField.SetValue(__instance, false);

            // Skip the original method and coroutine
            return false;
        }
    }

    // Patch to remove the scene loading delay
    [HarmonyPatch(typeof(MapManager), "LoadSceneDelayed")]
    public static class MapManager_LoadSceneDelayed_Patch
    {
        public static bool Prefix(MapManager __instance, string scene, float delay)
        {
            // Load the scene immediately without delay
            PhotonNetwork.LoadLevel(scene);

            // Skip the original coroutine
            return false;
        }
    }

    // Patch to skip vignette info display fade animations
    [HarmonyPatch(typeof(VignetteInfoDisplay), "FadeOut")]
    public static class VignetteInfoDisplay_FadeOut_Patch
    {
        public static bool Prefix(VignetteInfoDisplay __instance)
        {
            // Get the Fader field
            CanvasGroup fader = __instance.Fader;
            if (fader != null)
                // Immediately hide it
                fader.alpha = 0f;

            // Skip the coroutine
            return false;
        }
    }

    // Patch to skip the UI transition helper delays
    [HarmonyPatch(typeof(UITransitionHelper), "Update")]
    public static class UITransitionHelper_Update_Patch
    {
        public static void Postfix(UITransitionHelper __instance)
        {
            // Force immediate transitions
            FieldInfo canvasGroupField = AccessTools.Field(typeof(UITransitionHelper), "canvasGroup");
            CanvasGroup canvasGroup = canvasGroupField.GetValue(__instance) as CanvasGroup;

            FieldInfo wantVisibleTimeField = AccessTools.Field(typeof(UITransitionHelper), "wantVisibleTime");
            float wantVisibleTime = (float)wantVisibleTimeField.GetValue(__instance);

            if (canvasGroup != null)
                // Set alpha immediately based on visibility
                canvasGroup.alpha = wantVisibleTime > 0 ? 1f : 0f;
        }
    }
}