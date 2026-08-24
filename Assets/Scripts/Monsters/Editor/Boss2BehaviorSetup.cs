using System;
using System.Linq;
using System.Reflection;
using Unity.Behavior;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Boss2BehaviorSetup
{
    const string GraphPath = "Assets/Behavior/Boss2Brain.asset";
    const string PrefabPath = "Assets/Prefabs/Monster/Boss/Boss-2.prefab";
    const string ScenePath = "Assets/Scenes/boss-semi-complete-arena.unity";

    [MenuItem("Tools/Boss 2/Build and Verify")]
    public static void BuildAndVerify()
    {
        BehaviorGraph graph = BuildGraph();
        ConfigurePrefab(graph);
        VerifyScene();
        AssetDatabase.SaveAssets();
        Debug.Log("BOSS2_SETUP_PASS");
    }

    static BehaviorGraph BuildGraph()
    {
        AssetDatabase.DeleteAsset(GraphPath);
        Assembly authoring = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.Behavior.Authoring");
        Type graphType = authoring.GetType("Unity.Behavior.BehaviorAuthoringGraph", true);
        ScriptableObject asset = ScriptableObject.CreateInstance(graphType);
        asset.name = "Boss2Brain";
        AssetDatabase.CreateAsset(asset, GraphPath);

        Type registry = authoring.GetType("Unity.Behavior.NodeRegistry", true);
        MethodInfo getInfo = registry.GetMethod("GetInfo", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Type) }, null);
        MethodInfo createNode = graphType.BaseType.GetMethod("CreateNode", BindingFlags.Instance | BindingFlags.Public);
        MethodInfo addToSequence = graphType.BaseType.GetMethod("AddNodeToSequence", BindingFlags.Instance | BindingFlags.Public);

        object start = Create(typeof(Unity.Behavior.Action).Assembly.GetType("Unity.Behavior.Start", true), Vector2.zero, null);
        object output = GetEnumerable(start, "OutputPortModels").Cast<object>().First();
        Type sequenceType = AppDomain.CurrentDomain.GetAssemblies()
            .Single(a => a.GetName().Name == "Unity.Behavior.GraphFramework")
            .GetType("Unity.Behavior.GraphFramework.SequenceNodeModel", true);
        object sequence = createNode.Invoke(asset, new object[] { sequenceType, new Vector2(0, 100), output, null });
        object spread = Create(typeof(Boss2SpreadShotAction), new Vector2(-100, 200), null);
        object aimed = Create(typeof(Boss2AimedShotAction), new Vector2(100, 200), null);
        addToSequence.Invoke(asset, new[] { spread, sequence, 0 });
        addToSequence.Invoke(asset, new[] { aimed, sequence, 1 });

        graphType.BaseType.GetMethod("SetAssetDirty", BindingFlags.Instance | BindingFlags.Public).Invoke(asset, new object[] { true });
        BehaviorGraph runtime = (BehaviorGraph)graphType.GetMethod("BuildRuntimeGraph", BindingFlags.Instance | BindingFlags.Public).Invoke(asset, new object[] { true });
        EditorUtility.SetDirty(asset);
        return runtime;

        object Create(Type runtimeType, Vector2 position, object port)
        {
            object info = getInfo.Invoke(null, new object[] { runtimeType });
            if (info == null) throw new InvalidOperationException($"Behavior node is not registered: {runtimeType.Name}");
            object serializableType = info.GetType().GetField("ModelType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info);
            Type modelType = (Type)serializableType.GetType()
                .GetProperty("Type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(serializableType);
            return createNode.Invoke(asset, new object[] { modelType, position, port, new[] { info } });
        }
    }

    static System.Collections.IEnumerable GetEnumerable(object target, string property) =>
        (System.Collections.IEnumerable)target.GetType().GetProperty(property).GetValue(target);

    static void ConfigurePrefab(BehaviorGraph graph)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            BehaviorGraphAgent agent = root.GetComponent<BehaviorGraphAgent>() ?? root.AddComponent<BehaviorGraphAgent>();
            agent.Graph = graph;
            if (root.GetComponent<Boss>() != null || root.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "BossArenaController"))
                throw new InvalidOperationException("Boss 1 component found on Boss-2 prefab");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void VerifyScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        Boss2IntroTrigger trigger = roots.SelectMany(r => r.GetComponentsInChildren<Boss2IntroTrigger>(true)).Single();
        Boss2Controller boss2 = roots.SelectMany(r => r.GetComponentsInChildren<Boss2Controller>(true)).Single();
        if (boss2.gameObject.activeSelf) throw new InvalidOperationException("Boss-2 must start inactive");
        if (roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                 .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)) != 0)
            throw new InvalidOperationException("Scene contains Missing Script");

        SerializedObject serialized = new(trigger);
        if (serialized.FindProperty("boss2").objectReferenceValue != boss2.gameObject)
            throw new InvalidOperationException("Boss2IntroTrigger target mismatch");
    }
}
