using UnityEngine;
using UnityEngine.UI;

public class BackOnCancel : MonoBehaviour
{
    public Button backButton;

    private static int handledFrame = -1;

    private void Update()
    {
        if (backButton == null || !backButton.isActiveAndEnabled || !backButton.interactable || handledFrame == Time.frameCount)
            return;

        if (KeybindManager.Instance != null && KeybindManager.Instance.IsCapturingInput)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1) || UICancelPressed())
        {
            handledFrame = Time.frameCount;
            backButton.onClick.Invoke();
        }
    }

    private static bool UICancelPressed()
    {
        var module = UnityEngine.EventSystems.EventSystem.current != null
            ? UnityEngine.EventSystems.EventSystem.current.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule
            : null;
        return module != null && module.cancel != null && module.cancel.action != null && module.cancel.action.WasPressedThisFrame();
    }
}
