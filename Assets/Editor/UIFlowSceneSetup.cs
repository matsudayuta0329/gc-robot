using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 既存のゲームUIを接続し、未設定のロビー／チュートリアルに動作確認用UIを配置する。
public static class UIFlowSceneSetup
{
    [MenuItem("GC Robot/UI/Connect Scene Flows")]
    public static void ConnectScenes()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        UIAnimationSetup.CreateTemplates();
        SetupGame();
        SetupLobby();
        SetupTutorial();
        var scenes = EditorBuildSettings.scenes.ToList();
        foreach (var name in new[] { "Game", "Lobby", "Tutorial" })
        {
            string path = "Assets/Scenes/" + name + ".unity";
            var existing = scenes.Find(s => s.path == path);
            if (existing != null) existing.enabled = true;
            else scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
    }

    private static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    private static void Ref(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void TextValue(Object target, string field, string value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static Canvas Canvas()
    {
        var canvas = Find<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(990, 540);
        }
        if (Find<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        return canvas;
    }
    private static GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return go;
    }
    private static TMP_Text Label(string name, Transform parent, string text, Vector2 position)
    {
        var go = Rect(name, parent, new Vector2(640, 100), position);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = UIFont();
        label.text = text;
        label.fontSize = 26;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }
    private static TMP_FontAsset UIFont()
    {
        const string path = "Assets/UIAnimations/UIJapanese.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset != null) return asset;
        var source = AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/DotGothic16-Regular.ttf");
        asset = TMP_FontAsset.CreateFontAsset(source);
        asset.name = "UIJapanese";
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (var texture in asset.atlasTextures) AssetDatabase.AddObjectToAsset(texture, asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    public static void UpdateUIFonts()
    {
        var font = UIFont();
        foreach (string name in new[] { "Lobby", "Tutorial" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.GetComponentInParent<MessageBox>() != null ||
                    text.GetComponentInParent<ConfirmationDialog>() != null ||
                    text.GetComponentInParent<OperationGuideUI>() != null ||
                    text.GetComponentInParent<TutorialUI>() != null ||
                    text.GetComponentInParent<NPC>() != null)
                    text.font = font;
            }
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
    }
    private static Button Button(string name, Transform parent, Vector2 position)
    {
        var go = Rect(name, parent, new Vector2(180, 60), position);
        var image = go.AddComponent<Image>();
        image.color = new Color(0.18f, 0.28f, 0.38f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var text = Label("Label", go.transform, name, Vector2.zero);
        text.rectTransform.sizeDelta = new Vector2(180, 60);
        return button;
    }
    private static ConfirmationDialog Confirmation(Transform canvas)
    {
        var go = Rect("ConfirmationDialog", canvas, new Vector2(760, 260), Vector2.zero);
        var image = go.AddComponent<Image>();
        image.color = new Color(0.04f, 0.07f, 0.1f, 0.97f);
        var dialog = go.AddComponent<ConfirmationDialog>();
        Ref(dialog, "messageText", Label("Message", go.transform, "", new Vector2(0, 55)));
        Ref(dialog, "submitButton", Button("はい", go.transform, new Vector2(-120, -65)));
        Ref(dialog, "cancelButton", Button("いいえ", go.transform, new Vector2(120, -65)));
        return dialog;
    }
    private static MessageBox Conversation(Transform canvas)
    {
        var go = Rect("MessageBox", canvas, new Vector2(760, 260), Vector2.zero);
        var image = go.AddComponent<Image>();
        image.color = new Color(0.04f, 0.07f, 0.1f, 0.97f);
        var box = go.AddComponent<MessageBox>();
        Ref(box, "messageText", Label("Message", go.transform, "", new Vector2(0, 55)));
        Ref(box, "nextButton", Button("次へ", go.transform, new Vector2(0, -65)));
        return box;
    }
    private static UIParent Parent(Canvas canvas, AnimatedUIElement[] elements, ResultUI result = null)
    {
        var parent = canvas.GetComponent<UIParent>();
        if (parent == null) parent = canvas.gameObject.AddComponent<UIParent>();
        var serialized = new SerializedObject(parent);
        var list = serialized.FindProperty("elements");
        list.arraySize = elements.Length;
        for (int i = 0; i < elements.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = elements[i];
        serialized.FindProperty("resultUI").objectReferenceValue = result;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (canvas.transform.Find("Transition") == null)
        {
            var overlay = Rect("Transition", canvas.transform, Vector2.zero, Vector2.zero);
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            overlay.AddComponent<Image>().color = Color.black;
            overlay.GetComponent<Image>().raycastTarget = false;
            var group = overlay.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            overlay.SetActive(false);
        }
        Selection.activeGameObject = canvas.gameObject;
        UIAnimationSetup.AssignSelected();
        return parent;
    }
    private static Player Player()
    {
        var player = Find<Player>();
        if (player != null) return player;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SRC/Player.prefab");
        return ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<Player>();
    }

    private static void SetupGame()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var manager = Find<GameManager>();
        var gameUI = Find<GameUI>();
        var result = Find<ResultUI>();
        Ref(manager, "uiParent", Parent(Canvas(), new AnimatedUIElement[] { gameUI }, result));
        TextValue(manager, "lobbyScene", "Lobby");
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupLobby()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        if (Find<LobbyManager>() != null) return;
        var canvas = Canvas();
        var player = Player();
        var dialog = Confirmation(canvas.transform);
        var messageBox = Conversation(canvas.transform);
        var manager = new GameObject("LobbyManager").AddComponent<LobbyManager>();
        Ref(manager, "uiParent", Parent(canvas, System.Array.Empty<AnimatedUIElement>()));
        Ref(manager, "player", player);
        Ref(manager, "inputReader", player.GetComponent<InputReader>());
        Ref(manager, "confirmationDialog", dialog);
        Ref(manager, "messageBox", messageBox);
        // Gateの外観は既存のマップを利用し、接触判定だけを子に追加する。
        var gates = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name == "Gate" || t.name.StartsWith("Gate (")).ToArray();
        foreach (var visual in gates)
        {
            var trigger = new GameObject("GateTrigger");
            trigger.transform.position = visual.GetComponent<Renderer>() != null
                ? visual.GetComponent<Renderer>().bounds.center : visual.position;
            trigger.transform.position = new Vector3(trigger.transform.position.x,
                player.transform.position.y + 1, trigger.transform.position.z);
            var collider = trigger.AddComponent<BoxCollider>();
            collider.size = new Vector3(5, 3, 3);
            collider.isTrigger = true;
            // CharacterControllerとの接触を確実に受け取る。
            var body = trigger.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var gate = trigger.AddComponent<Gate>();
            Ref(gate, "lobbyManager", manager);
            TextValue(gate, "moveScene", "Game");
            TextValue(gate, "message", "ゲームを開始しますか？");
            trigger.transform.SetParent(visual, true);
        }
        var npcObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npcObject.name = "TutorialNPC";
        npcObject.transform.position = player.transform.position + new Vector3(3, 1, 0);
        var npc = npcObject.AddComponent<NPC>();
        Ref(npc, "lobbyManager", manager);
        TextValue(npc, "message", "制限時間内にごみを集めてください。ゲートからゲームを開始できます。");
        var guide = new GameObject("TalkGuide", typeof(TextMeshPro));
        guide.transform.SetParent(npc.transform, false);
        guide.transform.localPosition = Vector3.up * 1.5f;
        var guideText = guide.GetComponent<TextMeshPro>();
        guideText.font = UIFont();
        guideText.text = "E : 話しかける";
        guideText.fontSize = 4;
        guideText.alignment = TextAlignmentOptions.Center;
        Ref(npc, "guide", guide);
        var playerFields = new SerializedObject(player);
        playerFields.FindProperty("npcLayer").intValue = 1;
        playerFields.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupTutorial()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity");
        if (Find<TutorialManager>() != null) return;
        var canvas = Canvas();
        var player = Player();
        player.transform.position = Vector3.zero;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "TutorialFloor";
        floor.transform.position = new Vector3(0, -1, 0);
        floor.transform.localScale = new Vector3(60, 1, 60);
        var camera = Find<Camera>();
        camera.transform.position = new Vector3(0, 22, -18);
        camera.transform.LookAt(player.transform);
        var message = Conversation(canvas.transform);
        var guideObject = Rect("OperationGuide", canvas.transform, new Vector2(760, 160), new Vector2(0, -150));
        var guide = guideObject.AddComponent<OperationGuideUI>();
        Ref(guide, "messageText", Label("Guide", guideObject.transform, "", Vector2.zero));
        var tutorialObject = Rect("TutorialUI", canvas.transform, new Vector2(200, 80), new Vector2(350, 200));
        var tutorial = tutorialObject.AddComponent<TutorialUI>();
        Ref(tutorial, "messageBox", message);
        Ref(tutorial, "operationGuide", guide);
        Ref(tutorial, "trashCountText", Label("Count", tutorialObject.transform, "0", Vector2.zero));
        var parent = Parent(canvas, new AnimatedUIElement[] { tutorial });
        // メッセージと操作ガイドはTutorialPresenterが切り替える。
        message.gameObject.GetComponent<CanvasGroup>().alpha = 0;
        guide.gameObject.GetComponent<CanvasGroup>().alpha = 0;
        var manager = new GameObject("TutorialManager").AddComponent<TutorialManager>();
        Ref(manager, "tutorialUI", tutorial);
        Ref(manager, "uiParent", parent);
        Ref(manager, "player", player);
        Ref(manager, "inputReader", player.GetComponent<InputReader>());
        TextValue(manager, "lobbyScene", "Lobby");
        var serialized = new SerializedObject(manager);
        var data = serialized.FindProperty("tutorialData");
        string[] messages = { "制限時間内にごみを集めましょう。", "前後移動と旋回を試してください。",
            "ダッシュを試してください。", "吸引ボタンを押してごみを回収してください。", "練習は終了です。ロビーへ移動します。" };
        ActionType[] actions = { ActionType.Move, ActionType.Move, ActionType.Dash, ActionType.Vacuum, ActionType.Move };
        data.arraySize = messages.Length;
        for (int i = 0; i < messages.Length; i++)
        {
            var item = data.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("message").stringValue = messages[i];
            item.FindPropertyRelative("isTutorial").boolValue = i > 0 && i < messages.Length - 1;
            item.FindPropertyRelative("tutorialAction").intValue = (int)actions[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var trashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SRC/Garbage/Can.prefab");
        var trash = (GameObject)PrefabUtility.InstantiatePrefab(trashPrefab);
        trash.transform.position = new Vector3(0, 0, 3);
        EditorSceneManager.SaveScene(scene);
    }
}
