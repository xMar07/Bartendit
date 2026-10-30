using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the placeholder Potion Bar UI (background, shelves, six potion
/// slots, mixed-product slot, bartender placeholder and dialogue bar).
/// Temporary editor utility - safe to delete after the scene is saved.
/// </summary>
public static class PotionBarBuilder
{
    private const string ScenePath = "Assets/Scenes/PotionBar.unity";

    private static Sprite roundedSprite;

    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("PotionBarBuilder: no Canvas found in PotionBar.");
            return;
        }

        RemoveOldContent(canvas.transform);

        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        BuildBackground(canvas.transform);
        GameObject content = BuildContent(canvas.transform);

        PotionBarManager manager = content.AddComponent<PotionBarManager>();
        BartenderDialogue dialogue = content.AddComponent<BartenderDialogue>();

        BuildTitle(content.transform);

        TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[6];
        Image[] slotFrames = new Image[6];
        Outline[] slotOutlines = new Outline[6];
        EventTrigger[] slotTriggers = new EventTrigger[6];
        RectTransform[] slotRects = new RectTransform[6];

        BuildShelvesAndSlots(content.transform, slotLabels, slotFrames, slotOutlines, slotTriggers, slotRects);
        BuildMixedProduct(content.transform);
        BuildBartender(content.transform);
        BuildDialogueBar(content.transform, dialogue);

        WireManager(manager, slotLabels, slotFrames, slotOutlines, slotTriggers, slotRects);
        WireDialogue(dialogue);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = content;
        Debug.Log("PotionBarBuilder: Potion Bar placeholders created and scene saved.");
    }

    // ------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------

    private static void BuildBackground(Transform canvas)
    {
        GameObject background = CreateUI("PotionBarBackground", canvas);
        Image image = background.AddComponent<Image>();
        image.color = Hex("#140E1B");
        image.raycastTarget = false;
        Stretch(background.GetComponent<RectTransform>());
        background.transform.SetSiblingIndex(0);

        // Soft centre glow behind the bartender.
        GameObject glow = CreateUI("BarGlow", background.transform);
        Image glowImage = glow.AddComponent<Image>();
        glowImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        glowImage.color = new Color(0.36f, 0.22f, 0.5f, 0.18f);
        glowImage.raycastTarget = false;
        SetRect(glow.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 640f), new Vector2(720f, 620f));

        // Thin warm floor line above the dialogue bar.
        GameObject floor = CreateUI("BarFloorLine", background.transform);
        Image floorImage = floor.AddComponent<Image>();
        floorImage.color = new Color(0.46f, 0.33f, 0.2f, 0.7f);
        floorImage.raycastTarget = false;
        SetRect(floor.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 335f), new Vector2(-180f, 4f));
    }

    private static GameObject BuildContent(Transform canvas)
    {
        GameObject content = CreateUI("PotionBarContent", canvas);
        Stretch(content.GetComponent<RectTransform>());
        content.transform.SetSiblingIndex(2);
        return content;
    }

    private static void BuildTitle(Transform parent)
    {
        TextMeshProUGUI title = CreateText(parent, "SceneTitle", "POTION BAR", 50f,
            Hex("#E8C777"), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(900f, 72f));

        TextMeshProUGUI hint = CreateText(parent, "SceneHint",
            "Hover a potion to inspect it  -  click to hear the bartender",
            22f, new Color(0.74f, 0.68f, 0.8f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);
        SetRect(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(900f, 34f));
    }

    private static void BuildShelvesAndSlots(Transform parent, TextMeshProUGUI[] labels,
        Image[] frames, Outline[] outlines, EventTrigger[] triggers, RectTransform[] rects)
    {
        for (int i = 0; i < 6; i++)
        {
            bool leftSide = i < 3;
            int row = i % 3;

            float x = leftSide ? -645f : 645f;
            float y = 500f + row * 180f;
            string sideName = leftSide ? "Left" : "Right";

            GameObject plank = CreateUI($"Shelf_{sideName}_{row + 1}", parent);
            Image plankImage = plank.AddComponent<Image>();
            plankImage.sprite = roundedSprite;
            plankImage.type = Image.Type.Sliced;
            plankImage.color = Hex("#3B2519");
            plankImage.raycastTarget = false;
            SetRect(plank.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(x, y - 86f), new Vector2(430f, 18f));

            GameObject slot = CreateUI($"PotionSlot_{i + 1}", parent);
            Image frame = slot.AddComponent<Image>();
            frame.sprite = roundedSprite;
            frame.type = Image.Type.Sliced;
            frame.color = Hex("#3D314C");
            frame.raycastTarget = true;
            SetRect(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(242f, 152f));

            Outline outline = slot.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(3f, -3f);

            EventTrigger trigger = slot.AddComponent<EventTrigger>();
            trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

            BuildBottleIcon(slot.transform);

            TextMeshProUGUI label = CreateText(slot.transform, "Name", $"POTION {i + 1}", 24f,
                Hex("#F3EAF8"), TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-28f, 34f));

            labels[i] = label;
            frames[i] = frame;
            outlines[i] = outline;
            triggers[i] = trigger;
            rects[i] = slot.GetComponent<RectTransform>();
        }
    }

    private static void BuildBottleIcon(Transform slot)
    {
        GameObject bottle = CreateUI("BottlePlaceholder", slot);
        Image bottleImage = bottle.AddComponent<Image>();
        bottleImage.sprite = roundedSprite;
        bottleImage.type = Image.Type.Sliced;
        bottleImage.color = new Color(0.57f, 0.45f, 0.72f, 0.85f);
        bottleImage.raycastTarget = false;
        SetRect(bottle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(82f, 68f));

        GameObject neck = CreateUI("BottleNeck", slot);
        Image neckImage = neck.AddComponent<Image>();
        neckImage.sprite = roundedSprite;
        neckImage.type = Image.Type.Sliced;
        neckImage.color = new Color(0.68f, 0.55f, 0.82f, 0.85f);
        neckImage.raycastTarget = false;
        SetRect(neck.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(30f, 28f));

        GameObject cork = CreateUI("BottleCork", slot);
        Image corkImage = cork.AddComponent<Image>();
        corkImage.sprite = roundedSprite;
        corkImage.type = Image.Type.Sliced;
        corkImage.color = new Color(0.8f, 0.64f, 0.36f, 1f);
        corkImage.raycastTarget = false;
        SetRect(cork.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 84f), new Vector2(32f, 12f));
    }

    private static void BuildMixedProduct(Transform parent)
    {
        GameObject slot = CreateUI("MixedProductSlot", parent);
        Image frame = slot.AddComponent<Image>();
        frame.sprite = roundedSprite;
        frame.type = Image.Type.Sliced;
        frame.color = Hex("#24333A");
        frame.raycastTarget = false;
        SetRect(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 435f), new Vector2(270f, 180f));

        Outline outline = slot.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.78f, 0.72f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        TextMeshProUGUI mark = CreateText(slot.transform, "Mark", "?", 64f,
            new Color(0.45f, 0.85f, 0.78f, 0.85f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(mark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(120f, 84f));

        TextMeshProUGUI label = CreateText(slot.transform, "Label", "MIXED PRODUCT", 22f,
            Hex("#E7F6F3"), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(-20f, 28f));

        TextMeshProUGUI note = CreateText(slot.transform, "Note", "coming soon", 17f,
            new Color(0.58f, 0.72f, 0.7f, 0.9f), TextAlignmentOptions.Center, FontStyles.Italic);
        SetRect(note.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(-20f, 24f));
    }

    private static void BuildBartender(Transform parent)
    {
        GameObject root = CreateUI("BartenderPlaceholder", parent);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 790f), new Vector2(340f, 400f));

        GameObject body = CreateUI("Body", root.transform);
        Image bodyImage = body.AddComponent<Image>();
        bodyImage.sprite = roundedSprite;
        bodyImage.type = Image.Type.Sliced;
        bodyImage.color = Hex("#4B3760");
        bodyImage.raycastTarget = false;
        SetRect(body.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(250f, 250f));

        GameObject apron = CreateUI("Apron", root.transform);
        Image apronImage = apron.AddComponent<Image>();
        apronImage.sprite = roundedSprite;
        apronImage.type = Image.Type.Sliced;
        apronImage.color = Hex("#2F213D");
        apronImage.raycastTarget = false;
        SetRect(apron.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(170f, 190f));

        GameObject head = CreateUI("Head", root.transform);
        Image headImage = head.AddComponent<Image>();
        headImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        headImage.color = Hex("#8A6C9D");
        headImage.raycastTarget = false;
        SetRect(head.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 255f), new Vector2(110f, 110f));

        TextMeshProUGUI label = CreateText(root.transform, "Label", "BARTENDER\n(placeholder)", 24f,
            Hex("#E8DCF2"), TextAlignmentOptions.Center, FontStyles.Bold);
        SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 135f), new Vector2(-20f, 76f));
    }

    private static void BuildDialogueBar(Transform parent, BartenderDialogue dialogue)
    {
        GameObject bar = CreateUI("BartenderDialogueBar", parent);
        Image barImage = bar.AddComponent<Image>();
        barImage.sprite = roundedSprite;
        barImage.type = Image.Type.Sliced;
        barImage.color = Hex("#2A1D36");
        barImage.raycastTarget = false;
        SetRect(bar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(-160f, 280f));

        Outline outline = bar.AddComponent<Outline>();
        outline.effectColor = new Color(0.62f, 0.47f, 0.25f, 0.85f);
        outline.effectDistance = new Vector2(3f, -3f);

        GameObject nameplate = CreateUI("Nameplate", bar.transform);
        Image nameplateImage = nameplate.AddComponent<Image>();
        nameplateImage.sprite = roundedSprite;
        nameplateImage.type = Image.Type.Sliced;
        nameplateImage.color = Hex("#7B5B25");
        nameplateImage.raycastTarget = false;
        SetRect(nameplate.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(46f, 10f), new Vector2(470f, 70f));

        TextMeshProUGUI nameLabel = CreateText(nameplate.transform, "BartenderName", "BARTENDER", 34f,
            Hex("#FFF3D0"), TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(nameLabel.rectTransform, 24f, 24f, 8f, 8f);

        TextMeshProUGUI dialogueText = CreateText(bar.transform, "DialogueText",
            "Welcome!", 34f, Hex("#F4ECFA"), TextAlignmentOptions.TopLeft, FontStyles.Normal);

        SetRect(dialogueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dialogueText.rectTransform.offsetMin = new Vector2(62f, 30f);
        dialogueText.rectTransform.offsetMax = new Vector2(-62f, -32f);
        dialogueText.lineSpacing = 12f;
        dialogueText.overflowMode = TextOverflowModes.Overflow;

        TextMeshProUGUI hint = CreateText(bar.transform, "DialogueHint",
            "click a potion to ask about it", 18f,
            new Color(0.7f, 0.62f, 0.78f, 0.75f), TextAlignmentOptions.BottomRight, FontStyles.Italic);
        SetRect(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(-34f, 18f), new Vector2(420f, 26f));

        // References are wired below by serialized name.
        nameLabel.gameObject.name = "BartenderName";
    }

    // ------------------------------------------------------------------
    // Wiring
    // ------------------------------------------------------------------

    private static void WireManager(PotionBarManager manager, TextMeshProUGUI[] labels,
        Image[] frames, Outline[] outlines, EventTrigger[] triggers, RectTransform[] rects)
    {
        SerializedObject so = new SerializedObject(manager);

        SerializedProperty potions = so.FindProperty("potions");
        potions.arraySize = 6;

        string[] descriptions =
        {
            "Test description for potion 1. Replace this text with the real potion description later.",
            "Test description for potion 2. Replace this text with the real potion description later.",
            "Test description for potion 3. Replace this text with the real potion description later.",
            "Test description for potion 4. Replace this text with the real potion description later.",
            "Test description for potion 5. Replace this text with the real potion description later.",
            "Test description for potion 6. Replace this text with the real potion description later."
        };

        for (int i = 0; i < 6; i++)
        {
            SerializedProperty potion = potions.GetArrayElementAtIndex(i);
            potion.FindPropertyRelative("potionName").stringValue = $"POTION {i + 1}";
            potion.FindPropertyRelative("description").stringValue = descriptions[i];
            potion.FindPropertyRelative("price").intValue = (i + 1) * 10;
            potion.FindPropertyRelative("effectNotes").stringValue = "Effect placeholder.";
        }

        SerializedProperty slots = so.FindProperty("slots");
        slots.arraySize = 6;

        for (int i = 0; i < 6; i++)
        {
            SerializedProperty slot = slots.GetArrayElementAtIndex(i);
            slot.FindPropertyRelative("trigger").objectReferenceValue = triggers[i];
            slot.FindPropertyRelative("frame").objectReferenceValue = frames[i];
            slot.FindPropertyRelative("outline").objectReferenceValue = outlines[i];
            slot.FindPropertyRelative("scaleTarget").objectReferenceValue = rects[i];
            slot.FindPropertyRelative("nameLabel").objectReferenceValue = labels[i];
        }

        so.FindProperty("normalColor").colorValue = Hex("#3D314C");
        so.FindProperty("hoverColor").colorValue = Hex("#6B4C77");
        so.FindProperty("selectedColor").colorValue = Hex("#B7812D");
        so.FindProperty("normalOutlineColor").colorValue = new Color(0f, 0f, 0f, 0.55f);
        so.FindProperty("highlightOutlineColor").colorValue = Hex("#FFD166");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireDialogue(BartenderDialogue dialogue)
    {
        Transform content = dialogue.transform;
        TextMeshProUGUI nameLabel = content.Find("BartenderDialogueBar/Nameplate/BartenderName")
            .GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI dialogueText = content.Find("BartenderDialogueBar/DialogueText")
            .GetComponent<TextMeshProUGUI>();

        SerializedObject so = new SerializedObject(dialogue);
        so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
        so.FindProperty("dialogueText").objectReferenceValue = dialogueText;
        so.FindProperty("bartenderName").stringValue = "BARTENDER";
        so.FindProperty("welcomeMessage").stringValue = "Welcome!";
        so.FindProperty("charactersPerSecond").floatValue = 38f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static void RemoveOldContent(Transform canvas)
    {
        string[] oldNames = { "PotionBarBackground", "PotionBarContent" };

        foreach (string oldName in oldNames)
        {
            Transform old = canvas.Find(oldName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }
        }
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content,
        float fontSize, Color color, TextAlignmentOptions alignment, FontStyles style)
    {
        GameObject go = CreateUI(name, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        if (font != null)
        {
            text.font = font;
        }

        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect, float left = 0f, float right = 0f,
        float bottom = 0f, float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }

    private static Color Hex(string hex)
    {
        return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;
    }
}