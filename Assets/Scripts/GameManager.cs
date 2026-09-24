using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Serializable]
    public class TrashEntry
    {
        public GameObject trashObject;
        [Range(1, 3)] public int Size = 1;
    }

    [SerializeField] private List<TrashEntry> trashObjects = new List<TrashEntry>();
    [SerializeField, Min(0)] private float limit = 60f;
    [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
    [SerializeField, Range(1, 100)]private int maxGenCount = 15;
    [SerializeField] private Transform maxSpawnPoint;
    [SerializeField] private Transform minSpawnPoint;
    [SerializeField] private Transform trashParent;
    [SerializeField] private Player player;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private GarbageCounter garbageCounter;
    [SerializeField] private GameUI gameUI;
    [SerializeField] private ResultUI resultUI;
    [SerializeField] private string lobbyScene;
    private readonly List<TrashEntry> validTrash = new List<TrashEntry>();
    private readonly List<GameObject> spawnedTrash = new List<GameObject>();
    public float RemainingTime { get; private set; }
    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (player != null && garbageCounter == null)
            garbageCounter = player.GetComponent<GarbageCounter>();
        if (player != null) player.SetPaused(true);
        if (inputReader != null) inputReader.SetEnableAction(Array.Empty<ActionType>());
    }

    private async void Start()
    {
        if (player == null || inputReader == null || garbageCounter == null || gameUI == null || resultUI == null)
        {
            Debug.LogError("GameManager: Player / InputReader / GarbageCounter / GameUI / ResultUIを設定してください。", this);
            return;
        }
        foreach (var entry in trashObjects)
        {
            if (entry != null && entry.trashObject != null && entry.trashObject.GetComponent<Garbage>() != null)
                validTrash.Add(entry);
            else
                Debug.LogWarning("GameManager: Garbageのないごみ設定をスキップしました。", this);
        }
        garbageCounter.OnUpdateCount += gameUI.SetTrashNum;
        try
        {
            resultUI.Disable();
            garbageCounter.ResetCount();
            RemainingTime = Mathf.Max(0, limit);
            gameUI.SetTime(RemainingTime);
            gameUI.Enable();
            await WaitRealtime(gameUI.EnableDuration);
            player.SetPaused(false);
            inputReader.SetEnableAction(new[] { ActionType.Move, ActionType.Dash, ActionType.Vacuum, ActionType.Look });
            IsPlaying = true;
            float nextSpawn = 0;

            while (RemainingTime > 0)
            {
                if (nextSpawn <= 0)
                {
                    if(trashParent.childCount <= maxGenCount)
                        SpawnTrash();

                    nextSpawn = Mathf.Max(0.01f, spawnInterval);
                }
                await Awaitable.NextFrameAsync(destroyCancellationToken);
                RemainingTime = Mathf.Max(0, RemainingTime - Time.deltaTime);
                nextSpawn -= Time.deltaTime;
                gameUI.SetTime(RemainingTime);
            }
            IsPlaying = false;
            player.SetPaused(true);
            inputReader.SetEnableAction(Array.Empty<ActionType>());
            resultUI.Init(garbageCounter.CollectedCount);
            resultUI.Enable();
            gameUI.Disable();
            await WaitRealtime(Mathf.Max(gameUI.DisableDuration, resultUI.EnableDuration));
            while (!resultUI.IsConfirmed)
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            resultUI.Disable();
            await WaitRealtime(resultUI.DisableDuration);
            if (!string.IsNullOrWhiteSpace(lobbyScene))
                await SceneManager.LoadSceneAsync(lobbyScene);
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsPlaying = false;
            if (player != null) player.SetPaused(true);
            if (inputReader != null) inputReader.SetEnableAction(Array.Empty<ActionType>());
        }
    }

    private void SpawnTrash()
    {
        if (validTrash.Count == 0) return;
        var entry = validTrash[UnityEngine.Random.Range(0, validTrash.Count)];
        Vector3 spawnPoint = new Vector3(
                Mathf.Lerp(maxSpawnPoint.position.x, minSpawnPoint.position.x, UnityEngine.Random.value),
                minSpawnPoint.position.y,
                Mathf.Lerp(maxSpawnPoint.position.z, minSpawnPoint.position.z, UnityEngine.Random.value)
            );

        var instance = Instantiate(entry.trashObject, spawnPoint, Quaternion.identity, trashParent);
        instance.GetComponent<Garbage>().Init(entry.Size);
        spawnedTrash.RemoveAll(item => item == null);
        spawnedTrash.Add(instance);
    }

    private async Awaitable WaitRealtime(float duration)
    {
        float end = Time.unscaledTime + duration;
        while (Time.unscaledTime < end)
            await Awaitable.NextFrameAsync(destroyCancellationToken);
    }

    private void OnDestroy()
    {
        if (garbageCounter != null && gameUI != null)
        {
            garbageCounter.OnUpdateCount -= gameUI.SetTrashNum;
        }
        foreach (var trash in spawnedTrash)
            if (trash != null) Destroy(trash);
    }
}
