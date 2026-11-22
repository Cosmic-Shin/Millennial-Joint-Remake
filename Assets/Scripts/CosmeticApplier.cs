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
        {
            visible.ExceptWith(c.hides);
            if (c.lift != 0f && parts.TryGetValue(c.partSuffix, out GameObject part))
                Lift(part, c.lift);
        }

        foreach (var pair in parts)
            pair.Value.SetActive(visible.Contains(pair.Key));
    }

    // Parts are skinned to the head bone, so moving the transform does nothing: shift the mesh itself
    // (in bind-pose space, so it still follows the head). One shifted copy per source mesh, shared by all chefs.
    private static readonly Dictionary<Mesh, Mesh> liftedMeshes = new Dictionary<Mesh, Mesh>();

    private void Lift(GameObject part, float lift)
    {
        var smr = part.GetComponent<SkinnedMeshRenderer>();
        if (smr == null || smr.sharedMesh == null || liftedMeshes.ContainsValue(smr.sharedMesh))
            return;

        if (!liftedMeshes.TryGetValue(smr.sharedMesh, out Mesh lifted))
        {
            lifted = Instantiate(smr.sharedMesh);
            lifted.name = smr.sharedMesh.name + "_Lifted";
            Vector3 up = smr.transform.InverseTransformVector(transform.up) * lift;
            Vector3[] v = lifted.vertices;
            for (int i = 0; i < v.Length; i++)
                v[i] += up;
            lifted.vertices = v;
            lifted.RecalculateBounds();
            liftedMeshes[smr.sharedMesh] = lifted;
        }

        smr.sharedMesh = lifted;
    }
}
