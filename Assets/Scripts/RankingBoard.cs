using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public sealed class RankingBoard : MonoBehaviour
{
    [SerializeField] private GameObject guide;
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
        var collider = gameObject.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.size = new Vector3(2.5f, 2.5f, 2f);
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube); visual.name = "Ranking Board Display"; visual.transform.SetParent(transform, false); visual.transform.localPosition = Vector3.up * 1.3f; visual.transform.localScale = new Vector3(2.2f, 2.5f, 0.12f);
        UnityEngine.Object.Destroy(visual.GetComponent<Collider>());
        var canvasObject = new GameObject("Ranking Board Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false); canvasObject.transform.localPosition = Vector3.up * 1.3f + Vector3.forward * -0.08f; canvasObject.transform.localRotation = Quaternion.identity; canvasObject.transform.localScale = Vector3.one * 0.002f;
        boardCanvas = canvasObject.GetComponent<Canvas>(); boardCanvas.renderMode = RenderMode.WorldSpace;
        var rect = canvasObject.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1000, 1100);
        var textGo = new GameObject("Ranking Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); textGo.transform.SetParent(canvasObject.transform, false);
        var textRect = textGo.GetComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(50, 140); textRect.offsetMax = new Vector2(-50, -50);
        var text = textGo.GetComponent<TextMeshProUGUI>(); text.fontSize = 48; text.color = Color.black; text.richText = false; rankingText = text; text.alignment = TextAlignmentOptions.TopLeft;
        var closeGo = new GameObject("Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); closeGo.transform.SetParent(canvasObject.transform, false);
        var cr = closeGo.GetComponent<RectTransform>(); cr.anchorMin = new Vector2(.3f, 0); cr.anchorMax = new Vector2(.7f, 0); cr.pivot = new Vector2(.5f, 0); cr.sizeDelta = new Vector2(240, 100); cr.anchoredPosition = new Vector2(0, 20);
        closeGo.GetComponent<Image>().color = new Color(.2f,.35f,.5f,1); closeGo.GetComponent<Button>().onClick.AddListener(Close);
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); labelGo.transform.SetParent(closeGo.transform, false); var lr=labelGo.GetComponent<RectTransform>(); lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
        var label=labelGo.GetComponent<TextMeshProUGUI>(); label.text="閉じる";label.fontSize=42;label.alignment=TextAlignmentOptions.Center;
        text.text = BuildText();
        closeButton = closeGo;
        closeButton.SetActive(false);
        guide = new GameObject("Interaction Guide", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        guide.transform.SetParent(canvasObject.transform, false);
        var gr = guide.GetComponent<RectTransform>();
        gr.sizeDelta = new Vector2(900, 90); gr.anchoredPosition = new Vector2(0, -480);
        var gt = guide.GetComponent<TextMeshProUGUI>();
        gt.text = "[E] Ranking"; gt.fontSize = 42; gt.color = Color.black; gt.alignment = TextAlignmentOptions.Center;
        var source = FindFirstObjectByType<MessageBox>(FindObjectsInactive.Include)?.GetComponentInChildren<TMP_Text>(true);
        if (source != null) { text.font = source.font; label.font = source.font; gt.font = source.font; }
        var cameraObject = new GameObject("Ranking View Camera"); cameraObject.transform.SetPositionAndRotation(transform.position + transform.forward * -3f + Vector3.up * 1.5f, Quaternion.LookRotation(transform.position + Vector3.up*1.3f - (transform.position + transform.forward*-3f + Vector3.up*1.5f)));
        cameraObject.transform.SetParent(transform, true);
        viewCamera = cameraObject.AddComponent<CinemachineCamera>(); viewCamera.Priority = 0; viewCamera.enabled = false;
        viewCamera.Lens.FieldOfView = 50;
        viewCamera.Lens.NearClipPlane = 0.1f;
        guide?.SetActive(false);
    }
    public void SetGuideEnable(bool value) { if (guide != null) guide.SetActive(value && !open); }
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
            if (follow != null) follow.enabled = false;
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
        }
        finally { if (version == sessionVersion) RestoreSession(); }
    }

    public void Close()
    {
        open = false;
        RestoreSession();
    }

    private void RestoreSession()
    {
        if (!sessionActive) return;
        sessionActive = false;
        open = false;
        if (viewCamera != null) viewCamera.enabled = false;
        if (brain != null) brain.enabled = false;
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

    private void OnDisable() => Close();

    private string BuildText()
    {
        var b=new StringBuilder("ごみ回収ランキング\n"); var entries=ScoreRanking.Entries;
        for(int i=0;i<10;i++) b.AppendLine(i<entries.Count?$"{i+1,2}. {entries[i].name}   {entries[i].count}個":$"{i+1,2}. ---");
        return b.ToString();
    }
}
