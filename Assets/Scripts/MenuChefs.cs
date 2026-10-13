using UnityEngine;

public class MenuChefs : MonoBehaviour
{
    public Camera chefCamera;
    public Animator[] chefs;
    public string[] emoteTriggers = { "Emote1", "Emote2", "Emote3", "Emote4" };
    [Tooltip("Seconds between emotes (random in this range)")]
    public Vector2 emoteInterval = new Vector2(3f, 6f);

    private float[] nextEmote;

    private void OnEnable()
    {
        if (chefCamera != null)
            chefCamera.enabled = true;

        nextEmote = new float[chefs.Length];
        for (int i = 0; i < chefs.Length; i++)
            nextEmote[i] = Time.unscaledTime + Random.Range(1f, emoteInterval.y) + i * 1.5f;
    }

    private void OnDisable()
    {
        if (chefCamera != null)
            chefCamera.enabled = false;
    }

    private void Update()
    {
        for (int i = 0; i < chefs.Length; i++)
        {
            if (chefs[i] == null || Time.unscaledTime < nextEmote[i])
                continue;

            chefs[i].SetTrigger(emoteTriggers[Random.Range(0, emoteTriggers.Length)]);
            nextEmote[i] = Time.unscaledTime + Random.Range(emoteInterval.x, emoteInterval.y);
        }
    }
}
