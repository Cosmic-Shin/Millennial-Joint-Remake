using System.Collections;
using UnityEngine;

//so this script is basically useless but best not to remove it just yet in case we want to add some shared appliance functionality later
//im also using this shit as reference 
public class Appliance : MonoBehaviour, IInteractable
{
    public Canvas Panel;
    public float animationTime = 0.5f;

    public bool CanInteractWith(PlayerControl player) => true;

    public void OpenPanel(bool show)
    {
        LeanTween.cancel(Panel.gameObject);

        if (show)
        {
            Panel.transform.LeanScale(Vector3.zero, 0f).setOnComplete(() =>
            {
                Panel.gameObject.SetActive(true);
                Panel.transform.LeanScale(Vector3.one, animationTime).setEaseOutQuint();
            });
        }
        else
        {
            Panel.transform.LeanScale(Vector3.zero, animationTime)
                .setEaseOutQuint()
                .setOnComplete(() => Panel.gameObject.SetActive(false));
        }
    }

    public void Interact(PlayerControl player)
    {
        StartCoroutine(TestAppear(player));
    }

    private IEnumerator TestAppear(PlayerControl player)
    {
        OpenPanel(true);
        if (player != null) player.enabled = false;
        yield return new WaitForSeconds(2f);
        OpenPanel(false);
        if (player != null) player.enabled = true;
    }
}