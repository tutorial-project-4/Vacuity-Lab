using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// #11 관통 빔: Boss_test_Scene에 아레나 전폭 수평 빔 오브젝트(기본 비활성)를 생성한다.
/// 높이(y)는 BeamAction이 예고 시작 시 낮음/중간/높음으로 옮기므로 초기값은 의미 없음.
/// 그래프 연결은 수동: Blackboard에 Beam(GameObject) 추가 → 에이전트 인스펙터에서 BossBeam 할당.
/// 메뉴: Tools/Boss/Setup Beam. 재실행하면 기존 오브젝트를 지우고 다시 만든다(멱등).
public static class BeamSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Boss_test_Scene.unity";
    private const string SpritePath = "Assets/Art/WhiteSquare.png";
    private const float InnerX = 8.9309f;   // 내벽 면(±) — SpikeWallSceneSetup과 동일 실측
    private const float Thickness = 1f;     // 잠정: 빔 두께

    [MenuItem("Tools/Boss/Setup Beam (Boss_test_Scene)")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 기본 비활성이라 GameObject.Find로 안 잡힘 — 루트 순회로 탐색
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "BossBeam") Object.DestroyImmediate(root);
        }

        GameObject go = new GameObject("BossBeam") { layer = Mathf.Max(0, LayerMask.NameToLayer("Hazard")) };
        go.transform.position = new Vector3(0f, -1f, 0f);
        go.transform.localScale = new Vector3(InnerX * 2f, Thickness, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        sr.sortingOrder = 2;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        go.AddComponent<PlayerDamageSource>(); // Damage 기본 1하트
        go.AddComponent<TimedDeactivate>();    // 잔상 소멸 담당
        go.SetActive(false);                   // BeamAction이 예고 시작 시 활성화

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BeamSceneSetup] BossBeam 생성(비활성) 완료 — Blackboard Beam 변수에 수동 할당 필요");
    }
}
