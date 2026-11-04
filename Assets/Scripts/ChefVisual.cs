using System.Collections.Generic;
using UnityEngine;

// Swaps a player's original model for the chef + cosmetics chosen in GameSession.
public class ChefVisual : MonoBehaviour
{
    [Tooltip("The original model (e.g. Idle_C). Hidden when a chef is chosen; its local pose is reused.")]
    public GameObject defaultModel;

    private GameObject current;

    private void Start() => ApplyFromSession();

    public void ApplyFromSession()
    {
        PlayerControl player = GetComponent<PlayerControl>();
        GameSession.PlayerChoice choice = GameSession.ForPlayer(player.playerNumber);
        if (choice.character == null)
            return;

        if (current != null)
            Destroy(current);

        Transform pose = defaultModel != null ? defaultModel.transform : transform;
        Animator animator = Spawn(choice.character, choice.cosmetics.Values, transform);
        current = animator.gameObject;
        current.transform.SetLocalPositionAndRotation(pose.localPosition, pose.localRotation);
        current.transform.localScale = pose.localScale;

        if (defaultModel != null)
            defaultModel.SetActive(false);

        player.SetModel(animator);
    }

    // Shared with the select-screen previews.
    public static Animator Spawn(CharacterDefinition def, IEnumerable<CosmeticDefinition> cosmetics, Transform parent)
    {
        GameObject go = Instantiate(def.prefab, parent, false);
        go.name = def.displayName;

        Animator animator = go.GetComponent<Animator>();
        if (animator == null)
            animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = def.animator;
        animator.applyRootMotion = false;

        CosmeticApplier applier = go.AddComponent<CosmeticApplier>();
        applier.partPrefix = def.partPrefix;
        applier.Apply(cosmetics);

        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith("_Hat_Propeller_Blades"))
                t.gameObject.AddComponent<Spin>();

        return animator;
    }
}
