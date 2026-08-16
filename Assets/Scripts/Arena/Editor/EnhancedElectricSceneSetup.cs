using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// #12 강화 전기: Boss_test_Scene에 EnhancedElectric 루트(스케줄러 + 세로 라인 5개)를 생성한다.
/// 라인은 바닥 5구역과 같은 X 분할의 세로 기둥(바닥 위 전 높이) — 스케줄러·존 상태기계는
/// 1페이즈 전기(ElectricFloorScheduler/HazardBase)를 그대로 재사용하고 수치만 다르다
/// (예고 1s → 활성 2.5s → 대기 2s). Begin()은 Boss.cs가 페이즈 2 시작 시 호출한다.
/// 메뉴: Tools/Boss/Setup Enhanced Electric. 재실행하면 기존 루트를 지우고 다시 만든다(멱등).
public static class EnhancedElectricSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Boss_test_Scene.unity";
    private const string SpritePath = "Assets/Art/WhiteSquare.png";
    private const float FloorTopY = -4.0232f;     // ElectricFloorSceneSetup과 동일 실측
    private const float InnerHalfWidth = 8.9309f;
    private const float LineHeight = 12f;         // 잠정: 가시벽과 동일 — 아레나 확정 시 조정
    private const float VisualGap = 0.08f;

    [MenuItem("Tools/Boss/Setup Enhanced Electric (Boss_test_Scene)")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject old = GameObject.Find("EnhancedElectric");
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        int hazardLayer = Mathf.Max(0, LayerMask.NameToLayer("Hazard"));

        GameObject root = new GameObject("EnhancedElectric");
        var scheduler = root.AddComponent<ElectricFloorScheduler>();

        float lineWidth = InnerHalfWidth * 2f / 5f;
        var lines = new HazardBase[5];
        for (int i = 0; i < lines.Length; i++)
        {
            float centerX = -InnerHalfWidth + lineWidth * (i + 0.5f);
            lines[i] = CreateLine($"Line_{i + 1}", root.transform,
                new Vector2(centerX, FloorTopY + LineHeight * 0.5f),
                new Vector2(lineWidth - VisualGap, LineHeight), sprite, hazardLayer);
        }

        var so = new SerializedObject(scheduler);
        SerializedProperty arr = so.FindProperty("zones");
        arr.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            arr.GetArrayElementAtIndex(i).objectReferenceValue = lines[i];
        }
        so.FindProperty("restDuration").floatValue = 2f;
        so.FindProperty("maxActiveZones").intValue = 2;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 페이즈 2 시작 시 Boss가 Begin()을 호출하도록 연결 (프리팹 인스턴스 오버라이드)
        Boss boss = Object.FindAnyObjectByType<Boss>(FindObjectsInactive.Include);
        if (boss != null)
        {
            var bossSo = new SerializedObject(boss);
            bossSo.FindProperty("enhancedElectric").objectReferenceValue = scheduler;
            bossSo.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[EnhancedElectricSceneSetup] 씬에 Boss 없음 — enhancedElectric 수동 연결 필요");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[EnhancedElectricSceneSetup] 강화 전기 세로 라인 5개 생성 + Boss.enhancedElectric 연결 완료");
    }

    private static HazardBase CreateLine(string name, Transform parent, Vector2 center, Vector2 size, Sprite sprite, int layer)
    {
        GameObject go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;

        var descriptor = go.AddComponent<TerrainDescriptor>();
        descriptor.terrainKind = TerrainKind.EnhancedElectricFloor;
        descriptor.targetMask = ActorTarget.Player;

        GameObject damage = new GameObject("DamageTrigger") { layer = layer };
        damage.transform.SetParent(go.transform, false);
        var col = damage.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        damage.AddComponent<PlayerDamageSource>(); // Damage 기본 1하트
        damage.SetActive(false);

        var line = go.AddComponent<HazardBase>();
        line.SetupReferences(damage, sr);

        // 예고 1s(확정) / 활성 2.5s(잠정, 범위 2~3s) / 쿨다운 0 — 배치 간 대기는 스케줄러 소유.
        // 세로 기둥이 화면을 덮으므로 대기 상태는 투명, 예고·활성은 반투명으로.
        var lineSo = new SerializedObject(line);
        lineSo.FindProperty("warningDuration").floatValue = 1f;
        lineSo.FindProperty("activeDuration").floatValue = 2.5f;
        lineSo.FindProperty("cooldownDuration").floatValue = 0f;
        lineSo.FindProperty("inactiveColor").colorValue = new Color(1f, 1f, 1f, 0f);
        lineSo.FindProperty("cooldownColor").colorValue = new Color(1f, 1f, 1f, 0f);
        lineSo.FindProperty("warningColor").colorValue = new Color(1f, 0.85f, 0.2f, 0.3f);
        lineSo.FindProperty("activeColor").colorValue = new Color(1f, 0.25f, 0.2f, 0.45f);
        lineSo.ApplyModifiedPropertiesWithoutUndo();

        return line;
    }
}
