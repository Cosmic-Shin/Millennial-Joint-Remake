using UnityEngine;

// One alien customer model: shows exactly one Mood face + one Emote bubble, and plays Wait / Eat.
public class AlienCustomerVisual : MonoBehaviour
{
    public enum Mood { Happy, Impatient, Angry }

    private static readonly string[] MoodNames = { "Happy", "Impatient", "Angry" };
    private static readonly int EatingHash = Animator.StringToHash("Eating");

    private readonly GameObject[] moods = new GameObject[3];
    private readonly GameObject[] emotes = new GameObject[3];
    private Animator animator;
    private Mood? current;

    public void Init(string prefix, RuntimeAnimatorController controller)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            for (int i = 0; i < 3; i++)
            {
                if (t.name == $"{prefix}_Mood_{MoodNames[i]}") moods[i] = t.gameObject;
                if (t.name == $"{prefix}_Emote_{MoodNames[i]}") emotes[i] = t.gameObject;
            }

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
    }

    public void SetMood(Mood mood)
    {
        if (current == mood)
            return;
        current = mood;

        for (int i = 0; i < 3; i++)
        {
            if (moods[i] != null) moods[i].SetActive(i == (int)mood);
            if (emotes[i] != null) emotes[i].SetActive(i == (int)mood);
        }
    }

    public void SetEating(bool eating)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetBool(EatingHash, eating);
    }
}
