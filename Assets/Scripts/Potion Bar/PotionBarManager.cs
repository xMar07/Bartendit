using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Data for one potion on the bar. Fill these in from the Inspector - the
/// mixing rules and real effects are intentionally not implemented yet.
/// </summary>
[Serializable]
public class PotionDefinition
{
    [Tooltip("Name shown on the shelf label and in the bartender dialogue.")]
    public string potionName = "POTION";

    [TextArea(3, 8)]
    [Tooltip("Description the bartender says when this potion is clicked.")]
    public string description = "Test description. Replace this text later.";

    [Min(0)]
    [Tooltip("Price in coins. Only displayed for now - no purchase logic yet.")]
    public int price = 10;

    [TextArea(1, 4)]
    [Tooltip("Placeholder for the future potion effect / mixing notes.")]
    public string effectNotes = "";
}

/// <summary>
/// Serialized references for one potion placeholder on the shelf.
/// </summary>
[Serializable]
public class PotionSlotView
{
    [Tooltip("EventTrigger that receives pointer enter / exit / click. Added automatically if left empty.")]
    public EventTrigger trigger;

    [Tooltip("Background image that changes colour when hovered or selected.")]
    public Image frame;

    [Tooltip("Outline used to highlight the slot. Optional.")]
    public Outline outline;

    [Tooltip("Transform that grows slightly on hover / selection. Optional.")]
    public RectTransform scaleTarget;

    [Tooltip("Optional label on the shelf showing the potion name.")]
    public TextMeshProUGUI nameLabel;
}

/// <summary>
/// Owns the six potion placeholders, their hover/selected feedback, and the
/// events that tell the bartender what to say. Visuals are plain Unity UI so
/// they can be replaced with real art later.
/// </summary>
public class PotionBarManager : MonoBehaviour
{
    [Header("Potion Data")]
    [Tooltip("Six test entries. Replace names, descriptions and prices later.")]
    [SerializeField] private PotionDefinition[] potions = new PotionDefinition[0];

    [Header("Slots")]
    [Tooltip("Left slots are usually indices 0-2 (bottom to top) and right slots 3-5.")]
    [SerializeField] private PotionSlotView[] slots = new PotionSlotView[0];

    [Header("Shelf Labels")]
    [Tooltip("Copy each potion name onto its shelf label at startup.")]
    [SerializeField] private bool applyPotionNamesToSlots = true;

    [Header("Slot Colours")]
    [SerializeField] private Color normalColor = new Color(0.82f, 0.82f, 0.88f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.89f, 0.48f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.7f, 0.2f, 1f);

    [Header("Slot Outline")]
    [SerializeField] private Color normalOutlineColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color highlightOutlineColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Vector2 outlineDistance = new Vector2(3f, -3f);

    [Header("Slot Scale")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float selectedScale = 1.1f;
    [Tooltip("How quickly a slot grows/shrinks. 0 snaps instantly.")]
    [SerializeField] private float scaleLerpSpeed = 14f;

    private readonly List<Vector3> baseScales = new List<Vector3>();
    private int hoveredIndex = -1;
    private int selectedIndex = -1;

    public int SelectedIndex => selectedIndex;

    public PotionDefinition SelectedPotion =>
        IsValidPotionIndex(selectedIndex) ? potions[selectedIndex] : null;

    private void Awake()
    {
        CacheBaseScales();
        WireAllSlots();
    }

    private void Start()
    {
        ApplyPotionNames();
        RefreshAllSlotVisuals();
    }

    private void Update()
    {
        if (scaleLerpSpeed <= 0f) return;

        float t = 1f - Mathf.Exp(-scaleLerpSpeed * Time.unscaledDeltaTime);

        for (int i = 0; i < slots.Length; i++)
        {
            PotionSlotView slot = slots[i];
            if (slot == null || slot.scaleTarget == null) continue;

            Vector3 target = baseScales[i] * GetTargetScale(i);
            slot.scaleTarget.localScale = Vector3.Lerp(slot.scaleTarget.localScale, target, t);
        }
    }

    // ------------------------------------------------------------------
    // Setup
    // ------------------------------------------------------------------

    private void CacheBaseScales()
    {
        baseScales.Clear();

        for (int i = 0; i < slots.Length; i++)
        {
            PotionSlotView slot = slots[i];
            baseScales.Add(slot != null && slot.scaleTarget != null
                ? slot.scaleTarget.localScale
                : Vector3.one);
        }
    }

    private void WireAllSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            PotionSlotView slot = slots[i];
            if (slot == null || slot.frame == null) continue;

            EventTrigger trigger = slot.trigger;

            if (trigger == null)
            {
                trigger = slot.frame.GetComponent<EventTrigger>();
            }

            if (trigger == null)
            {
                trigger = slot.frame.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers ??= new List<EventTrigger.Entry>();
            slot.trigger = trigger;

            int index = i;
            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => HandlePointerEnter(index));
            AddTrigger(trigger, EventTriggerType.PointerExit, _ => HandlePointerExit(index));
            AddTrigger(trigger, EventTriggerType.PointerClick, _ => HandlePointerClick(index));
        }
    }

    private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = type,
            callback = new EventTrigger.TriggerEvent()
        };

        entry.callback.AddListener(data => callback(data));
        trigger.triggers.Add(entry);
    }

    private void ApplyPotionNames()
    {
        if (!applyPotionNamesToSlots) return;

        for (int i = 0; i < slots.Length; i++)
        {
            PotionSlotView slot = slots[i];
            if (slot == null || slot.nameLabel == null) continue;

            if (IsValidPotionIndex(i))
            {
                slot.nameLabel.text = potions[i].potionName;
            }
        }
    }

    // ------------------------------------------------------------------
    // Pointer handling
    // ------------------------------------------------------------------

    private void HandlePointerEnter(int index)
    {
        hoveredIndex = index;
        RefreshSlotVisual(index);

        GameEventManager.TriggerPotionHovered(index);
    }

    private void HandlePointerExit(int index)
    {
        if (hoveredIndex == index)
        {
            hoveredIndex = -1;
        }

        RefreshSlotVisual(index);

        GameEventManager.TriggerPotionUnhovered(index);
    }

    private void HandlePointerClick(int index)
    {
        if (!IsValidPotionIndex(index)) return;

        selectedIndex = index;

        RefreshAllSlotVisuals();

        GameEventManager.TriggerPotionSelected(index);
        GameEventManager.TriggerPotionDescriptionRequested(potions[index]);
    }

    /// <summary>
    /// Selects a slot from code (keyboard navigation, tutorials, etc.).
    /// </summary>
    public void SelectPotion(int index)
    {
        HandlePointerClick(index);
    }

    // ------------------------------------------------------------------
    // Visuals
    // ------------------------------------------------------------------

    private void RefreshAllSlotVisuals()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            RefreshSlotVisual(i);
        }
    }

    private void RefreshSlotVisual(int index)
    {
        if (index < 0 || index >= slots.Length) return;

        PotionSlotView slot = slots[index];
        if (slot == null) return;

        Color frameColor = GetTargetColor(index);

        if (slot.frame != null)
        {
            slot.frame.color = frameColor;
        }

        if (slot.outline != null)
        {
            bool highlighted = index == hoveredIndex || index == selectedIndex;
            slot.outline.effectColor = highlighted ? highlightOutlineColor : normalOutlineColor;
            slot.outline.effectDistance = outlineDistance;
        }

        if (slot.scaleTarget != null && scaleLerpSpeed <= 0f)
        {
            slot.scaleTarget.localScale = baseScales[index] * GetTargetScale(index);
        }
    }

    private Color GetTargetColor(int index)
    {
        if (index == selectedIndex) return selectedColor;
        if (index == hoveredIndex) return hoverColor;
        return normalColor;
    }

    private float GetTargetScale(int index)
    {
        if (index == selectedIndex) return selectedScale;
        if (index == hoveredIndex) return hoverScale;
        return 1f;
    }

    private bool IsValidPotionIndex(int index)
    {
        return potions != null && index >= 0 && index < potions.Length && potions[index] != null;
    }
}
