using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EndingSequenceSceneSetup
{
    private const string ScenePath = "Assets/Scenes/semi-complete-arena.unity";

    [MenuItem("Tools/Ending/Setup Injection Ending")]
    public static void SetupInjectionEnding()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject boss2DoorTrigger = GameObject.Find("Boss2 Door Trigger");
        if (boss2DoorTrigger == null)
        {
            Debug.LogError("[EndingSequenceSceneSetup] Boss2 Door Trigger was not found.");
            return;
        }

        GameObject root = FindOrCreateRoot("EndingEventRoot");
        EndingChoiceState choiceState = GetOrAdd<EndingChoiceState>(root);
        EndingSequenceController sequenceController = GetOrAdd<EndingSequenceController>(root);
        AudioSource bgmSource = GetOrAdd<AudioSource>(root);
        bgmSource.playOnAwake = false;

        Canvas canvas = BuildEndingCanvas(root.transform);
        Transform screen = canvas.transform.Find("Ending Screen");
        CanvasGroup rootGroup = screen.GetComponent<CanvasGroup>();
        Image background = screen.GetComponent<Image>();
        Image slideImage = screen.Find("Ending Slide Image").GetComponent<Image>();
        Text speakerText = screen.Find("Ending Speaker Text").GetComponent<Text>();
        Text bodyText = screen.Find("Ending Body Text").GetComponent<Text>();

        ConfigureSequenceController(sequenceController, canvas, rootGroup, background, slideImage, speakerText, bodyText, bgmSource);

        SetEndingChoiceAction acceptChoiceAction = GetOrAddChoiceAction(boss2DoorTrigger, EndingChoice.AcceptedInjection);
        ConfigureChoiceAction(acceptChoiceAction, choiceState, EndingChoice.AcceptedInjection, true);
        SetEndingChoiceAction rejectChoiceAction = GetOrAddChoiceAction(boss2DoorTrigger, EndingChoice.RejectedInjection);
        ConfigureChoiceAction(rejectChoiceAction, choiceState, EndingChoice.RejectedInjection, false);
        PlayDialogueAudioAction acceptAudioAction = GetOrAddAudioAction(boss2DoorTrigger, "Assets/음악/Story/Syringe_Select.wav");
        ConfigureAudioAction(acceptAudioAction, "Assets/음악/Story/Syringe_Select.wav", DialogueAudioChannel.Story);
        PlayDialogueAudioAction rejectAudioAction = GetOrAddAudioAction(boss2DoorTrigger, "Assets/음악/Story/Syringe_Reject.wav");
        ConfigureAudioAction(rejectAudioAction, "Assets/음악/Story/Syringe_Reject.wav", DialogueAudioChannel.Story);

        StartBoss2IntroAction boss2IntroAction = GetOrAdd<StartBoss2IntroAction>(boss2DoorTrigger);
        ConfigureBoss2IntroAction(boss2IntroAction);

        ConfigureEnding2Sequence(canvas, rootGroup, background, slideImage, speakerText, bodyText);

        EndingDoorTrigger endingTrigger = GetOrAdd<EndingDoorTrigger>(boss2DoorTrigger);
        ConfigureEndingTrigger(endingTrigger, choiceState, sequenceController);

        AutoDialogueTrigger autoDialogue = boss2DoorTrigger.GetComponent<AutoDialogueTrigger>();
        if (autoDialogue == null)
        {
            autoDialogue = boss2DoorTrigger.AddComponent<AutoDialogueTrigger>();
        }

        ConfigureBoss2DoorDialogue(autoDialogue, acceptChoiceAction, acceptAudioAction, rejectChoiceAction, rejectAudioAction, boss2IntroAction);

        EditorUtility.SetDirty(boss2DoorTrigger);
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(sequenceController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[EndingSequenceSceneSetup] Injection ending setup complete.");
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing;
        }

        return new GameObject(name);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static SetEndingChoiceAction GetOrAddChoiceAction(GameObject target, EndingChoice choice)
    {
        SetEndingChoiceAction[] actions = target.GetComponents<SetEndingChoiceAction>();
        for (int i = 0; i < actions.Length; i++)
        {
            SerializedObject serialized = new SerializedObject(actions[i]);
            if (serialized.FindProperty("choice").enumValueIndex == (int)choice)
            {
                return actions[i];
            }
        }

        return target.AddComponent<SetEndingChoiceAction>();
    }

    private static PlayDialogueAudioAction GetOrAddAudioAction(GameObject target, string clipPath)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        PlayDialogueAudioAction[] actions = target.GetComponents<PlayDialogueAudioAction>();
        for (int i = 0; i < actions.Length; i++)
        {
            SerializedObject serialized = new SerializedObject(actions[i]);
            if (serialized.FindProperty("clip").objectReferenceValue == clip)
            {
                return actions[i];
            }
        }

        return target.AddComponent<PlayDialogueAudioAction>();
    }

    private static Canvas BuildEndingCanvas(Transform parent)
    {
        GameObject canvasObject = FindDirectChild(parent, "Ending Canvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Ending Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
        }

        SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        GameObject screenObject = FindDirectChild(canvasObject.transform, "Ending Screen");
        if (screenObject == null)
        {
            screenObject = new GameObject("Ending Screen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            screenObject.transform.SetParent(canvasObject.transform, false);
        }

        RectTransform screenRect = screenObject.GetComponent<RectTransform>();
        Stretch(screenRect);

        Image background = screenObject.GetComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = true;

        CanvasGroup group = screenObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        screenObject.SetActive(false);

        Image slideImage = CreateOrGetImage(screenObject.transform, "Ending Slide Image");
        RectTransform slideRect = slideImage.GetComponent<RectTransform>();
        slideRect.anchorMin = new Vector2(0.5f, 0.5f);
        slideRect.anchorMax = new Vector2(0.5f, 0.5f);
        slideRect.pivot = new Vector2(0.5f, 0.5f);
        slideRect.anchoredPosition = new Vector2(0f, 115f);
        slideRect.sizeDelta = new Vector2(1320f, 742f);
        slideImage.preserveAspect = true;
        slideImage.raycastTarget = false;
        slideImage.gameObject.SetActive(false);

        Text speakerText = CreateOrGetText(screenObject.transform, "Ending Speaker Text");
        RectTransform speakerRect = speakerText.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0.5f, 0f);
        speakerRect.anchorMax = new Vector2(0.5f, 0f);
        speakerRect.pivot = new Vector2(0.5f, 0f);
        speakerRect.anchoredPosition = new Vector2(0f, 262f);
        speakerRect.sizeDelta = new Vector2(1420f, 48f);
        speakerText.fontSize = 30;
        speakerText.alignment = TextAnchor.MiddleLeft;

        Text bodyText = CreateOrGetText(screenObject.transform, "Ending Body Text");
        RectTransform bodyRect = bodyText.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 0f);
        bodyRect.anchorMax = new Vector2(0.5f, 0f);
        bodyRect.pivot = new Vector2(0.5f, 0f);
        bodyRect.anchoredPosition = new Vector2(0f, 58f);
        bodyRect.sizeDelta = new Vector2(1420f, 210f);
        bodyText.fontSize = 28;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;

        return canvas;
    }

    private static Image CreateOrGetImage(Transform parent, string name)
    {
        GameObject existing = FindDirectChild(parent, name);
        if (existing == null)
        {
            existing = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            existing.transform.SetParent(parent, false);
        }

        return existing.GetComponent<Image>();
    }

    private static Text CreateOrGetText(Transform parent, string name)
    {
        GameObject existing = FindDirectChild(parent, name);
        if (existing == null)
        {
            existing = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            existing.transform.SetParent(parent, false);
        }

        Text text = existing.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.raycastTarget = false;
        text.supportRichText = true;
        return text;
    }

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void ConfigureSequenceController(
        EndingSequenceController controller,
        Canvas canvas,
        CanvasGroup rootGroup,
        Image background,
        Image slideImage,
        Text speakerText,
        Text bodyText,
        AudioSource bgmSource)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("canvas").objectReferenceValue = canvas;
        serialized.FindProperty("rootGroup").objectReferenceValue = rootGroup;
        serialized.FindProperty("backgroundImage").objectReferenceValue = background;
        serialized.FindProperty("slideImage").objectReferenceValue = slideImage;
        serialized.FindProperty("speakerText").objectReferenceValue = speakerText;
        serialized.FindProperty("bodyText").objectReferenceValue = bodyText;
        serialized.FindProperty("bgmSource").objectReferenceValue = bgmSource;
        serialized.FindProperty("sfxSource").objectReferenceValue = bgmSource;
        serialized.FindProperty("titleSceneName").stringValue = "Title 1";
        serialized.FindProperty("fadeInDuration").floatValue = 1f;
        serialized.FindProperty("fadeOutDuration").floatValue = 0.8f;
        serialized.FindProperty("charactersPerSecond").floatValue = 35f;
        serialized.FindProperty("slideFadeDuration").floatValue = 0.35f;

        SerializedProperty slides = serialized.FindProperty("slides");
        slides.arraySize = 3;
        ConfigureSlide(slides.GetArrayElementAtIndex(0), "그림 1) 풀 숲", "Assets/Art/1. 풀숲.png",
            "우리는 무작정 풀을 헤치고 걸었다. 도로가 나올 때까지.\n한참 걷고 나서 마침내 도로를 발견한 우리는 간신히 풀만 가득한 이 산을 벗어났다.");
        ConfigureSlide(slides.GetArrayElementAtIndex(1), "그림 2) 모텔", "Assets/Art/2. 모텔.png",
            "근처 모텔에 도착한 후 대니는 이곳에 있으면 누군가 나를 데리러 올 것이라 일러주고 사라졌다. 아무것도 기억나지 않았지만 주사를 한 대 맞으니 기분이 괜찮아진다. 다행히 그 친절한 남자는 떠나면서 나에게 여러 개의 주사기를 주고 갔다.");
        ConfigureSlide(slides.GetArrayElementAtIndex(2), "그림 3) 텔레비전", "Assets/Art/3. 텔레비전.png",
            "얼마나 시간이 지났을까, 기억을 회복할 때마다 주사를 놓으니 시간의 흐름도 알 수 없어졌다. 갖고 있던 주사기는 모두 사용했다. 조금씩 기억이 돌아오는 감각이 불쾌하다. 오래된 모텔 TV에서 내 얼굴의 수배지가 방영되고 있다.\n[ Q 연구소 부소장 폴 맥그래스 ]\n연구원인 아내와 연구소장을 살해하고 연구소를 폭파, 현재는 도주 중... 아나운서가 심각한 목소리로 말한다. 도대체 영문을 모르겠는 소리 뿐이다. 기억이 조금씩 돌아오고 있다. 아주 불쾌하고 역겨운 기분이다. 주사기, 주사기가 필요하다.\n[end1 주사기가 필요해]");

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSlide(SerializedProperty slide, string title, string imagePath, string text, string fallbackImagePath = null)
    {
        slide.FindPropertyRelative("title").stringValue = title;
        slide.FindPropertyRelative("image").objectReferenceValue = LoadSprite(imagePath) ?? LoadSprite(fallbackImagePath);
        slide.FindPropertyRelative("text").stringValue = text;
    }

    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        return assets.OfType<Sprite>().FirstOrDefault();
    }

    private static void ConfigureChoiceAction(SetEndingChoiceAction action, EndingChoiceState choiceState, EndingChoice choice, bool healPlayerToFull)
    {
        SerializedObject serialized = new SerializedObject(action);
        serialized.FindProperty("choiceState").objectReferenceValue = choiceState;
        serialized.FindProperty("choice").enumValueIndex = (int)choice;
        serialized.FindProperty("healPlayerToFull").boolValue = healPlayerToFull;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureAudioAction(PlayDialogueAudioAction action, string clipPath, DialogueAudioChannel channel)
    {
        SerializedObject serialized = new SerializedObject(action);
        serialized.FindProperty("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        serialized.FindProperty("channel").enumValueIndex = (int)channel;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureEnding2Sequence(
        Canvas canvas,
        CanvasGroup rootGroup,
        Image background,
        Image slideImage,
        Text speakerText,
        Text bodyText)
    {
        GameObject root = FindOrCreateRoot("Ending2EventRoot");
        EndingSequenceController sequenceController = GetOrAdd<EndingSequenceController>(root);
        AudioSource bgmSource = GetOrAdd<AudioSource>(root);
        bgmSource.playOnAwake = false;

        ConfigureEnding2Controller(sequenceController, canvas, rootGroup, background, slideImage, speakerText, bodyText, bgmSource);

        BossDeathEndingSequenceTrigger trigger = GetOrAdd<BossDeathEndingSequenceTrigger>(root);
        SerializedObject triggerSerialized = new SerializedObject(trigger);
        triggerSerialized.FindProperty("bossHealth").objectReferenceValue = GameObject.Find("Boss-2")?.GetComponent<BossHealth>();
        triggerSerialized.FindProperty("choiceState").objectReferenceValue = GameObject.Find("EndingEventRoot")?.GetComponent<EndingChoiceState>();
        triggerSerialized.FindProperty("sequenceController").objectReferenceValue = sequenceController;
        SerializedProperty preEndingLines = triggerSerialized.FindProperty("preEndingLines");
        preEndingLines.arraySize = 2;
        SetDialogueLine(preEndingLines.GetArrayElementAtIndex(0), "대니", "어서... 나가 봐... 그녀가 기다리는 곳으로 가라구...그동안 도와줘서 고마웠어.");
        SetDialogueLine(preEndingLines.GetArrayElementAtIndex(1), string.Empty, "대니는 기분 나쁜 웃음을 짓고 숨을 거뒀다.");
        triggerSerialized.FindProperty("requireRejectedInjection").boolValue = true;
        triggerSerialized.FindProperty("playDelay").floatValue = 1.5f;
        triggerSerialized.FindProperty("triggerOnce").boolValue = true;
        triggerSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(sequenceController);
        EditorUtility.SetDirty(trigger);
    }

    private static void ConfigureEnding2Controller(
        EndingSequenceController controller,
        Canvas canvas,
        CanvasGroup rootGroup,
        Image background,
        Image slideImage,
        Text speakerText,
        Text bodyText,
        AudioSource bgmSource)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("canvas").objectReferenceValue = canvas;
        serialized.FindProperty("rootGroup").objectReferenceValue = rootGroup;
        serialized.FindProperty("backgroundImage").objectReferenceValue = background;
        serialized.FindProperty("slideImage").objectReferenceValue = slideImage;
        serialized.FindProperty("speakerText").objectReferenceValue = speakerText;
        serialized.FindProperty("bodyText").objectReferenceValue = bodyText;
        serialized.FindProperty("bgmSource").objectReferenceValue = bgmSource;
        serialized.FindProperty("sfxSource").objectReferenceValue = bgmSource;
        serialized.FindProperty("titleSceneName").stringValue = "Title 1";
        serialized.FindProperty("fadeInDuration").floatValue = 1f;
        serialized.FindProperty("fadeOutDuration").floatValue = 0.8f;
        serialized.FindProperty("charactersPerSecond").floatValue = 35f;
        serialized.FindProperty("slideFadeDuration").floatValue = 0.35f;

        SerializedProperty blackScreenLines = serialized.FindProperty("blackScreenLines");
        blackScreenLines.arraySize = 0;

        SerializedProperty slides = serialized.FindProperty("slides");
        slides.arraySize = 3;
        ConfigureSlide(slides.GetArrayElementAtIndex(0), string.Empty, "Assets/Art/1. 풀숲.png",
            "수습할 것이 많았지만, 우선 아내의 생사부터 확인해야 했다. 나는 서둘러 집으로 뛰어갔다.");
        ConfigureSlide(slides.GetArrayElementAtIndex(1), string.Empty, "Assets/Art/엔딩 아내 사진.png",
            "비밀리에 운영되던 연구소에는 총 28명의 연구원이 있었다. 이번 사고로 28명 중 27명이 사망했다. 미친 듯이 뛰어간 집에는 아무도 없었다. 당연한 일이다. 선배의 여동생이자 동료 연구원인 나의 아내는 집이 아니라 연구소 지하에 있었으니까.",
            "Assets/Art/카드지갑 사진.png");
        ConfigureSlide(slides.GetArrayElementAtIndex(2), string.Empty, "Assets/Art/공허한 연구.실.png",
            "사형을 선고받은 범죄자가 있었다. 그는 사기와 살인을 수없이 저질렀음에도 불구하고 뻔뻔스럽게 감형을 받고자 연구소의 실험에 자원했다. 그 녀석이 아무리 고통스럽게 비명을 질러도 무시했어야 했는데. 왜 그날, 그의 고통을 덜어줘야겠다는 생각을 했을까. 왜 그 억제기의 강도를 낮췄을까. 끊임없이 떠오르는 기억에 잡아먹힐 것 같다. 아무것도 할 수가 없다. 머리가, 마음이, 구멍이라도 난 것처럼 멍하다.");

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureEndingTrigger(EndingDoorTrigger trigger, EndingChoiceState choiceState, EndingSequenceController sequenceController)
    {
        SerializedObject serialized = new SerializedObject(trigger);
        serialized.FindProperty("choiceState").objectReferenceValue = choiceState;
        serialized.FindProperty("sequenceController").objectReferenceValue = sequenceController;
        serialized.FindProperty("requireAcceptedInjection").boolValue = true;
        serialized.FindProperty("triggerOnce").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBoss2IntroAction(StartBoss2IntroAction action)
    {
        GameObject retryPoint = FindOrCreateRoot("Boss2 Player Respawn Point");
        retryPoint.transform.position = new Vector3(65.7f, 23.51f, 0f);

        GameObject checkpointObject = FindOrCreateRoot("Boss2 Retry Checkpoint");
        BossRetryCheckpoint retryCheckpoint = GetOrAdd<BossRetryCheckpoint>(checkpointObject);
        BossArenaRespawnController respawnController = Object.FindFirstObjectByType<BossArenaRespawnController>();
        Boss2Controller boss2 = GameObject.Find("Boss-2")?.GetComponent<Boss2Controller>();
        Transform boss2Platform = GameObject.Find("platform-boss2")?.transform;

        SerializedObject checkpointSerialized = new SerializedObject(retryCheckpoint);
        checkpointSerialized.FindProperty("respawnPoint").objectReferenceValue = retryPoint.transform;
        checkpointSerialized.FindProperty("retryCameraFocus").objectReferenceValue = null;
        checkpointSerialized.FindProperty("bossRetryTarget").objectReferenceValue = boss2;
        checkpointSerialized.FindProperty("beginBattleAfterRetry").boolValue = true;
        SerializedProperty raisedTargets = checkpointSerialized.FindProperty("raisedStateTargets");
        raisedTargets.arraySize = boss2Platform != null ? 1 : 0;
        if (boss2Platform != null)
        {
            raisedTargets.GetArrayElementAtIndex(0).objectReferenceValue = boss2Platform;
        }

        checkpointSerialized.FindProperty("raisedTargetLocalY").floatValue = 26f;
        checkpointSerialized.FindProperty("respawnInvincibleDuration").floatValue = 0.25f;
        checkpointSerialized.ApplyModifiedPropertiesWithoutUndo();

        Boss2IntroTrigger introTrigger = GameObject.Find("Boss2 Intro Trigger")?.GetComponent<Boss2IntroTrigger>();
        if (introTrigger != null)
        {
            SerializedObject introSerialized = new SerializedObject(introTrigger);
            introSerialized.FindProperty("retryPoint").objectReferenceValue = retryPoint.transform;
            introSerialized.FindProperty("triggerOnPlayerEnter").boolValue = false;
            introSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        SerializedObject serialized = new SerializedObject(action);
        serialized.FindProperty("introTrigger").objectReferenceValue = introTrigger;
        serialized.FindProperty("respawnController").objectReferenceValue = respawnController;
        serialized.FindProperty("retryCheckpoint").objectReferenceValue = retryCheckpoint;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBoss2DoorDialogue(
        AutoDialogueTrigger trigger,
        SetEndingChoiceAction acceptChoiceAction,
        PlayDialogueAudioAction acceptAudioAction,
        SetEndingChoiceAction rejectChoiceAction,
        PlayDialogueAudioAction rejectAudioAction,
        StartBoss2IntroAction boss2IntroAction)
    {
        SerializedObject serialized = new SerializedObject(trigger);
        SerializedProperty lines = serialized.FindProperty("lines");
        lines.arraySize = 1;

        SerializedProperty promptLine = lines.GetArrayElementAtIndex(0);
        SetDialogueLine(promptLine, string.Empty, "주사기를 어떻게 할까?");

        SerializedProperty choices = promptLine.FindPropertyRelative("choices");
        choices.arraySize = 2;

        SerializedProperty acceptChoice = choices.GetArrayElementAtIndex(0);
        acceptChoice.FindPropertyRelative("text").stringValue = "주사기를 맞는다";
        SerializedProperty acceptActions = acceptChoice.FindPropertyRelative("actionsOnChoose");
        acceptActions.arraySize = 2;
        acceptActions.GetArrayElementAtIndex(0).objectReferenceValue = acceptChoiceAction;
        acceptActions.GetArrayElementAtIndex(1).objectReferenceValue = acceptAudioAction;
        acceptChoice.FindPropertyRelative("actionsOnComplete").arraySize = 0;
        SerializedProperty acceptLines = acceptChoice.FindPropertyRelative("nextLines");
        acceptLines.arraySize = 4;
        SetDialogueLine(acceptLines.GetArrayElementAtIndex(0), "폴", "알았어.");
        SetDialogueLine(acceptLines.GetArrayElementAtIndex(1), string.Empty, "마음이 편안해진다. 조금 나른한 것 같기도 하다. 몸 상태가 회복되었다.");
        SetDialogueLine(acceptLines.GetArrayElementAtIndex(2), "대니", "이제 나갈까?");
        SetDialogueLine(acceptLines.GetArrayElementAtIndex(3), "폴", "그래.");

        SerializedProperty rejectChoice = choices.GetArrayElementAtIndex(1);
        rejectChoice.FindPropertyRelative("text").stringValue = "맞지 않는다";
        SerializedProperty rejectActions = rejectChoice.FindPropertyRelative("actionsOnChoose");
        rejectActions.arraySize = 2;
        rejectActions.GetArrayElementAtIndex(0).objectReferenceValue = rejectChoiceAction;
        rejectActions.GetArrayElementAtIndex(1).objectReferenceValue = rejectAudioAction;
        SerializedProperty rejectCompleteActions = rejectChoice.FindPropertyRelative("actionsOnComplete");
        rejectCompleteActions.arraySize = 1;
        rejectCompleteActions.GetArrayElementAtIndex(0).objectReferenceValue = boss2IntroAction;
        SerializedProperty rejectLines = rejectChoice.FindPropertyRelative("nextLines");
        rejectLines.arraySize = 12;
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(0), "폴", "아니. 이 주사는 필요 없어.");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(1), "대니", "무슨 소리야? 머리가 아파서 숨도 못 쉬고 있으면서!");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(2), "폴", "괜찮아졌어. 두통이 조금 가시니까 몇 가지 떠오르는 기억이 있어.");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(3), "대니", "그래? 뭐가 떠오르는데?");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(4), "폴", "...");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(5), "대니", "뭘 기억하길래 눈물을 흘리는 거야. 응?");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(6), "폴", "...");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(7), "대니", "기분 나쁜 표정만 짓지 말고, 대답해!");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(8), "폴", "연구소에 사고가 났었지. 한 연구원이 실험체를 동정하는 바람에 억제 장치가 느슨해졌어. 녀석은 탈출하기 위해 난동을 부리고 그 여파로 연구원 대부분이 죽었다. 두 명을 제외하고, 빌어먹을 대니, 제이콥은 내 선배 동료였어. 그리고, 나는 수감자가 아니라 이 연구소의 부소장이야.");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(9), "대니", "이거 골치 아프게 됐네. 그래서 뭐, 어쩌라구? 뒈진 사람들은 돌아오지 않아!");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(10), "폴", "내가 살아있는 한 너는 이곳에서 탈출할 수 없어. 너 같은 녀석을 바깥 세상에 내보내선 안 돼, 밖에는 아내가 있으니까.");
        SetDialogueLine(rejectLines.GetArrayElementAtIndex(11), "대니", "푸핫, 그거 정말 유감이야.");

        serialized.FindProperty("triggerOnce").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetDialogueLine(SerializedProperty line, string speaker, string text)
    {
        line.FindPropertyRelative("speaker").stringValue = speaker;
        line.FindPropertyRelative("text").stringValue = text;
        line.FindPropertyRelative("image").objectReferenceValue = null;
        line.FindPropertyRelative("hideDialogueWhileImage").boolValue = false;
        line.FindPropertyRelative("choices").arraySize = 0;
    }
}
