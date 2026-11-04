using System.Collections.Generic;

// Static, so choices survive the scene reload used by Retry.
public static class GameSession
{
    public class PlayerChoice
    {
        public CharacterDefinition character;
        public readonly Dictionary<CosmeticSlot, CosmeticDefinition> cosmetics = new Dictionary<CosmeticSlot, CosmeticDefinition>();
    }

    public static readonly PlayerChoice[] Players = { new PlayerChoice(), new PlayerChoice() };

    // null = random layout (MapSelector's original behaviour)
    public static MapDefinition Map;

    public static PlayerChoice ForPlayer(int playerNumber) => Players[UnityEngine.Mathf.Clamp(playerNumber - 1, 0, 1)];
}
