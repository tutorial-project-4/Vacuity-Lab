using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// #10 게이지 2단: Boss_test_Scene에 보스 체력 게이지 UI(Canvas + 배경 + 2단 Fill)를 생성하고
/// BossHealthGauge의 fill(아래층)·fillTop(위층)에 연결한다. 숫자 없이 비율만 표시(기획).
/// 메뉴: Tools/Boss/Setup Boss Gauge. 재실행하면 기존 캔버스를 지우고 다시 만든다(멱등).
public static class BossGaugeSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Boss_test_Scene.unity";
    private const string SpritePath = "Assets/Art/WhiteSquare.png";

    [MenuItem("Tools/Boss/Setup Boss Gauge (Boss_test_Scene)")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject old = GameObject.Find("BossHealthCanvas");
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

        var canvasGo = new GameObject("BossHealthCanvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 배경(어두운 바) — 화면 상단 중앙
        var bgGo = new GameObject("BossGauge", typeof(RectTransform));
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRt = (RectTransform)bgGo.transform;
        bgRt.anchorMin = bgRt.anchorMax = bgRt.pivot = new Vector2(0.5f, 1f);
        bgRt.anchoredPosition = new Vector2(0f, -40f);
        bgRt.sizeDelta = new Vector2(600f, 24f);
        var bg = bgGo.AddComponent<Image>();
        bg.sprite = sprite;
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        // 아래층(HP 0~500) 먼저, 위층(HP 500~1000)을 나중에 — 위층이 위에 그려져 먼저 닳는 게 보인다
        Image fill = CreateFill("Fill", bgGo.transform, new Color(0.95f, 0.55f, 0.15f), sprite);
        Image fillTop = CreateFill("FillTop", bgGo.transform, new Color(0.85f, 0.2f, 0.2f), sprite);

        BossHealthGauge gauge = Object.FindAnyObjectByType<BossHealthGauge>(FindObjectsInactive.Include);
        if (gauge != null)
        {
            var so = new SerializedObject(gauge);
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("fillTop").objectReferenceValue = fillTop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[BossGaugeSceneSetup] 씬에 BossHealthGauge 없음 — fill/fillTop 수동 연결 필요");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BossGaugeSceneSetup] 게이지 UI 생성 + fill/fillTop 연결 완료 — 플레이에서 위층(빨강)→아래층(주황) 순으로 닳는지 확인하세요.");
    }

    private static Image CreateFill(string name, Transform parent, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(2f, 2f);
        rt.offsetMax = new Vector2(-2f, -2f);

        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        return img;
    }
}
