using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// #8 전기 바닥: Boss_test_Scene에 ElectricFloor 루트(스케줄러 + 5구역)를 생성한다.
/// 메뉴: Tools/Boss/Setup Electric Floor. 재실행하면 기존 루트를 지우고 다시 만든다(멱등).
/// 좌표는 씬 바닥(Square: 중심 y -4.5, 높이 0.9537)과 좌우 벽(±9.5, 폭 1.1382) 실측 기준 잠정값 —
/// 실제 아레나 정렬은 아레나 담당자와 좌표를 맞출 때 다시 잡는다.
public static class ElectricFloorSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Boss_test_Scene.unity";
    private const string SpritePath = "Assets/Art/WhiteSquare.png";
    private const float FloorTopY = -4.0232f;     // -4.5 + 0.9537/2
    private const float InnerHalfWidth = 8.9309f; // 9.5 - 1.1382/2
    private const float ZoneHeight = 0.5f;
    private const float VisualGap = 0.08f;        // 구역 경계가 눈에 보이도록 하는 틈

    [MenuItem("Tools/Boss/Setup Electric Floor (Boss_test_Scene)")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject old = GameObject.Find("ElectricFloor");
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        int hazardLayer = Mathf.Max(0, LayerMask.NameToLayer("Hazard"));

        GameObject root = new GameObject("ElectricFloor");
        var scheduler = root.AddComponent<ElectricFloorScheduler>();

        float zoneWidth = InnerHalfWidth * 2f / 5f;
        var zones = new HazardBase[5];
        for (int i = 0; i < zones.Length; i++)
        {
            float centerX = -InnerHalfWidth + zoneWidth * (i + 0.5f);
            zones[i] = CreateZone($"Zone_{i + 1}", root.transform,
                new Vector2(centerX, FloorTopY + ZoneHeight * 0.5f),
                new Vector2(zoneWidth - VisualGap, ZoneHeight), sprite, hazardLayer);
        }

        var so = new SerializedObject(scheduler);
        SerializedProperty arr = so.FindProperty("zones");
        arr.arraySize = zones.Length;
        for (int i = 0; i < zones.Length; i++)
        {
            arr.GetArrayElementAtIndex(i).objectReferenceValue = zones[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ElectricFloorSceneSetup] ElectricFloor 5구역 생성 완료 — 플레이에서 예고(노랑 1s)→활성(빨강 2s)→대기(2s) 사이클을 확인하세요.");
    }

    private static HazardBase CreateZone(string name, Transform parent, Vector2 center, Vector2 size, Sprite sprite, int layer)
    {
        GameObject go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;

        var descriptor = go.AddComponent<TerrainDescriptor>();
        descriptor.terrainKind = TerrainKind.ElectricFloor;
        descriptor.targetMask = ActorTarget.Player;

        GameObject damage = new GameObject("DamageTrigger") { layer = layer };
        damage.transform.SetParent(go.transform, false);
        var col = damage.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        damage.AddComponent<PlayerDamageSource>(); // Damage 기본 1하트
        damage.SetActive(false);

        var zone = go.AddComponent<HazardBase>();
        zone.SetupReferences(damage, sr);

        // 예고 1s(확정) / 활성 2s(확정) / 쿨다운 0 — 배치 간 대기는 스케줄러가 담당
        var zoneSo = new SerializedObject(zone);
        zoneSo.FindProperty("warningDuration").floatValue = 1f;
        zoneSo.FindProperty("activeDuration").floatValue = 2f;
        zoneSo.FindProperty("cooldownDuration").floatValue = 0f;
        zoneSo.ApplyModifiedPropertiesWithoutUndo();

        return zone;
    }
}
