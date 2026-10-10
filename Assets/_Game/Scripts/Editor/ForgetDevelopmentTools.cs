using System.Collections.Generic;
using System.Text;
using LightRemembers.Memory;
using UnityEditor;
using UnityEngine;

namespace LightRemembers.Editor
{
    /// <summary>Editor-only controls for testing the Forget prototype without production bindings.</summary>
    public static class ForgetDevelopmentTools
    {
        [MenuItem("Light Remembers/Forget/Unlock Forget (Development)")]
        private static void UnlockForget() => MemoryAbilityState.SetForgetUnlocked(true, true);

        [MenuItem("Light Remembers/Forget/Lock Forget (Development)")]
        private static void LockForget() => MemoryAbilityState.SetForgetUnlocked(false, true);

        [MenuItem("Light Remembers/Forget/Force Forget Selected")]
        private static void ForceForgetSelected()
        {
            var target = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<MemoryForgettable>()
                : null;
            if (target == null)
            {
                Debug.LogWarning("Select an explicitly authored MemoryForgettable object first.");
                return;
            }
            target.SetRevealed(true);
            if (!target.TryForget())
                Debug.LogWarning(target.LastFailureFeedback, target);
        }

        [MenuItem("Light Remembers/Forget/Restore All Forgotten Objects")]
        private static void RestoreAllForgottenObjects()
        {
            foreach (var target in FindForgettables())
                if (target != null && target.State != ForgetState.Present)
                    target.RestorePresentImmediately();
        }

        [MenuItem("Light Remembers/Forget/Test Perception Forget on Selected Hollow")]
        private static void TestPerceptionForget()
        {
            var hollow = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<HollowController>()
                : null;
            if (hollow == null)
            {
                Debug.LogWarning("Select The Hollow first.");
                return;
            }
            MemoryAbilityState.SetForgetUnlocked(true);
            hollow.Wake();
            hollow.SetRevealed(true);
            hollow.TickLightExposure(true, 1.3f);
            if (!hollow.TryForget())
                Debug.LogWarning(hollow.LastFailureFeedback, hollow);
        }

        [MenuItem("Light Remembers/Forget/Inspect Active Forget Target")]
        private static void InspectActiveTarget()
        {
            var target = MemoryForgetCapacity.ActiveTarget;
            Debug.Log(target == null
                ? "No physical MemoryForgettable object currently occupies Forget capacity."
                : $"Forget target: {target.name} | category={target.Category} | state={target.State}", target);
        }

        [MenuItem("Light Remembers/Forget/Reset ForgetPrototype")]
        private static void ResetForgetPrototype()
        {
            foreach (var target in FindForgettables())
                target.RestorePresentImmediately();
            MemoryAbilityState.SetForgetUnlocked(false, true);
            foreach (var hollow in Object.FindObjectsByType<HollowController>(FindObjectsInactive.Include))
                if (hollow != null)
                    hollow.SetRevealed(false);
        }

        [MenuItem("Light Remembers/Forget/Validate Open Scene Forget Authoring")]
        private static void ValidateOpenScenes()
        {
            var errors = new List<string>();
            var forgettables = FindForgettables();
            foreach (var target in forgettables)
            {
                if (target.RendererTargets == null || target.RendererTargets.Length == 0)
                    errors.Add($"{target.name}: no renderer targets configured.");
                if (target.Category != ForgetCategory.EnvironmentalProp &&
                    (target.ColliderTargets == null || target.ColliderTargets.Length == 0))
                    errors.Add($"{target.name}: physical Forget category has no collider targets.");
                var composite = target.GetComponent<MemoryComposite>();
                if (composite != null && composite.Forget == target && composite.Recall != null &&
                    target.Recallable != composite.Recall)
                    errors.Add($"{target.name}: Recall/Forget composite is not wired to the same memory entity.");
            }

            foreach (var hollow in Object.FindObjectsByType<HollowController>(FindObjectsInactive.Include))
                if (!hollow.HasPerceptionForgetConfiguration)
                    errors.Add($"{hollow.name}: perception Forget requires player, Memory Light, and body references.");

            if (errors.Count == 0)
                Debug.Log($"Forget validation passed for {forgettables.Count} explicitly-authored target(s).");
            else
            {
                var report = new StringBuilder("Forget authoring validation found issues:");
                foreach (var error in errors)
                    report.AppendLine().Append("- ").Append(error);
                Debug.LogError(report.ToString());
            }
        }

        private static List<MemoryForgettable> FindForgettables()
        {
            return new List<MemoryForgettable>(Object.FindObjectsByType<MemoryForgettable>(FindObjectsInactive.Include));
        }
    }
}
