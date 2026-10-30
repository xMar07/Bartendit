using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles potion mixing UI. Shows sprites of potions added to the mix,
/// allows mixing, and displays the result placeholder.
/// Tied to GameEventManager events.
/// </summary>
public class PotionMixer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform mixContainer;
    [SerializeField] private Button mixButton;
    [SerializeField] private GameObject potionPlaceholderPrefab;
    [SerializeField] private GameObject resultPlaceholderPrefab;
    [SerializeField] private TextMeshProUGUI resultNameText;

    [Header("Result Potion")]
    [SerializeField] private string resultPotionName = "MYSTIC MIX";
    [SerializeField] private Sprite resultSprite; // placeholder

    private readonly List<string> addedPotions = new();
    private readonly List<Image> addedPlaceholders = new();

    private void OnEnable()
    {
        GameEventManager.OnPotionAddedToMix += OnPotionAddedToMix;
        GameEventManager.OnPotionMixed += OnPotionMixed;
        if (mixButton != null) mixButton.onClick.AddListener(OnMixClicked);
    }

    private void OnDisable()
    {
        GameEventManager.OnPotionAddedToMix -= OnPotionAddedToMix;
        GameEventManager.OnPotionMixed -= OnPotionMixed;
        if (mixButton != null) mixButton.onClick.RemoveListener(OnMixClicked);
    }

    private void Start()
    {
        ClearMixUI();
    }

    private void OnPotionAddedToMix(string potionName, int remaining)
    {
        addedPotions.Add(potionName);
        RefreshMixUI();
    }

    private void OnPotionMixed(string resultName)
    {
        ShowResult(resultName);
    }

    private void RefreshMixUI()
    {
        ClearPlaceholders();
        foreach (string name in addedPotions)
        {
            CreatePlaceholder(name);
        }
    }

    private void CreatePlaceholder(string potionName)
    {
        if (mixContainer == null || potionPlaceholderPrefab == null) return;
        GameObject go = Instantiate(potionPlaceholderPrefab, mixContainer);
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.sprite = null; // placeholder sprite; user can assign real sprites later
        TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null)
        {
            GameObject txt = new GameObject("Label", typeof(RectTransform));
            txt.transform.SetParent(go.transform, false);
            label = txt.AddComponent<TextMeshProUGUI>();
            label.fontSize = 14;
            label.alignment = TextAlignmentOptions.Center;
        }
        label.text = potionName;
        addedPlaceholders.Add(img);
    }

    private void ClearPlaceholders()
    {
        foreach (var img in addedPlaceholders)
        {
            if (img != null && img.gameObject != null)
                Destroy(img.gameObject);
        }
        addedPlaceholders.Clear();
    }

    private void ClearMixUI()
    {
        addedPotions.Clear();
        ClearPlaceholders();
        if (mixButton != null) mixButton.interactable = false;
    }

    private void OnMixClicked()
    {
        if (addedPotions.Count == 0) return;
        // Use configured result name; user will mention later
        GameEventManager.TriggerPotionMixed(resultPotionName);
        // After mixing, clear mix
        ClearMixUI();
        // Also show result placeholder
        ShowResult(resultPotionName);
    }

    private void ShowResult(string name)
    {
        if (resultPlaceholderPrefab != null && mixContainer != null)
        {
            GameObject result = Instantiate(resultPlaceholderPrefab, mixContainer);
            TextMeshProUGUI label = result.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = name;
            else if (resultNameText != null) resultNameText.text = name;
        }
    }
}
