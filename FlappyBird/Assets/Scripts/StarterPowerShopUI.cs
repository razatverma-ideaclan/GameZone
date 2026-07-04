using UnityEngine;
using UnityEngine.UI;

/// <summary>Drives the Shop screen: buy charges of Magnet/Boost/Double, each usable as a pre-run starter power.</summary>
public class StarterPowerShopUI : MonoBehaviour
{
    [System.Serializable]
    public class BuyRow
    {
        public Text countText;
        public Button buyButton;
    }

    public Text totalCoinsText;
    public BuyRow magnetRow;
    public BuyRow boostRow;
    public BuyRow doubleRow;
    public BuyRow hammerRow;

    private void Awake()
    {
        BindRow(magnetRow, GameManager.StarterPower.Magnet);
        BindRow(boostRow, GameManager.StarterPower.Boost);
        BindRow(doubleRow, GameManager.StarterPower.Double);
        BindRow(hammerRow, GameManager.StarterPower.Hammer);
    }

    private void BindRow(BuyRow row, GameManager.StarterPower type)
    {
        if (row.buyButton == null) return;
        row.buyButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.BuyStarterCharge(type)) Refresh();
        });
    }

    public void Refresh()
    {
        if (GameManager.Instance == null) return;

        if (totalCoinsText != null) totalCoinsText.text = GameManager.Instance.TotalCoins.ToString();

        RefreshRow(magnetRow, GameManager.Instance.StarterMagnetCount);
        RefreshRow(boostRow, GameManager.Instance.StarterBoostCount);
        RefreshRow(doubleRow, GameManager.Instance.StarterDoubleCount);
        RefreshRow(hammerRow, GameManager.Instance.StarterHammerCount);
    }

    private void RefreshRow(BuyRow row, int count)
    {
        if (row.countText != null) row.countText.text = "x" + count;
        if (row.buyButton != null) row.buyButton.interactable = GameManager.Instance.TotalCoins >= GameManager.StarterPurchaseCost;
    }
}
