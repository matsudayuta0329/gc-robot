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
    public bool IsOpen => open;

    public void Initialize(Player player, InputReader input)
    {
        this.player = player; this.input = input;
        var collider = gameObject.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.size = new Vector3(2.5f, 2.5f, 2f);
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube); visual.name = "Ranking Board Display"; visual.transform.SetParent(transform, false); visual.transform.localPosition = Vector3.up * 1.3f; visual.transform.localScale = new Vector3(2.2f, 2.5f, 0.12f);
        UnityEngine.Object.Destroy(visual.GetComponent<Collider>());
        var canvasObject = new GameObject("Ranking Board Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false); canvasObject.transform.localPosition = Vector3.up * 1.3f + Vector3.forward * -0.08f; canvasObject.transform.localRotation = Quaternion.identity; canvasObject.transform.localScale = Vector3.one * 0.002f;
        boardCanvas = canvasObject.GetComponent<Canvas>(); boardCanvas.renderMode = RenderMode.WorldSpace;
        var rect = canvasObject.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1000, 1100);
        var textGo = new GameObject("Ranking Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); textGo.transform.SetParent(canvasObject.transform, false);
        var textRect = textGo.GetComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(50, 140); textRect.offsetMax = new Vector2(-50, -50);
        var text = textGo.GetComponent<TextMeshProUGUI>(); text.fontSize = 48; text.color = Color.white; text.alignment = TextAlignmentOptions.TopLeft;
        var closeGo = new GameObject("Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); closeGo.transform.SetParent(canvasObject.transform, false);
        var cr = closeGo.GetComponent<RectTransform>(); cr.anchorMin = new Vector2(.3f, 0); cr.anchorMax = new Vector2(.7f, 0); cr.pivot = new Vector2(.5f, 0); cr.sizeDelta = new Vector2(240, 100); cr.anchoredPosition = new Vector2(0, 20);
        closeGo.GetComponent<Image>().color = new Color(.2f,.35f,.5f,1); closeGo.GetComponent<Button>().onClick.AddListener(Close);
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); labelGo.transform.SetParent(closeGo.transform, false); var lr=labelGo.GetComponent<RectTransform>(); lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
        var label=labelGo.GetComponent<TextMeshProUGUI>(); label.text="閉じる";label.fontSize=42;label.alignment=TextAlignmentOptions.Center;
        text.text = BuildText(); canvasObject.SetActive(false);
        var cameraObject = new GameObject("Ranking View Camera"); cameraObject.transform.SetPositionAndRotation(transform.position + transform.forward * 3f + Vector3.up * 1.5f, Quaternion.LookRotation(transform.position + Vector3.up*1.3f - (transform.position + transform.forward*3f + Vector3.up*1.5f)));
        viewCamera = cameraObject.AddComponent<CinemachineCamera>(); viewCamera.Priority = 0; viewCamera.enabled = false;
        if (Camera.main != null && Camera.main.GetComponent<CinemachineBrain>() == null) Camera.main.gameObject.AddComponent<CinemachineBrain>();
        guide?.SetActive(false);
    }
    public void SetGuideEnable(bool value) { if (guide != null) guide.SetActive(value && !open); }
    public async Awaitable OpenAsync()
    {
        if (open || player == null || input == null) return;
        open = true; SetGuideEnable(false); boardCanvas.gameObject.SetActive(true);
        var actions=input.GetEnabledActions(); bool wasPaused=player.IsPaused;
        player.SetPaused(true); input.SetEnableAction(Array.Empty<ActionType>()); viewCamera.enabled=true; viewCamera.Priority=100;
        try { while(open) await Awaitable.NextFrameAsync(destroyCancellationToken); }
        finally { viewCamera.Priority=0; viewCamera.enabled=false; boardCanvas.gameObject.SetActive(false); if(player!=null)player.SetPaused(wasPaused);if(input!=null)input.SetEnableAction(actions); }
    }
    public void Close() { open=false; }
    private string BuildText()
    {
        var b=new StringBuilder("ごみ回収ランキング\n"); var entries=ScoreRanking.Entries;
        for(int i=0;i<10;i++) b.AppendLine(i<entries.Count?$"{i+1,2}. {entries[i].name}   {entries[i].count}個":$"{i+1,2}. ---");
        return b.ToString();
    }
}
