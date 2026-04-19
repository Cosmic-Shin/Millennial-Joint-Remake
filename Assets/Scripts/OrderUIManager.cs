using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderUIManager : MonoBehaviour
{
    public static OrderUIManager Instance { get; private set; }

    public TextMeshProUGUI orderText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI quotaText;
    public TextMeshProUGUI statusText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Removed call to UpdateDisplay here, as UpdateGameUI will handle it
    }

    public void UpdateDisplay(Order order)
    {
        if (orderText == null)
        {
            Debug.LogWarning("OrderUIManager: orderText not assigned!");
            return;
        }

        orderText.text = order.GetDisplayText();
        UpdateMoneyDisplay();
    }

    public void UpdateGameUI()
    {
        if (OrderManager.Instance == null) return;

        if (timerText != null)
            timerText.text = "" + Mathf.Ceil(OrderManager.Instance.GetCurrentTime()).ToString() + "s";

        if (quotaText != null)
            quotaText.text = "Goal: $" + OrderManager.Instance.moneyQuota.ToString("0.00");

        if (statusText != null)
            statusText.text = OrderManager.Instance.state.ToString();

        UpdateMoneyDisplay();
    }

    public void UpdateMoneyDisplay()
    {
        if (moneyText == null)
        {
            Debug.LogWarning("OrderUIManager: moneyText not assigned!");
            return;
        }

        if (OrderManager.Instance != null)
        {
            moneyText.text = "$" + OrderManager.Instance.money.ToString("0.00");
        }
        else
        {
            moneyText.text = "$0.00";
        }
    }
}
