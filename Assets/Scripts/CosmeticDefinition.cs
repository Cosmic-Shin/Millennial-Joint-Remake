using UnityEngine;

public enum CosmeticSlot { Hat, Glasses, Top, Apron, Neck, Wrist, Accessory, Shoes }

[CreateAssetMenu(menuName = "Millennial Joint/Cosmetic")]
public class CosmeticDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public CosmeticSlot slot;
    [Tooltip("Child mesh suffix after the chef prefix, e.g. Hat_Beanie")]
    public string partSuffix;
    [Tooltip("Extra child meshes shown with this item, e.g. Hat_Propeller_Blades")]
    public string[] extraParts = new string[0];
    [Tooltip("Parts hidden while this is worn (base parts or other cosmetics)")]
    public string[] hides = new string[0];
    [Tooltip("Cosmetics that can't be worn together with this one")]
    public CosmeticDefinition[] conflicts = new CosmeticDefinition[0];

    public bool ConflictsWith(CosmeticDefinition other) =>
        other != null && (System.Array.IndexOf(conflicts, other) >= 0 || System.Array.IndexOf(other.conflicts, this) >= 0);
}
