using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private GameObject credits;
    [SerializeField] private Button startButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button closeCreditsButton;
    [SerializeField] private UIParent uiParent;
    [SerializeField] private Animator animator;
    [SerializeField] private string startTrigger = "Start";
    [SerializeField] private string tutorialScene;
    private bool isStarting;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartButtonClicked);
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
        if (closeCreditsButton != null) closeCreditsButton.onClick.AddListener(CloseCredits);
    }

    private async void Start()
    {
        if (uiParent == null) return;
        if (startButton != null) startButton.interactable = false;
        try { await uiParent.Enable(destroyCancellationToken); }
        catch (OperationCanceledException) { return; }
        catch (Exception exception) { Debug.LogException(exception, this); return; }
        if (startButton != null) startButton.interactable = true;
    }
    public void OpenCredits() { if (credits != null) credits.SetActive(true); }
    public void CloseCredits() { if (credits != null) credits.SetActive(false); }
    public void OnStartButtonClicked()
    {
        if (isStarting) return;
        if (uiParent != null) { StartGame(); return; }
        if (animator != null && !string.IsNullOrEmpty(startTrigger)) animator.SetTrigger(startTrigger);
        else StartGame();
    }
    // 開始アニメーションのAnimation Eventから呼ぶ。
    public async void StartGame()
    {
        if (isStarting || string.IsNullOrWhiteSpace(tutorialScene)) return;
        isStarting = true;
        if (startButton != null) startButton.interactable = false;
        try
        {
            if (uiParent != null) await uiParent.FinishAsync(destroyCancellationToken);
            await SceneManager.LoadSceneAsync(tutorialScene);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            isStarting = false;
            if (startButton != null) startButton.interactable = true;
        }
    }
    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(OnStartButtonClicked);
        if (creditsButton != null) creditsButton.onClick.RemoveListener(OpenCredits);
        if (closeCreditsButton != null) closeCreditsButton.onClick.RemoveListener(CloseCredits);
    }
}
