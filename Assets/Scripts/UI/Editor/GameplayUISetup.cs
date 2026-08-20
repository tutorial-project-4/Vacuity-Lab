using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayUISetup
{
    const string ScenePath = "Assets/Scenes/boss-semi-complete-arena.unity";

    [MenuItem("Tools/UI/Setup Gameplay UI")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var old = GameObject.Find("Gameplay UI");
        if (old) Object.DestroyImmediate(old);
        new GameObject("Gameplay UI", typeof(RectTransform), typeof(GameplayUI));
        StyleDialogue();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GameplayUISetup] Gameplay UI와 대화 레이아웃 설정 완료");
    }

    static void StyleDialogue()
    {
        var panel = GameObject.Find("Dialogue Panel");
        if (!panel) return;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(.08f, 0);
        rt.anchorMax = new Vector2(.92f, 0);
        rt.pivot = new Vector2(.5f, 0);
        rt.anchoredPosition = new Vector2(0, 42);
        rt.sizeDelta = new Vector2(0, 230);
        var image = panel.GetComponent<Image>();
        if (image) image.color = new Color(.02f, .035f, .055f, .9f);

        foreach (var text in panel.GetComponentsInChildren<Text>(true))
        {
            text.color = new Color(.9f, .95f, .98f);
            text.fontSize = text.name.Contains("Speaker") ? 28 : 24;
        }
    }
}
