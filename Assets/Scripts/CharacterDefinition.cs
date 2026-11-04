using UnityEngine;

[CreateAssetMenu(menuName = "Millennial Joint/Character")]
public class CharacterDefinition : ScriptableObject
{
    public string displayName;
    [Tooltip("Model to spawn (the chef FBX).")]
    public GameObject prefab;
    public Sprite portrait;
    [Tooltip("Child mesh name prefix, e.g. Charles_New_")]
    public string partPrefix;
    [Tooltip("Animator for this chef (override of the shared chef controller).")]
    public RuntimeAnimatorController animator;
}
