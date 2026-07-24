using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// terrain_system_handover.md 10절 구조대로 샘플 지형 씬을 생성한다.
/// 메뉴: Tools/Terrain/Build Sample Scene → Assets/Scenes/TerrainSampleScene.unity 저장(기존 파일 덮어씀).
/// 아트 에셋 없이 단색 스프라이트로 배치하며, 지형 의미는 전부 TerrainDescriptor가 담는다.
/// </summary>
public static class TerrainSampleSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TerrainSampleScene.unity";
    private const string SpritePath = "Assets/Art/WhiteSquare.png";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const string DashWallLayerName = "DashPassableWall";

    private const string SpikeChannel = "LAB_SPIKE_A";
    private const string EnhancedChannel = "LAB_ELECTRIC_ENHANCED_A";

    // 지면 최고점 y=1. 플레이어 점프 최고 상승량이 약 1.17유닛(jumpSpeed 6.5, gravity 18)이라
    // 넘어야 하는 단차는 전부 0.9 이하로 설계했다.
    private const float FloorTopY = 1f;

    private static Sprite whiteSprite;

    [MenuItem("Tools/Terrain/Build Sample Scene")]
    public static void Build()
    {
        int solidLayer = LayerMask.NameToLayer("Solid");
        int hazardLayer = LayerMask.NameToLayer("Hazard");
        int dashWallLayer = EnsureLayer(DashWallLayerName);
        whiteSprite = EnsureWhiteSprite();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject arenaRoot = new GameObject("ArenaRoot");

        // ---- Background (충돌 없음, 문서 7.1) ----
        Transform background = Group(arenaRoot.transform, "Background");
        GameObject bgPanel = Box("BackgroundPanel", background, new Vector2(28f, 6.5f), new Vector2(56f, 11f),
            new Color(0.09f, 0.1f, 0.14f), 0, TerrainKind.Background, withCollider: false);
        bgPanel.GetComponent<SpriteRenderer>().sortingOrder = -10;

        // ---- Geometry ----
        Transform geometry = Group(arenaRoot.transform, "Geometry");
        Color boundaryColor = new Color(0.12f, 0.12f, 0.15f);
        Color solidColor = new Color(0.5f, 0.5f, 0.55f);
        Color floorColor = new Color(0.35f, 0.4f, 0.4f);
        Color platformColor = new Color(0.45f, 0.55f, 0.7f);
        Color dashWallColor = new Color(0.2f, 0.9f, 1f);

        // 절대 경계(문서 7.5): 바닥·천장·좌우. 내부 플레이 영역 x 0~56, y 1~12.
        Box("Boundary_Bottom", geometry, new Vector2(28f, -0.5f), new Vector2(58f, 1f), boundaryColor, solidLayer, TerrainKind.BoundaryWall);
        Box("Boundary_Top", geometry, new Vector2(28f, 12.5f), new Vector2(58f, 1f), boundaryColor, solidLayer, TerrainKind.BoundaryWall);
        Box("Boundary_Left", geometry, new Vector2(-0.5f, 6f), new Vector2(1f, 14f), boundaryColor, solidLayer, TerrainKind.BoundaryWall);
        Box("Boundary_Right", geometry, new Vector2(56.5f, 6f), new Vector2(1f, 14f), boundaryColor, solidLayer, TerrainKind.BoundaryWall);

        // 바닥(문서 7.2): 경계와 별도 타입
        Box("Floor", geometry, new Vector2(28f, 0.5f), new Vector2(56f, 1f), floorColor, solidLayer, TerrainKind.Floor);

        // 일반 벽(문서 7.3) + 넘어가기용 계단
        Box("SolidWall", geometry, new Vector2(12f, 1.75f), new Vector2(1f, 1.5f), solidColor, solidLayer, TerrainKind.SolidWall);
        Box("SolidWall_Step", geometry, new Vector2(10.8f, 1.25f), new Vector2(0.8f, 0.5f), floorColor, solidLayer, TerrainKind.Floor);

        // 벽뚫대시 통과 벽(문서 7.4): 전용 Layer + Descriptor. 대시 미구현 동안은 계단으로 넘는다.
        Transform dashWalls = Group(geometry, "DashPassableWalls");
        Box("DashPassableWall_A", dashWalls, new Vector2(17f, 1.75f), new Vector2(1f, 1.5f), dashWallColor, dashWallLayer, TerrainKind.DashPassableWall);
        Box("DashPassableWall_Step", dashWalls, new Vector2(15.8f, 1.25f), new Vector2(0.8f, 0.5f), floorColor, solidLayer, TerrainKind.Floor);

        // 발판(문서 7.7): 플레이어 정책 TBD → 임시 Solid, Inspector에서 교체
        Box("Platform_Low", geometry, new Vector2(22f, 1.7f), new Vector2(3f, 0.4f), platformColor, solidLayer, TerrainKind.Platform);
        Box("Platform_High", geometry, new Vector2(25.5f, 2.6f), new Vector2(3f, 0.4f), platformColor, solidLayer, TerrainKind.Platform);

        // ---- Hazards (문서 7.8~7.10) ----
        Transform hazards = Group(arenaRoot.transform, "Hazards");
        ChannelTriggeredHazard spike = CreateHazard<ChannelTriggeredHazard>(
            "Spike_A", Group(hazards, "Spikes"), new Vector2(31f, 1.25f), new Vector2(4f, 0.5f), TerrainKind.Spike, hazardLayer);
        spike.SetChannel(SpikeChannel);

        CreateHazard<AutoCycleHazard>(
            "ElectricFloor_A", Group(hazards, "ElectricFloors"), new Vector2(39f, 1.25f), new Vector2(6f, 0.5f), TerrainKind.ElectricFloor, hazardLayer);

        ChannelTriggeredHazard enhanced = CreateHazard<ChannelTriggeredHazard>(
            "EnhancedElectricFloor_A", Group(hazards, "EnhancedElectricFloors"), new Vector2(47f, 1.25f), new Vector2(4f, 0.5f), TerrainKind.EnhancedElectricFloor, hazardLayer);
        enhanced.SetChannel(EnhancedChannel);

        // ---- SpawnPoints ----
        Transform spawns = Group(arenaRoot.transform, "SpawnPoints");
        Transform playerSpawn = Group(spawns, "PlayerSpawn");
        playerSpawn.position = new Vector3(3f, 2.2f, 0f);
        Transform bossSpawn = Group(spawns, "BossSpawn");
        bossSpawn.position = new Vector3(25.5f, 4f, 0f);

        // ---- Debug ----
        Transform debug = Group(arenaRoot.transform, "Debug");
        GameObject panel = new GameObject("HazardTriggerPanel");
        panel.transform.SetParent(debug);
        panel.AddComponent<HazardDebugTrigger>();

        // ---- 구역 라벨 (문서 13 순회 구성) ----
        Transform labels = Group(debug, "Labels");
        Label(labels, new Vector2(3f, 5.5f), "1. Spawn / Floor / Background");
        Label(labels, new Vector2(12f, 5.5f), "2. SolidWall (climb steps)");
        Label(labels, new Vector2(17f, 4.5f), "3. DashPassableWall (dash TBD)");
        Label(labels, new Vector2(24f, 5.5f), "4. Platform (player policy TBD)");
        Label(labels, new Vector2(31f, 4.5f), "5. Spike (manual trigger)");
        Label(labels, new Vector2(39f, 5.5f), "6. ElectricFloor (auto)");
        Label(labels, new Vector2(47f, 4.5f), "7. EnhancedElectric (channel)");
        Label(labels, new Vector2(54f, 5.5f), "8. Boundary");

        // ---- Player ----
        GameObject player = SpawnPlayer(playerSpawn.position, solidLayer, dashWallLayer);

        // ---- Camera / Light ----
        GameObject camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
        camGo.AddComponent<UniversalAdditionalCameraData>();
        camGo.AddComponent<AudioListener>();
        SimpleCameraFollow follow = camGo.AddComponent<SimpleCameraFollow>();
        if (player != null)
        {
            follow.SetTarget(player.transform);
            camGo.transform.position = player.transform.position + new Vector3(0f, 2f, -10f);
        }

        GameObject lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[TerrainSampleSceneBuilder] 씬 생성 완료: {ScenePath} (DashPassableWall Layer = {dashWallLayer})");
    }

    private const string PrefabFolder = "Assets/Prefabs/Terrain";

    /// 디자이너 배치용 프리팹 4종을 생성한다. 사용법은 terrain_placement_guide.md 참고.
    [MenuItem("Tools/Terrain/Create Terrain Prefabs")]
    public static void CreatePrefabs()
    {
        int hazardLayer = LayerMask.NameToLayer("Hazard");
        int dashWallLayer = EnsureLayer(DashWallLayerName);
        whiteSprite = EnsureWhiteSprite();

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();
        }

        GameObject dashWall = Box("DashPassableWall", null, Vector2.zero, new Vector2(1f, 1.5f),
            new Color(0.2f, 0.9f, 1f), dashWallLayer, TerrainKind.DashPassableWall);
        SavePrefab(dashWall);

        ChannelTriggeredHazard spike = CreateHazard<ChannelTriggeredHazard>(
            "Spike", null, Vector2.zero, new Vector2(4f, 0.5f), TerrainKind.Spike, hazardLayer);
        spike.SetChannel(SpikeChannel);
        SavePrefab(spike.gameObject);

        AutoCycleHazard electric = CreateHazard<AutoCycleHazard>(
            "ElectricFloor", null, Vector2.zero, new Vector2(6f, 0.5f), TerrainKind.ElectricFloor, hazardLayer);
        SavePrefab(electric.gameObject);

        ChannelTriggeredHazard enhanced = CreateHazard<ChannelTriggeredHazard>(
            "EnhancedElectricFloor", null, Vector2.zero, new Vector2(4f, 0.5f), TerrainKind.EnhancedElectricFloor, hazardLayer);
        enhanced.SetChannel(EnhancedChannel);
        SavePrefab(enhanced.gameObject);

        AssetDatabase.SaveAssets();
        Debug.Log($"[TerrainSampleSceneBuilder] 프리팹 4종 생성 완료: {PrefabFolder}");
    }

    private static void SavePrefab(GameObject go)
    {
        string path = $"{PrefabFolder}/{go.name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ---------- helpers ----------

    private static Transform Group(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.transform;
    }

    private static GameObject Box(string name, Transform parent, Vector2 center, Vector2 size,
        Color color, int layer, TerrainKind kind, bool withCollider = true)
    {
        GameObject go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = color;

        if (withCollider)
        {
            go.AddComponent<BoxCollider2D>();
        }

        go.AddComponent<TerrainDescriptor>().terrainKind = kind;
        return go;
    }

    private static T CreateHazard<T>(string name, Transform parent, Vector2 center, Vector2 size,
        TerrainKind kind, int hazardLayer) where T : HazardBase
    {
        GameObject root = new GameObject(name) { layer = hazardLayer };
        root.transform.SetParent(parent);
        root.transform.position = center;
        root.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.sortingOrder = 1;

        TerrainDescriptor descriptor = root.AddComponent<TerrainDescriptor>();
        descriptor.terrainKind = kind;
        descriptor.targetMask = ActorTarget.Player; // 보스 피해 여부 TBD(문서 18)

        GameObject damage = new GameObject("DamageTrigger") { layer = hazardLayer };
        damage.transform.SetParent(root.transform, false);
        BoxCollider2D col = damage.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        damage.AddComponent<PlayerDamageSource>(); // 피해량 데이터 소스(팀 규약). 값은 TBD, 기본 1하트
        damage.SetActive(false);

        T hazard = root.AddComponent<T>();
        hazard.SetupReferences(damage, sr);
        return hazard;
    }

    private static GameObject SpawnPlayer(Vector3 position, int solidLayer, int dashWallLayer)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[TerrainSampleSceneBuilder] 플레이어 프리팹을 찾지 못해 스폰을 건너뜀: {PlayerPrefabPath}");
            return null;
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = position;

        // 평상시에는 DashPassableWall도 벽으로 취급해야 하므로 충돌 마스크에 포함시킨다.
        // 벽뚫대시 구현 시 대시 중에만 이 비트를 빼면 된다 (문서 7.4).
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            SerializedObject so = new SerializedObject(movement);
            so.FindProperty("solidLayer").intValue = (1 << solidLayer) | (1 << dashWallLayer);
            so.ApplyModifiedProperties();
        }

        return player;
    }

    private static void Label(Transform parent, Vector2 position, string text)
    {
        GameObject go = new GameObject($"Label_{text}");
        go.transform.SetParent(parent);
        go.transform.position = position;

        TextMesh tm = go.AddComponent<TextMesh>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        tm.text = text;
        tm.characterSize = 0.22f;
        tm.fontSize = 32;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = new Color(0.9f, 0.9f, 0.9f);
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        go.GetComponent<MeshRenderer>().sortingOrder = 5;
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing != -1)
        {
            return existing;
        }

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 11; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        throw new System.InvalidOperationException("빈 Layer 슬롯이 없습니다. TagManager를 정리해 주세요.");
    }

    private static Sprite EnsureWhiteSprite()
    {
        if (!File.Exists(SpritePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));

            Texture2D tex = new Texture2D(16, 16);
            Color32[] pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(SpritePath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
}
