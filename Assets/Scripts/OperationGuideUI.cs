using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OperationGuideUI : MonoBehaviour, IUIElement
{
    [Serializable]
    public class GuideEntry
    {
        public ActionType actionType;
        public GameObject guide;
    }

    [SerializeField] private List<GuideEntry> guides = new List<GuideEntry>();
    [SerializeField] private TMP_Text messageText;
    private readonly Dictionary<ActionType, List<GameObject>> lookup = new Dictionary<ActionType, List<GameObject>>();
    private bool initialized;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        foreach (var entry in guides)
        {
            if (entry == null || entry.guide == null) continue;
            if (!lookup.TryGetValue(entry.actionType, out var objects))
                lookup[entry.actionType] = objects = new List<GameObject>();
            objects.Add(entry.guide);
        }
    }

    public void Enable() => gameObject.SetActive(true);
    public void Disable() => gameObject.SetActive(false);
    public void SetText(string text)
    {
        if (messageText != null) messageText.text = text ?? string.Empty;
    }

    public void SetGuideEnable(ActionType[] types)
    {
        Initialize();
        foreach (var pair in lookup)
            foreach (var guide in pair.Value)
                guide.SetActive(types == null || Array.IndexOf(types, pair.Key) >= 0);
    }
}
