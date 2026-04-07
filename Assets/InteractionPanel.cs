using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPanel : MonoBehaviour
{
    public Canvas panelCanvas;
    public Text messageText;
    public float showDuration = 1.2f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (panelCanvas != null)
            panelCanvas.gameObject.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (panelCanvas != null)
            panelCanvas.gameObject.SetActive(true);

        yield return new WaitForSeconds(showDuration);

        if (panelCanvas != null)
            panelCanvas.gameObject.SetActive(false);

        currentRoutine = null;
    }
}