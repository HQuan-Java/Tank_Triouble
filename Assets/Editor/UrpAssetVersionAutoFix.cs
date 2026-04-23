using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class UrpAssetVersionAutoFix : IPreprocessBuildWithReport
{
    private static readonly List<string> UrpAssetPaths = new()
    {
        "Assets/Settings/UniversalRP.asset",
        "Assets/UniversalRenderPipelineGlobalSettings.asset"
    };

    public int callbackOrder => int.MinValue;

    public void OnPreprocessBuild(BuildReport report)
    {
        EnsureUrpAssetsAreReserialized();
    }

    [MenuItem("Tools/Build/Fix URP Asset Version")]
    private static void FixFromMenu()
    {
        EnsureUrpAssetsAreReserialized();
        EditorUtility.DisplayDialog(
            "URP Asset Fix",
            "URP assets have been reserialized. Try building again.",
            "OK");
    }

    private static void EnsureUrpAssetsAreReserialized()
    {
        UpgradeUniversalRenderPipelineAsset();
        UpgradeUniversalRenderPipelineGlobalSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void UpgradeUniversalRenderPipelineAsset()
    {
        var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPaths[0]);
        if (urpAsset == null)
        {
            return;
        }

        // Force migration logic that runs on deserialize for old serialized versions.
        urpAsset.OnAfterDeserialize();

        var upgradeMethod = typeof(UniversalRenderPipelineAsset).GetMethod(
            "UpgradeAsset",
            BindingFlags.Static | BindingFlags.NonPublic);
        upgradeMethod?.Invoke(null, new object[] { urpAsset.GetInstanceID() });

        EditorUtility.SetDirty(urpAsset);
        AssetDatabase.SaveAssetIfDirty(urpAsset);
    }

    private static void UpgradeUniversalRenderPipelineGlobalSettings()
    {
        var urpGlobalSettingsType = Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings, Unity.RenderPipelines.Universal.Runtime");
        if (urpGlobalSettingsType == null)
        {
            return;
        }

        var globalSettingsAsset = AssetDatabase.LoadMainAssetAtPath(UrpAssetPaths[1]);
        if (globalSettingsAsset == null || !urpGlobalSettingsType.IsInstanceOfType(globalSettingsAsset))
        {
            return;
        }

        var upgradeMethod = urpGlobalSettingsType.GetMethod(
            "UpgradeAsset",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        upgradeMethod?.Invoke(null, new object[] { globalSettingsAsset.GetInstanceID() });

        EditorUtility.SetDirty(globalSettingsAsset);
        AssetDatabase.SaveAssetIfDirty(globalSettingsAsset);
    }
}
