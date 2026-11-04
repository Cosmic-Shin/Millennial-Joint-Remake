using System.Collections.Generic;
using UnityEngine;

// Shows base parts + equipped cosmetics on a chef model, hides everything else.
public class CosmeticApplier : MonoBehaviour
{
    public static readonly string[] BaseParts =
        { "Head", "Hair", "FacialHair", "Hat_Chef", "Shirt", "Arms", "Gloves", "Pants", "Belt", "Shoes", "Apron" };

    [Tooltip("Child mesh name prefix, e.g. Charles_New_")]
    public string partPrefix;

    private Dictionary<string, GameObject> parts;

    private void CacheParts()
    {
        parts = new Dictionary<string, GameObject>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r.name.StartsWith(partPrefix))
                parts[r.name.Substring(partPrefix.Length)] = r.gameObject;
    }

    public void Apply(IEnumerable<CosmeticDefinition> equipped)
    {
        if (parts == null)
            CacheParts();

        var visible = new HashSet<string>(BaseParts);
        var worn = new List<CosmeticDefinition>();

        if (equipped != null)
            foreach (CosmeticDefinition c in equipped)
            {
                if (c == null || worn.Exists(w => w.slot == c.slot || w.ConflictsWith(c)))
                    continue;
                worn.Add(c);
                visible.Add(c.partSuffix);
                visible.UnionWith(c.extraParts);
            }

        foreach (CosmeticDefinition c in worn)
            visible.ExceptWith(c.hides);

        foreach (var pair in parts)
            pair.Value.SetActive(visible.Contains(pair.Key));
    }
}
