using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Appliance : MonoBehaviour, IInteractable
{
    public Canvas Panel;
    public float animationTime;

    public void OpenPanel(bool show) //Examine UI Animation
    {
        LeanTween.cancel(Panel.gameObject); // reset animation

        if (show)
        {
            Panel.transform.LeanScale(new(0, 0, 0), 0).setOnComplete(() =>
            {
                Panel.gameObject.SetActive(true);
                Panel.transform.LeanScale(new(1, 1, 1), animationTime).setEaseOutQuint();
            });
        }
        else
        {
            Panel.transform.LeanScale(new(0, 0, 0), animationTime).setEaseOutQuint().setOnComplete(() =>
            {
                Panel.gameObject.SetActive(false);
            });
        }
    }




    public void Interact(PlayerControl player)
    {
        StartCoroutine(TestAppear(player));
    }

    IEnumerator TestAppear(PlayerControl player)
    {
        OpenPanel(true);
        player.enabled = false;
        yield return new WaitForSeconds(2);
        OpenPanel(false);
        player.enabled = true;
    }
}