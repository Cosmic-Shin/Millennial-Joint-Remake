using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderUIManager : MonoBehaviour
{
    public static OrderUIManager Instance { get; private set; }

    public TextMeshProUGUI orderText;
    public TextMeshProUGUI moneyText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

    public void UpdateMoneyDisplay()
    {
        if (moneyText == null)
        {
            Debug.LogWarning("OrderUIManager: moneyText not assigned!");
            return;
        }

        if (OrderManager.Instance != null)
        {
            moneyText.text = "Money: $" + OrderManager.Instance.money.ToString("0.00");
        }
        else
        {
            moneyText.text = "Money: $0.00";
        }
    }
}
