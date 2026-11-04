using UnityEngine;

[CreateAssetMenu(menuName = "Millennial Joint/Map")]
public class MapDefinition : ScriptableObject
{
    public string displayName;
    [Tooltip("Name of the matching layout in MapSelector (maps are layouts inside SampleScene, not separate scenes).")]
    public string layoutName;
    public Sprite preview;
}
