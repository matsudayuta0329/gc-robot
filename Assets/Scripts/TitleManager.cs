using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private GameObject credits;
    [SerializeField] private Button startButton;
    [SerializeField] private Animator animator;
    [SerializeField] private string startTrigger = "Start";
    [SerializeField] private string tutorialScene;
    private bool isStarting;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartButtonClicked);
    }
    public void OpenCredits() { if (credits != null) credits.SetActive(true); }
    public void CloseCredits() { if (credits != null) credits.SetActive(false); }
    public void OnStartButtonClicked()
    {
        if (isStarting) return;
        if (animator != null && !string.IsNullOrEmpty(startTrigger)) animator.SetTrigger(startTrigger);
        else StartGame();
    }
    // 開始アニメーションのAnimation Eventから呼ぶ。
    public async void StartGame()
    {
        if (isStarting || string.IsNullOrWhiteSpace(tutorialScene)) return;
        isStarting = true;
        try { await SceneManager.LoadSceneAsync(tutorialScene); }
        catch (Exception exception)
        {
            isStarting = false;
            Debug.LogException(exception, this);
        }
    }
    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(OnStartButtonClicked);
    }
}
