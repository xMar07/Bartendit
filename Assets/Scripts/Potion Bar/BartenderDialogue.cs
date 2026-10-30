using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual-novel style dialogue bar for the bartender. Shows a welcome message
/// on entry and replaces it with a potion description + price when a potion is
/// clicked. Text is revealed character by character.
/// </summary>
public class BartenderDialogue : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Text above/on the dialogue bar that shows the bartender's name.")]
    [SerializeField] private TextMeshProUGUI nameLabel;

    [Tooltip("Main dialogue text. Uses TMP maxVisibleCharacters for the typewriter effect.")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Bartender")]
    [Tooltip("Editable name shown above the dialogue bar.")]
    [SerializeField] private string bartenderName = "BARTENDER";

    [Header("Messages")]
    [TextArea(2, 5)]
    [SerializeField] private string welcomeMessage = "Welcome!";

    [Tooltip("Format used for the potion heading. {0} is the potion name.")]
    [SerializeField] private string potionTitleFormat = "<b>{0}</b>";

    [Tooltip("Format used for the price line. {0} is the price in coins.")]
    [SerializeField] private string priceFormat = "<color=#FFD633>PRICE : {0} COINS</color>";

    [Header("Typewriter")]
    [Tooltip("Characters revealed per second. Set to 0 to show text instantly.")]
    [SerializeField] private float charactersPerSecond = 38f;

    [Header("Purchase / Add Buttons")]
    [SerializeField] private UnityEngine.UI.Button buyButton;
    [SerializeField] private UnityEngine.UI.Button addButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private TextMeshProUGUI addButtonText;
    [SerializeField] private TextMeshProUGUI addCountText;

    private PotionDefinition currentPotion;

    private Coroutine typingRoutine;

    private void OnEnable()
    {
        GameEventManager.OnPotionDescriptionRequested += ShowPotion;
        GameEventManager.OnPotionBought += OnPotionBoughtUpdate;
        GameEventManager.OnPotionAddedToMix += OnPotionAddedUpdate;
        GameEventManager.OnCoinsChanged += RefreshBuyState;
        WireButtons();
    }

    private void OnDisable()
    {
        GameEventManager.OnPotionDescriptionRequested -= ShowPotion;
        GameEventManager.OnPotionBought -= OnPotionBoughtUpdate;
        GameEventManager.OnPotionAddedToMix -= OnPotionAddedUpdate;
        GameEventManager.OnCoinsChanged -= RefreshBuyState;
        UnwireButtons();
        StopTyping();
    }

    private void Start()
    {
        ApplyBartenderName();
        ShowWelcome();
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    private void WireButtons()
    {
        if (buyButton != null) buyButton.onClick.RemoveAllListeners();
        if (addButton != null) addButton.onClick.RemoveAllListeners();
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
        if (addButton != null) addButton.onClick.AddListener(OnAddClicked);
    }

    private void UnwireButtons()
    {
        if (buyButton != null) buyButton.onClick.RemoveListener(OnBuyClicked);
        if (addButton != null) addButton.onClick.RemoveListener(OnAddClicked);
    }

    private void OnBuyClicked()
    {
        if (currentPotion == null) return;
        bool ok = GameEventManager.TryBuyPotion(currentPotion.potionName, currentPotion.price);
        RefreshDialogueButtons();
    }

    private void OnAddClicked()
    {
        if (currentPotion == null) return;
        GameEventManager.TryAddPotionToMix(currentPotion.potionName);
        RefreshDialogueButtons();
    }

    private void OnPotionBoughtUpdate(string name, int count)
    {
        RefreshDialogueButtons();
    }

    private void OnPotionAddedUpdate(string name, int remaining)
    {
        RefreshDialogueButtons();
    }

    private void RefreshBuyState(int coins)
    {
        RefreshDialogueButtons();
    }

    private void RefreshDialogueButtons()
    {
        if (currentPotion == null) return;
        int count = GameEventManager.GetPotionCount(currentPotion.potionName);
        bool canBuy = GameEventManager.PlayerCoins >= currentPotion.price;

        if (buyButton != null)
        {
            buyButton.interactable = canBuy;
            Image buyImg = buyButton.GetComponent<Image>();
            if (buyImg != null) buyImg.color = canBuy ? new Color(0.3f, 0.9f, 0.4f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            if (buyButtonText != null) buyButtonText.text = $"BUY ({currentPotion.price})";
        }

        if (addButton != null)
        {
            bool hasPotion = count > 0;
            addButton.interactable = hasPotion;
            Image addImg = addButton.GetComponent<Image>();
            if (addImg != null) addImg.color = hasPotion ? new Color(0.9f, 0.9f, 0.3f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            if (addButtonText != null) addButtonText.text = "ADD";
        }

        if (addCountText != null) addCountText.text = count.ToString();
    }

    public void SetBartenderName(string newName)
    {
        bartenderName = newName;
        ApplyBartenderName();
    }

    public void ShowWelcome()
    {
        currentPotion = null;
        PlayMessage(welcomeMessage);
        if (buyButton != null) buyButton.interactable = false;
        if (addButton != null) addButton.interactable = false;
        if (addCountText != null) addCountText.text = "0";
    }

    /// <summary>
    /// Types the selected potion's name, description and price.
    /// </summary>
    public void ShowPotion(PotionDefinition potion)
    {
        if (potion == null) return;
        currentPotion = potion;

        string title = string.Format(potionTitleFormat, potion.potionName);
        string description = string.IsNullOrWhiteSpace(potion.description)
            ? "No description yet."
            : potion.description;
        string price = string.Format(priceFormat, potion.price);

        PlayMessage($"{title}\n{description}\n{price}");
        RefreshDialogueButtons();
    }

    // ------------------------------------------------------------------
    // Rendering
    // ------------------------------------------------------------------

    private void ApplyBartenderName()
    {
        if (nameLabel != null)
        {
            nameLabel.text = bartenderName;
        }
    }

    private void PlayMessage(string message)
    {
        if (dialogueText == null) return;

        StopTyping();

        dialogueText.text = message;
        dialogueText.ForceMeshUpdate();

        int totalCharacters = dialogueText.textInfo.characterCount;

        if (!isActiveAndEnabled || charactersPerSecond <= 0f)
        {
            dialogueText.maxVisibleCharacters = totalCharacters;
            return;
        }

        dialogueText.maxVisibleCharacters = 0;
        typingRoutine = StartCoroutine(TypeMessage(totalCharacters));
    }

    private IEnumerator TypeMessage(int totalCharacters)
    {
        float revealed = 0f;

        // Unscaled time keeps the bartender talking even if a menu freezes time.
        while (revealed < totalCharacters)
        {
            revealed += charactersPerSecond * Time.unscaledDeltaTime;

            int visible = Mathf.Clamp(Mathf.FloorToInt(revealed), 0, totalCharacters);
            dialogueText.maxVisibleCharacters = visible;

            yield return null;
        }

        dialogueText.maxVisibleCharacters = totalCharacters;
        typingRoutine = null;
    }

    private void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
    }
}
