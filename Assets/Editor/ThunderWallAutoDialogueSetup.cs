#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ThunderWallAutoDialogueSetup
{
    private const string ScenePath = "Assets/Scenes/semi-complete-arena.unity";
    private const string ThunderWallName = "Thunder Wall";
    private const string TriggerName = "Thunder Wall Dialogue Trigger";

    [MenuItem("Tools/Codex/Setup Thunder Wall Auto Dialogue")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);

        GameObject thunderWall = GameObject.Find(ThunderWallName);
        if (thunderWall == null)
        {
            Debug.LogWarning($"[Codex] {ThunderWallName} object not found.");
            return;
        }

        Transform triggerTransform = thunderWall.transform.Find(TriggerName);
        GameObject triggerObject;
        if (triggerTransform == null)
        {
            triggerObject = new GameObject(TriggerName);
            triggerObject.transform.SetParent(thunderWall.transform, false);
            triggerObject.transform.localPosition = Vector3.zero;
        }
        else
        {
            triggerObject = triggerTransform.gameObject;
        }

        triggerObject.layer = 0;

        BoxCollider2D triggerCollider = triggerObject.GetComponent<BoxCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = triggerObject.AddComponent<BoxCollider2D>();
        }

        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(6f, 6f);
        triggerCollider.offset = Vector2.zero;

        AutoDialogueTrigger autoDialogue = triggerObject.GetComponent<AutoDialogueTrigger>();
        if (autoDialogue == null)
        {
            autoDialogue = triggerObject.AddComponent<AutoDialogueTrigger>();
        }

        DialogueRunner runner = Object.FindFirstObjectByType<DialogueRunner>(FindObjectsInactive.Include);
        SerializedObject autoDialogueObject = new SerializedObject(autoDialogue);
        autoDialogueObject.FindProperty("dialogueRunner").objectReferenceValue = runner;
        autoDialogueObject.FindProperty("triggerOnce").boolValue = true;

        SerializedProperty lines = autoDialogueObject.FindProperty("lines");
        lines.arraySize = 1;
        SerializedProperty line = lines.GetArrayElementAtIndex(0);
        line.FindPropertyRelative("speaker").stringValue = "나레이션";
        line.FindPropertyRelative("text").stringValue = "강한 전류가 흐르고 있다. 가까이 가면 위험할 것 같다.";
        line.FindPropertyRelative("image").objectReferenceValue = null;
        line.FindPropertyRelative("hideDialogueWhileImage").boolValue = false;

        autoDialogueObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(triggerObject);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Codex] Thunder Wall auto dialogue trigger setup complete.");
    }
}
#endif
