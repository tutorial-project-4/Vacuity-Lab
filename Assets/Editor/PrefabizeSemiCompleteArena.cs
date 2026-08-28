#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrefabizeSemiCompleteArena
{
    private const string ScenePath = "Assets/Scenes/semi-complete-arena.unity";

    [MenuItem("Tools/Codex/Prefabize Semi Complete Arena")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);

        SaveAndConnect("Dialogue Canvas", "Assets/Prefabs/UI/Dialogue Canvas.prefab");
        SaveAndConnect("Game Over Panel", "Assets/Prefabs/UI/Game Over Panel.prefab");
        SaveAndConnect("Player Return Camera Focus", "Assets/Prefabs/Camera/Player Return Camera Focus.prefab");
        SaveAndConnect("Boss1 Retry Camera Profile", "Assets/Prefabs/Camera/Boss1 Retry Camera Profile.prefab");
        SaveAndConnect("Boss1 Clear Trigger", "Assets/Prefabs/Arena/Boss1 Clear Trigger.prefab");
        SaveAndConnect("Boss1 Death Reward Sequence", "Assets/Prefabs/Arena/Boss1 Death Reward Sequence.prefab");

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Codex] Prefabize semi-complete-arena finished. BossBeam was intentionally skipped.");
    }

    private static void SaveAndConnect(string objectName, string prefabPath)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            Debug.LogWarning($"[Codex] Prefab target not found: {objectName}");
            return;
        }

        string directory = Path.GetDirectoryName(prefabPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(target, prefabPath, InteractionMode.AutomatedAction);
        Debug.Log($"[Codex] Prefabized {objectName} -> {prefabPath}");
    }
}
#endif
