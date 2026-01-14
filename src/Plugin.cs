using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LookSensPatch;

public class Plugin : IPuckMod
{
    private Harmony harmony;

    public bool OnEnable()
    {
        harmony = new Harmony("com.puckmods.looksensitivitypatch");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        return true;
    }

    public bool OnDisable()
    {
        harmony?.UnpatchSelf();
        harmony = null;
        return true;
    }
}

// UpdateLookAngle adds stickValue * sensitivity without deltaTime scaling, so look speed
// scales with server tick rate. Multiply by deltaTime * 200 to normalize to the reference rate.
[HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.UpdateLookAngle))]
static class UpdateLookAnglePatch
{
    private const float ReferenceTickRate = 200f;

    private static readonly FieldInfo MinLookAngleField =
        typeof(PlayerInput).GetField("minimumLookAngle", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo MaxLookAngleField =
        typeof(PlayerInput).GetField("maximumLookAngle", BindingFlags.NonPublic | BindingFlags.Instance);

    [HarmonyPrefix]
    static bool Prefix(PlayerInput __instance, float deltaTime)
    {
        if (!__instance.LookInput.ClientValue)
            return true;

        Vector2 stickValue = MonoBehaviourSingleton<InputManager>.Instance.StickAction.ReadValue<Vector2>();
        float sensitivity = MonoBehaviourSingleton<SettingsManager>.Instance.LookSensitivity;

        Vector2 b = new Vector2(
            -stickValue.y * (sensitivity / 2f),
             stickValue.x * (sensitivity / 2f)
        ) * (deltaTime * ReferenceTickRate);

        var minAngle = (Vector2)MinLookAngleField.GetValue(__instance);
        var maxAngle = (Vector2)MaxLookAngleField.GetValue(__instance);

        __instance.LookAngleInput.ClientValue = Utils.Vector2Clamp(
            __instance.LookAngleInput.ClientValue + b,
            minAngle,
            maxAngle
        );

        return false;
    }
}
