using System;
using UnityEngine;

public class GameEventManager : MonoBehaviour
{
    public static event Action<FallingObject> OnItemCaught;
    public static event Action<FallingObject> OnGoodItemMissed; // Added this

    /// <summary>Raised whenever the run score changes (0 at the start of a run).</summary>
    public static event Action<int> OnScoreChanged;

    /// <summary>Raised when the stored best score changes (also on load).</summary>
    public static event Action<int> OnBestScoreChanged;

    /// <summary>Raised the moment the current run beats the stored best score.</summary>
    public static event Action<int> OnNewRecordAchieved;

    /// <summary>Raised whenever health changes, and once at the start of a run.</summary>
    public static event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)

    /// <summary>Raised when the player runs out of health. Listeners handle freeze/UI.</summary>
    public static event Action OnGameOver;

    /// <summary>Raised whenever the difficulty changes the falling speed multiplier.</summary>
    public static event Action<float> OnSpeedMultiplierChanged;

    /// <summary>Raised whenever the player's total coin balance changes.</summary>
    public static event Action<int> OnCoinsChanged;

    /// <summary>
    /// Current global falling speed multiplier. Newly spawned items read this in Awake
    /// so they start at the difficulty the player has already reached.
    /// </summary>
    public static float CurrentSpeedMultiplier { get; private set; } = 1f;

    /// <summary>
    /// Last published coin balance. Handy for scenes/UI that appear after the
    /// reward manager has already loaded the value from PlayerPrefs.
    /// </summary>
    public static int CurrentCoins { get; private set; }

    public static void TriggerItemCaught(FallingObject caughtItem)
    {
        OnItemCaught?.Invoke(caughtItem);
    }

    public static void TriggerGoodItemMissed(FallingObject missedItem)
    {
        OnGoodItemMissed?.Invoke(missedItem);
    }

    public static void TriggerScoreChanged(int currentScore)
    {
        OnScoreChanged?.Invoke(currentScore);
    }

    public static void TriggerBestScoreChanged(int bestScore)
    {
        OnBestScoreChanged?.Invoke(bestScore);
    }

    public static void TriggerNewRecordAchieved(int bestScore)
    {
        OnNewRecordAchieved?.Invoke(bestScore);
    }

    public static void TriggerHealthChanged(int currentHealth, int maxHealth)
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public static void TriggerGameOver()
    {
        OnGameOver?.Invoke();
    }

    public static void TriggerSpeedMultiplierChanged(float multiplier)
    {
        CurrentSpeedMultiplier = multiplier;
        OnSpeedMultiplierChanged?.Invoke(multiplier);
    }

    public static void TriggerCoinsChanged(int totalCoins)
    {
        CurrentCoins = totalCoins;
        OnCoinsChanged?.Invoke(totalCoins);
    }
}
