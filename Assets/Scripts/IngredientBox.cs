using System.Collections.Generic;
using UnityEngine;

public class IngredientBox : BaseStation, IInteractable
{
    [Header("Ingredient Selection")]
    public ItemType[] selectableIngredients = new ItemType[]
    {
        ItemType.Bun,
        ItemType.Bread,
        ItemType.VeggieRaw,
        ItemType.PattyRaw,
        ItemType.FrozenFries,
        ItemType.ChickenRaw,
        ItemType.HamRaw,
        ItemType.DogBun,
        ItemType.HotDogRaw
    };

    [Header("Smart Suggestion")]
    [Tooltip("Plates on counters within this distance of the box are checked for their next ingredient")]
    public float suggestionPlateRadius = 15f;
    public Color suggestionTextColor = new Color(0.05f, 0.5f, 0.1f);

    private static readonly (string name, ItemType[] items)[] Categories =
    {
        ("Breads", new[] { ItemType.Bun, ItemType.Bread, ItemType.DogBun }),
        ("Meats", new[] { ItemType.PattyRaw, ItemType.ChickenRaw, ItemType.HamRaw, ItemType.HotDogRaw }),
        ("Produce", new[] { ItemType.VeggieRaw, ItemType.FrozenFries })
    };

    private PlayerControl currentPlayer;
    private bool isSelectingIngredient;
    private bool choosingCategory;
    private readonly List<(string name, List<ItemType> items)> groups = new List<(string, List<ItemType>)>();
    private int categoryIndex;
    private int itemIndex;
    private ItemType? suggestion;
    private Color normalTextColor = Color.black;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (isSelectingIngredient && currentPlayer != player) return false;

        return player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;
        if (isSelectingIngredient && currentPlayer != player) return;

        currentPlayer = player;

        if (!isSelectingIngredient)
            StartIngredientSelection();
        else if (choosingCategory)
            OpenCategory();
        else
            ConfirmIngredientSelection();
    }

    private void StartIngredientSelection()
    {
        BuildGroups();
        if (groups.Count == 0)
            return;

        isSelectingIngredient = true;
        currentPlayer.doMove = false;
        currentPlayer.ResetMenuStep();

        if (currentPlayer.emoteSelectionText != null)
            normalTextColor = currentPlayer.emoteSelectionText.color;

        suggestion = FindSuggestion();
        categoryIndex = Mathf.Max(0, groups.FindIndex(g => suggestion.HasValue && g.items.Contains(suggestion.Value)));
        choosingCategory = groups.Count > 1;
        if (!choosingCategory)
            OpenCategory();

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(true);

        UpdateIngredientSelectionText();
    }

    private void BuildGroups()
    {
        groups.Clear();
        foreach (var (name, items) in Categories)
        {
            var available = new List<ItemType>();
            foreach (ItemType item in items)
                if (System.Array.IndexOf(selectableIngredients, item) >= 0) available.Add(item);

            if (available.Count > 0)
                groups.Add((name, available));
        }

        var other = new List<ItemType>();
        foreach (ItemType item in selectableIngredients)
            if (!groups.Exists(g => g.items.Contains(item))) other.Add(item);
        if (other.Count > 0)
            groups.Add(("Other", other));
    }

    private void OpenCategory()
    {
        choosingCategory = false;
        itemIndex = suggestion.HasValue ? Mathf.Max(0, groups[categoryIndex].items.IndexOf(suggestion.Value)) : 0;
        currentPlayer.ResetMenuStep();
        UpdateIngredientSelectionText();
    }

    private void ConfirmIngredientSelection()
    {
        ItemType selectedType = groups[categoryIndex].items[itemIndex];
        currentPlayer.heldItem.Set(selectedType);
        currentPlayer.RefreshHeldItemDisplay();
        Show(currentPlayer, "Picked up " + currentPlayer.heldItem.GetDisplayName());
        AudioManager.Instance?.PlayGetIngredientFromBoxSFX();

        EndIngredientSelection();
    }

    private void EndIngredientSelection()
    {
        isSelectingIngredient = false;

        if (currentPlayer != null)
        {
            currentPlayer.doMove = true;
            currentPlayer.currentIngredientBox = null;

            if (currentPlayer.emoteSelectionObject != null)
                currentPlayer.emoteSelectionObject.SetActive(false);

            if (currentPlayer.emoteSelectionText != null)
            {
                currentPlayer.emoteSelectionText.text = string.Empty;
                currentPlayer.emoteSelectionText.color = normalTextColor;
            }
        }

        currentPlayer = null;
    }

    private void UpdateIngredientSelectionText()
    {
        if (currentPlayer == null || currentPlayer.emoteSelectionText == null)
            return;

        var group = groups[categoryIndex];
        bool suggested;
        string label;

        if (choosingCategory)
        {
            label = group.name;
            suggested = suggestion.HasValue && group.items.Contains(suggestion.Value);
        }
        else
        {
            ItemType item = group.items[itemIndex];
            label = GetIngredientDisplayName(item);
            suggested = suggestion.HasValue && suggestion.Value == item;
        }

        currentPlayer.emoteSelectionText.text = "< " + label + " >";
        currentPlayer.emoteSelectionText.color = suggested ? suggestionTextColor : normalTextColor;
    }

    private string GetIngredientDisplayName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Bun: return "Bun";
            case ItemType.Bread: return "Bread";
            case ItemType.VeggieRaw: return "Veggie";
            case ItemType.PattyRaw: return "Raw Patty";
            case ItemType.FrozenFries: return "Frozen Fries";
            case ItemType.ChickenRaw: return "Raw Chicken";
            case ItemType.HamRaw: return "Ham & Cheese";
            case ItemType.DogBun: return "Dog Bun";
            case ItemType.HotDogRaw: return "Raw Hotdog";
            default: return type.ToString();
        }
    }

    public void HandleIngredientSelectionInput()
    {
        if (!isSelectingIngredient || currentPlayer == null)
            return;

        if (Vector3.Distance(transform.position, currentPlayer.transform.position) > 3f)
        {
            EndIngredientSelection();
            return;
        }

        if (currentPlayer.IsDropItemPressed())
        {
            if (!choosingCategory && groups.Count > 1)
            {
                choosingCategory = true;
                currentPlayer.ResetMenuStep();
                UpdateIngredientSelectionText();
            }
            else
            {
                EndIngredientSelection();
            }
            return;
        }

        int step = currentPlayer.GetMenuStep();
        if (step == 0)
            return;

        if (choosingCategory)
            categoryIndex = (categoryIndex + step + groups.Count) % groups.Count;
        else
            itemIndex = (itemIndex + step + groups[categoryIndex].items.Count) % groups[categoryIndex].items.Count;

        UpdateIngredientSelectionText();
    }

    private ItemType? FindSuggestion()
    {
        ItemType? fromPlate = NextIngredientForNearbyPlate();
        if (fromPlate.HasValue && Offers(fromPlate.Value))
            return fromPlate;

        ItemType? fromOrder = FirstIngredientForUrgentOrder();
        if (fromOrder.HasValue && Offers(fromOrder.Value))
            return fromOrder;

        return null;
    }

    private bool Offers(ItemType item) => System.Array.IndexOf(selectableIngredients, item) >= 0;

    private ItemType? NextIngredientForNearbyPlate()
    {
        bool pattyOnStove = false, hotdogOnStove = false;
        foreach (StoveCounter stove in FindObjectsByType<StoveCounter>(FindObjectsSortMode.None))
        {
            ItemType t = stove.storedItem.type;
            pattyOnStove |= t == ItemType.PattyRaw || t == ItemType.PattyCooked;
            hotdogOnStove |= t == ItemType.HotDogRaw || t == ItemType.HotDogCooked;
        }

        float bestDistance = suggestionPlateRadius;
        ItemType? best = null;

        foreach (CounterTop counter in FindObjectsByType<CounterTop>(FindObjectsSortMode.None))
        {
            KitchenItemData plate = counter.storedItem;
            if (!plate.IsPlate)
                continue;

            float distance = Vector3.Distance(transform.position, counter.transform.position);
            if (distance >= bestDistance)
                continue;

            ItemType? next = NextFor(plate, pattyOnStove, hotdogOnStove);
            if (next.HasValue)
            {
                bestDistance = distance;
                best = next;
            }
        }

        return best;
    }

    private static ItemType? NextFor(KitchenItemData p, bool pattyOnStove, bool hotdogOnStove)
    {
        bool onlyBun = p.plateHasBun && !p.plateHasPatty && !p.plateHasVeggie;
        if (onlyBun) return pattyOnStove ? ItemType.VeggieRaw : ItemType.PattyRaw;
        if (p.plateHasBun && p.plateHasPatty && !p.plateHasVeggie) return ItemType.VeggieRaw;
        if (p.plateHasBread && !p.plateHasHam) return ItemType.HamRaw;
        if (p.plateHasDogBun && !p.plateHasHotdog && !hotdogOnStove) return ItemType.HotDogRaw;
        return null;
    }

    private ItemType? FirstIngredientForUrgentOrder()
    {
        if (OrderManager.Instance == null || currentPlayer == null)
            return null;

        QueuedOrder mostUrgent = OrderManager.Instance.MostUrgentOrder(currentPlayer, dish => StartingIngredient(dish).HasValue);
        return mostUrgent != null ? StartingIngredient(mostUrgent.item.type) : null;
    }

    private static ItemType? StartingIngredient(OrderItemType dish)
    {
        switch (dish)
        {
            case OrderItemType.Burger: return ItemType.Bun;
            case OrderItemType.Sandwich: return ItemType.Bread;
            case OrderItemType.ChiliDog: return ItemType.DogBun;
            case OrderItemType.Fries: return ItemType.FrozenFries;
            case OrderItemType.FriedChicken: return ItemType.ChickenRaw;
            default: return null;
        }
    }

    private void LateUpdate()
    {
        if (!isSelectingIngredient || currentPlayer == null)
            return;

        if (currentPlayer.emoteSelectionObject == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Transform textTransform = currentPlayer.emoteSelectionObject.transform;

        Vector3 direction = cam.transform.position - textTransform.position;
        textTransform.rotation = Quaternion.LookRotation(direction);
        textTransform.Rotate(0f, 180f, 0f);
    }
}
