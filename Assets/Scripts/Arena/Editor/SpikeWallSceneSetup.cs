using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// #10 가시벽: Boss_test_Scene 좌우 내벽 면에 가시벽 트리거(기본 비활성)를 생성하고
/// Boss.spikeWalls에 연결한다. 넉백은 PlayerController.ReceiveHit가 피해 소스 반대 방향
/// (= 안쪽)으로 처리하므로 별도 코드가 없다.
/// 메뉴: Tools/Boss/Setup Spike Walls. 재실행하면 기존 루트를 지우고 다시 만든다(멱등).
public static class SpikeWallSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Boss_test_Scene.unity";
    private const string SpritePath = "Assets/Art/WhiteSquare.png";
    private const float InnerX = 8.9309f;    // 내벽 면(±) — ElectricFloorSceneSetup과 동일 실측
    private const float FloorTopY = -4.0232f;
    private const float Height = 12f;        // 잠정: 바닥 위 전 높이 — 아레나 확정 시 조정
    private const float Thickness = 0.35f;

    [MenuItem("Tools/Boss/Setup Spike Walls (Boss_test_Scene)")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject old = GameObject.Find("SpikeWalls");
        if (old == null)
        {
            // 비활성 루트는 Find로 안 잡힘 — Boss가 들고 있던 참조로 재탐색
            Boss existing = Object.FindAnyObjectByType<Boss>(FindObjectsInactive.Include);
            if (existing != null)
            {
                var prevSo = new SerializedObject(existing);
                old = prevSo.FindProperty("spikeWalls").objectReferenceValue as GameObject;
            }
        }
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        int hazardLayer = Mathf.Max(0, LayerMask.NameToLayer("Hazard"));

        GameObject root = new GameObject("SpikeWalls");
        CreateWall("SpikeWall_L", root.transform, -InnerX + Thickness * 0.5f, sprite, hazardLayer);
        CreateWall("SpikeWall_R", root.transform, InnerX - Thickness * 0.5f, sprite, hazardLayer);
        root.SetActive(false); // #10 전환 컷신에서 Boss가 활성화

        Boss boss = Object.FindAnyObjectByType<Boss>(FindObjectsInactive.Include);
        if (boss != null)
        {
            var so = new SerializedObject(boss);
            so.FindProperty("spikeWalls").objectReferenceValue = root;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[SpikeWallSceneSetup] 씬에 Boss 없음 — spikeWalls 수동 연결 필요");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SpikeWallSceneSetup] 가시벽 좌우 생성(비활성) + Boss.spikeWalls 연결 완료");
    }

    private static void CreateWall(string name, Transform parent, float centerX, Sprite sprite, int layer)
    {
        GameObject go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(centerX, FloorTopY + Height * 0.5f, 0f);
        go.transform.localScale = new Vector3(Thickness, Height, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.9f, 0.25f, 0.25f);
        sr.sortingOrder = 2;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        go.AddComponent<PlayerDamageSource>(); // Damage 기본 1하트
    }
}
