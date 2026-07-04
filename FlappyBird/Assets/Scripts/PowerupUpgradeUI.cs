using UnityEngine;
using UnityEngine.UI;

public class PowerupUpgradeUI : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeRow
    {
        public Text levelText;
        public Text segmentsText;
        public Text durationText;
        public Text costText;
        public Button upgradeButton;
    }

    public Text totalCoinsText;
    public UpgradeRow magnetRow;
    public UpgradeRow boostRow;
    public UpgradeRow doubleRow;

    private void Awake()
    {
        BindRow(magnetRow, () => GameManager.Instance.UpgradeMagnet());
        BindRow(boostRow, () => GameManager.Instance.UpgradeBoost());
        BindRow(doubleRow, () => GameManager.Instance.UpgradeDouble());
    }

    private void BindRow(UpgradeRow row, System.Func<bool> upgradeAction)
    {
        if (row.upgradeButton == null) return;
        row.upgradeButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance == null) return;
            if (upgradeAction())
            {
                GameManager.Instance.PlayClickSound();
                Refresh();
            }
        });
    }

    public void Refresh()
    {
        if (GameManager.Instance == null) return;

        if (totalCoinsText != null) totalCoinsText.text = GameManager.Instance.TotalCoins.ToString();

        RefreshRow(magnetRow, GameManager.Instance.MagnetLevel);
        RefreshRow(boostRow, GameManager.Instance.BoostLevel);
        RefreshRow(doubleRow, GameManager.Instance.DoubleLevel);
    }

    private void RefreshRow(UpgradeRow row, int level)
    {
        if (row.levelText != null) row.levelText.text = "LEVEL " + level + "/5";
        if (row.segmentsText != null) row.segmentsText.text = BuildSegments(level);
        if (row.durationText != null) row.durationText.text = GameManager.GetPowerupDuration(level).ToString("F1") + "s";

        int cost = GameManager.Instance.GetUpgradeCost(level);
        bool isMax = cost < 0;
        if (row.costText != null) row.costText.text = isMax ? "MAX" : cost + " COINS";
        if (row.upgradeButton != null) row.upgradeButton.interactable = !isMax && GameManager.Instance.TotalCoins >= cost;
    }

    private string BuildSegments(int level)
    {
        string s = "";
        for (int i = 0; i < 5; i++)
        {
            s += (i < level ? "■" : "□");
            if (i < 4) s += " ";
        }
        return s;
    }
}
