using UnityEngine;

/// <summary>
/// Owns the coin economy:
///  - falling coins caught by the gloves add coins one at a time
///  - score milestones pay out escalating coin bundles
/// It only talks through GameEventManager and persists the balance in
/// PlayerPrefs, so coins survive replays, scene changes and app restarts.
/// </summary>
public class RewardManager : MonoBehaviour
{
    [Header("Coin Pickups")]
    [Tooltip("Coins granted for catching one falling coin.")]
    [SerializeField] private int coinsPerPickup = 1;

    [Header("Score Milestones")]
    [Tooltip("First score that pays out a bundle.")]
    [SerializeField] private int firstMilestoneScore = 20;

    [Tooltip("Score distance between two milestones.")]
    [SerializeField] private int milestoneScoreStep = 20;

    [Header("Milestone Rewards")]
    [Tooltip("Coins awarded at the first milestone (score 20 -> 10).")]
    [SerializeField] private int firstMilestoneReward = 10;

    [Tooltip("Extra coins added to each following milestone. Doubles every time: 20, 40, 80, 160...")]
    [SerializeField] private int firstRewardIncrement = 20;

    [Tooltip("Safety cap for the doubling increment so it can never overflow.")]
    [SerializeField] private int maxRewardIncrement = 1000000;

    [Header("Persistence")]
    [SerializeField] private string coinsPlayerPrefsKey = "TotalCoins";

    [Tooltip("Write to PlayerPrefs on every change. If off, only saved on disable.")]
    [SerializeField] private bool saveEveryChange = true;

    private int totalCoins;

    // Milestone state for the current run
    private int nextMilestoneScore;
    private int currentMilestoneReward;
    private int rewardIncrement;

    private void OnEnable()
    {
        GameEventManager.OnItemCaught += HandleItemCaught;
        GameEventManager.OnScoreChanged += HandleScoreChanged;
    }

    private void OnDisable()
    {
        GameEventManager.OnItemCaught -= HandleItemCaught;
        GameEventManager.OnScoreChanged -= HandleScoreChanged;

        if (!saveEveryChange) SaveCoins();
    }

    private void Start()
    {
        totalCoins = PlayerPrefs.GetInt(coinsPlayerPrefsKey, 0);

        ResetMilestones();

        // Repaint anything that loaded before us
        PublishCoins();
    }

    private void ResetMilestones()
    {
        nextMilestoneScore = firstMilestoneScore;
        currentMilestoneReward = firstMilestoneReward;
        rewardIncrement = firstRewardIncrement;
    }

    private void HandleItemCaught(FallingObject item)
    {
        if (item.itemType != FallingObject.ObjectType.Coin) return;

        AddCoins(coinsPerPickup);
    }

    private void HandleScoreChanged(int score)
    {
        if (score <= 0 || milestoneScoreStep <= 0) return;

        // 'while' instead of 'if' so a big score jump still pays every milestone
        while (score >= nextMilestoneScore)
        {
            AddCoins(currentMilestoneReward);

            // 10, 30, 70, 150, 310... (increments double: 20, 40, 80, 160...)
            nextMilestoneScore += milestoneScoreStep;
            currentMilestoneReward += rewardIncrement;
            rewardIncrement = Mathf.Min(rewardIncrement * 2, maxRewardIncrement);
        }
    }

    private void AddCoins(int amount)
    {
        if (amount <= 0) return;

        totalCoins += amount;

        // UI listens for this - nothing here touches UI directly
        PublishCoins();

        if (saveEveryChange) SaveCoins();
    }

    private void PublishCoins()
    {
        GameEventManager.TriggerCoinsChanged(totalCoins);
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(coinsPlayerPrefsKey, totalCoins);
        PlayerPrefs.Save();
    }
}
