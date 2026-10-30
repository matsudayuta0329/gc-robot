using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public sealed class RankingBoard : MonoBehaviour
{
    [SerializeField] private GameObject guide;
    [SerializeField, Min(0.01f)] private float cameraBlendDuration = 0.7f;
    private CinemachineCamera returnCamera;
    private CinemachineBlendDefinition savedBlend;
    private CinemachineBlenderSettings savedCustomBlends;
    private CinemachineBrain.UpdateMethods savedUpdateMethod;
    private int lastBrainUpdateFrame = -1;
    private Renderer[] playerRenderers;
    private bool[] rendererStates;
    private Player player;
    private InputReader input;
    private CinemachineCamera viewCamera;
    private Canvas boardCanvas;
    private bool open;
    private bool sessionActive;
    private int sessionVersion;
    private Camera outputCamera;
    private CinemachineBrain brain;
    private PlayerCamera follow;
    private bool brainEnabled, followEnabled, wasPaused;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private float savedFov, savedSize, savedNear, savedFar;
    private ActionType[] savedActions;
    private TMP_Text rankingText;
    private GameObject closeButton;
    private LobbyManager lobby;
    public bool IsOpen => open;

    public void Initialize(Player player, InputReader input)
    {
        this.player = player; this.input = input;
        lobby = FindFirstObjectByType<LobbyManager>();
        var collider = gameObject.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.center = Vector3.up * 4.4f; collider.size = new Vector3(8.5f, 9.2f, 3f);

        var canvasObject = new GameObject("Ranking Board Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false); canvasObject.transform.localPosition = Vector3.up * 4.6f + Vector3.forward * -0.08f; canvasObject.transform.localRotation = Quaternion.identity; canvasObject.transform.localScale = Vector3.one * 0.008f;
        boardCanvas = canvasObject.GetComponent<Canvas>(); boardCanvas.renderMode = RenderMode.WorldSpace;
        var rect = canvasObject.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1000, 1100);
        // CanvasのImageは2三角形のQuad。UI標準の透明ブレンドで黒い背景を描画する。
        var background = new GameObject("Background Quad", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(canvasObject.transform, false);
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(-30, -30); bgRect.offsetMax = new Vector2(30, 30);
        background.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        background.GetComponent<Image>().raycastTarget = false;
        var textGo = new GameObject("Ranking Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); textGo.transform.SetParent(canvasObject.transform, false);
        var textRect = textGo.GetComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(50, 140); textRect.offsetMax = new Vector2(-50, -170);
        var text = textGo.GetComponent<TextMeshProUGUI>(); text.fontSize = 48; text.color = Color.white; text.richText = false; rankingText = text; text.alignment = TextAlignmentOptions.TopLeft;
        var headingObject = new GameObject("Ranking Heading", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        headingObject.transform.SetParent(canvasObject.transform, false);
        var headingRect = headingObject.GetComponent<RectTransform>();
        headingRect.anchorMin = headingRect.anchorMax = new Vector2(0.5f, 1f);
        headingRect.sizeDelta = new Vector2(960, 140);
        headingRect.anchoredPosition = new Vector2(0, -85);
        var heading = headingObject.GetComponent<TextMeshProUGUI>();
        heading.text = "ごみ回収ランキング";
        heading.fontSize = 85; heading.color = Color.white; heading.alignment = TextAlignmentOptions.Center;
        var closeGo = new GameObject("Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); closeGo.transform.SetParent(canvasObject.transform, false);
        var cr = closeGo.GetComponent<RectTransform>(); cr.anchorMin = new Vector2(.3f, 0); cr.anchorMax = new Vector2(.7f, 0); cr.pivot = new Vector2(.5f, 0); cr.sizeDelta = new Vector2(240, 100); cr.anchoredPosition = new Vector2(0, 20);
        closeGo.GetComponent<Image>().color = new Color(.2f,.35f,.5f,1); closeGo.GetComponent<Button>().onClick.AddListener(Close);
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); labelGo.transform.SetParent(closeGo.transform, false); var lr=labelGo.GetComponent<RectTransform>(); lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
        var label=labelGo.GetComponent<TextMeshProUGUI>(); label.text="閉じる";label.fontSize=42;label.alignment=TextAlignmentOptions.Center;
        text.text = BuildText();
        closeButton = closeGo;
        closeButton.SetActive(false);
        guide = new GameObject("Interaction Guide", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var guideCanvas = new GameObject("Ranking Guide Overlay", typeof(Canvas), typeof(CanvasScaler));
        guideCanvas.transform.SetParent(transform, false);
        var overlay = guideCanvas.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 100;
        var scaler = guideCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        guide.transform.SetParent(guideCanvas.transform, false);
        var gr = guide.GetComponent<RectTransform>();
        gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0);
        gr.sizeDelta = new Vector2(900, 90); gr.anchoredPosition = new Vector2(0, 110);
        var gt = guide.GetComponent<TextMeshProUGUI>();
        gt.text = "[E] ランキングを見る"; gt.fontSize = 42; gt.raycastTarget = false; gt.color = Color.white; gt.alignment = TextAlignmentOptions.Center;
        var source = FindFirstObjectByType<MessageBox>(FindObjectsInactive.Include)?.GetComponentInChildren<TMP_Text>(true);
        if (source != null) { heading.font = source.font; text.font = source.font; label.font = source.font; gt.font = source.font; }
        var cameraObject = new GameObject("Ranking View Camera");
        cameraObject.transform.SetPositionAndRotation(canvasObject.transform.position - transform.forward * 10.5f, Quaternion.LookRotation(transform.forward, transform.up));
        cameraObject.transform.SetParent(transform, true);
        viewCamera = cameraObject.AddComponent<CinemachineCamera>(); viewCamera.Priority = 0; viewCamera.enabled = false;
        viewCamera.Lens.FieldOfView = 50;
        viewCamera.Lens.NearClipPlane = 0.1f;
        guide?.SetActive(false);
    }
    public void SetGuideEnable(bool value) { if (guide != null) guide.SetActive(value && !sessionActive); }
    public async Awaitable OpenAsync()
    {
        if (sessionActive || player == null || input == null || !isActiveAndEnabled) return;
        outputCamera = Camera.main;
        if (outputCamera == null || (lobby != null && !lobby.TryBeginRanking())) return;
        int version = ++sessionVersion;
        sessionActive = true;
        open = true;
        try
        {
            savedActions = input.GetEnabledActions();
            wasPaused = player.IsPaused;
            savedPosition = outputCamera.transform.position;
            savedRotation = outputCamera.transform.rotation;
            savedFov = outputCamera.fieldOfView;
            savedSize = outputCamera.orthographicSize;
            savedNear = outputCamera.nearClipPlane;
            savedFar = outputCamera.farClipPlane;
            follow = outputCamera.GetComponent<PlayerCamera>();
            followEnabled = follow != null && follow.enabled;
            brain = outputCamera.GetComponent<CinemachineBrain>();
            brainEnabled = brain != null && brain.enabled;
            if (brain == null) brain = outputCamera.gameObject.AddComponent<CinemachineBrain>();
            savedUpdateMethod = brain.UpdateMethod;
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            lastBrainUpdateFrame = -1;
            savedBlend = brain.DefaultBlend;
            savedCustomBlends = brain.CustomBlends;
            brain.CustomBlends = null;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Linear, cameraBlendDuration);
            if (follow != null) follow.enabled = false;
            if (returnCamera == null)
            {
                var go = new GameObject("Ranking Return Camera");
                go.transform.SetParent(transform, false);
                returnCamera = go.AddComponent<CinemachineCamera>();
            }
            returnCamera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            returnCamera.Lens = LensSettings.FromCamera(outputCamera);
            returnCamera.Priority = int.MaxValue - 1;
            returnCamera.enabled = true;
            brain.enabled = true;
            UpdateBrain();
            playerRenderers = player.GetComponentsInChildren<Renderer>(true);
            rendererStates = new bool[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                rendererStates[i] = playerRenderers[i].enabled;
                playerRenderers[i].enabled = false;
            }
            rankingText.text = BuildText();
            boardCanvas.worldCamera = outputCamera;
            closeButton.SetActive(true);
            SetGuideEnable(false);
            player.SetPaused(true);
            input.SetEnableAction(Array.Empty<ActionType>());
            input.Cancel += Close;
            viewCamera.Priority = int.MaxValue;
            viewCamera.enabled = true;
            brain.enabled = true;
            while (open && version == sessionVersion) await Awaitable.NextFrameAsync(destroyCancellationToken);
            if (sessionActive && version == sessionVersion)
            {
                viewCamera.enabled = false;
                closeButton.SetActive(false);
                // 切替先をBrainに反映してからブレンド完了を待つ。
                UpdateBrain();
                await Awaitable.NextFrameAsync(destroyCancellationToken);
                await Awaitable.NextFrameAsync(destroyCancellationToken);
                while (brain != null && brain.IsBlending && sessionActive)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
        }
        finally { if (version == sessionVersion) RestoreSession(); }
    }

    public void Close()
    {
        open = false;
    }

    private void RestoreSession()
    {
        if (!sessionActive) return;
        sessionActive = false;
        open = false;
        if (viewCamera != null) viewCamera.enabled = false;
        if (brain != null) brain.enabled = false;
        if (returnCamera != null) returnCamera.enabled = false;
        if (brain != null) { brain.UpdateMethod = savedUpdateMethod; brain.DefaultBlend = savedBlend; brain.CustomBlends = savedCustomBlends; }
        if (playerRenderers != null)
            for (int i = 0; i < playerRenderers.Length; i++)
                if (playerRenderers[i] != null) playerRenderers[i].enabled = rendererStates[i];
        if (outputCamera != null)
        {
            outputCamera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            outputCamera.fieldOfView = savedFov;
            outputCamera.orthographicSize = savedSize;
            outputCamera.nearClipPlane = savedNear;
            outputCamera.farClipPlane = savedFar;
        }
        if (follow != null) follow.enabled = followEnabled;
        if (brain != null) brain.enabled = brainEnabled;
        if (closeButton != null) closeButton.SetActive(false);
        if (player != null) player.SetPaused(wasPaused);
        if (input != null)
        {
            input.Cancel -= Close;
            input.SetEnableAction(savedActions);
        }
        if (lobby != null) lobby.EndRanking();
    }

    private void LateUpdate() => UpdateBrain();

    private void UpdateBrain()
    {
        if (!sessionActive || brain == null || !brain.enabled || lastBrainUpdateFrame == Time.frameCount) return;
        lastBrainUpdateFrame = Time.frameCount;
        brain.ManualUpdate();
    }

    private void OnDisable() { Close(); RestoreSession(); }

    private string BuildText()
    {
        var b=new StringBuilder(); var entries=ScoreRanking.Entries;
        for(int i=0;i<10;i++) b.AppendLine(i<entries.Count?$"{i+1,2}. {entries[i].name}   {entries[i].count}個":$"{i+1,2}. ---");
        return b.ToString();
    }
}
