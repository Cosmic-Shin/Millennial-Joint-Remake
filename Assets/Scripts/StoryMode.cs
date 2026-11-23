using System;
using System.Linq;
using UnityEngine;

// The 2-player campaign: Time matches with a fixed map, menu and goal. Static, so it survives the scene reload used by Retry.
public static class StoryMode
{
    public class Chapter
    {
        public string title;
        public string layoutName;
        public float goal;
        public float seconds;
        public OrderItemType[] menu;
    }

    // Money / goal needed for 2 and 3 stars (reaching the goal = 1 star).
    public const float TwoStarRatio = 1.35f;
    public const float ThreeStarRatio = 1.7f;

    // ponytail: goals come from measured station distances and cook times; tune them here after 2-player playtests.
    public static readonly Chapter[] Chapters =
    {
        new Chapter { title = "Grand Opening", layoutName = "Main", goal = 50f, seconds = 120f,
            menu = new[] { OrderItemType.Soda, OrderItemType.FriedChicken } },
        new Chapter { title = "Side Orders", layoutName = "SplitIslands", goal = 60f, seconds = 150f,
            menu = new[] { OrderItemType.Soda, OrderItemType.IceTea, OrderItemType.FriedChicken, OrderItemType.Fries } },
        new Chapter { title = "Morning Rush", layoutName = "BridgeIslands", goal = 70f, seconds = 150f,
            menu = new[] { OrderItemType.OrangeJuice, OrderItemType.Coffee, OrderItemType.Fries, OrderItemType.StrawberryIceCream } },
        new Chapter { title = "Orbital Sweets", layoutName = "RingStation", goal = 85f, seconds = 180f,
            menu = new[] { OrderItemType.Coffee, OrderItemType.BubblegumIceCream, OrderItemType.MangoIceCream, OrderItemType.ShrimpTempura, OrderItemType.FriedChicken } },
        new Chapter { title = "Lunch in Transit", layoutName = "ShuttleHop", goal = 95f, seconds = 270f,
            menu = new[] { OrderItemType.Burger, OrderItemType.Fries, OrderItemType.ChocolateDonut, OrderItemType.StrawberryDonut, OrderItemType.OrangeJuice } },
        new Chapter { title = "The Big Bang Special", layoutName = "AsteroidDiner", goal = 170f, seconds = 270f,
            menu = new[] { OrderItemType.Sandwich, OrderItemType.ChiliDog, OrderItemType.Burger, OrderItemType.GalaxyDonut, OrderItemType.ShrimpTempura } }
    };

    private const string UnlockedKey = "Story_Unlocked";

    public static int CurrentIndex = -1;
    public static bool IsActive => CurrentIndex >= 0;
    public static Chapter Current => IsActive ? Chapters[CurrentIndex] : null;
    public static bool HasNext => IsActive && CurrentIndex + 1 < Chapters.Length;

    public static int UnlockedCount => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 1), 1, Chapters.Length);
    public static bool IsUnlocked(int chapter) => chapter >= 0 && chapter < UnlockedCount;
    public static int GetStars(int chapter) => PlayerPrefs.GetInt("Story_Stars_" + chapter, 0);
    public static int IndexOfLayout(string layoutName) => Array.FindIndex(Chapters, c => c.layoutName == layoutName);

    // Time, Speed and Versus: a map opens once its chapter is cleared. The first map is always open.
    public static bool IsMapOpen(string layoutName)
    {
        int chapter = IndexOfLayout(layoutName);
        return chapter <= 0 || GetStars(chapter) > 0;
    }

    public static int TotalStars => Enumerable.Range(0, Chapters.Length).Sum(GetStars);
    public static bool OnMenu(OrderItemType meal) => !IsActive || Current.menu.Contains(meal);

    // Station pickers: the next option on today's menu, wrapping around.
    public static int NextOnMenu(OrderItemType[] options, int from, int step)
    {
        for (int i = 1; i <= options.Length; i++)
        {
            int index = ((from + step * i) % options.Length + options.Length) % options.Length;
            if (OnMenu(options[index]))
                return index;
        }
        return Mathf.Max(from, 0);
    }

    // Deals the whole menu before repeating, so every meal (especially new ones) actually comes up.
    private static readonly System.Collections.Generic.List<OrderItemType> mealBag = new System.Collections.Generic.List<OrderItemType>();

    public static OrderItemType RandomMeal()
    {
        if (mealBag.Count == 0)
            mealBag.AddRange(Current.menu);

        int pick = UnityEngine.Random.Range(0, mealBag.Count);
        OrderItemType meal = mealBag[pick];
        mealBag.RemoveAt(pick);
        return meal;
    }

    // A finished meal from this chapter's menu, for the Free Food power-up.
    public static KitchenItemData RandomFinishedMeal()
    {
        OrderItemType meal = Current.menu[UnityEngine.Random.Range(0, Current.menu.Length)];
        var food = new KitchenItemData();
        switch (meal)
        {
            case OrderItemType.Soda: food.Set(ItemType.Cup); food.cupHasSoda = true; break;
            case OrderItemType.IceTea: food.Set(ItemType.Cup); food.cupHasIceTea = true; break;
            case OrderItemType.OrangeJuice: food.Set(ItemType.Cup); food.cupHasOrangeJuice = true; break;
            case OrderItemType.Coffee: food.Set(ItemType.Cup); food.cupHasCoffee = true; break;
            case OrderItemType.StrawberryIceCream: food.Set(ItemType.StrawberryIceCream); break;
            case OrderItemType.BubblegumIceCream: food.Set(ItemType.BubblegumIceCream); break;
            case OrderItemType.MangoIceCream: food.Set(ItemType.MangoIceCream); break;
            default:
                food.Set(ItemType.Plate);
                food.plateHasBun = food.plateHasPatty = food.plateHasVeggie = meal == OrderItemType.Burger;
                food.plateHasBread = food.plateHasHam = meal == OrderItemType.Sandwich;
                food.plateHasChicken = meal == OrderItemType.FriedChicken;
                food.plateHasFries = meal == OrderItemType.Fries;
                food.plateHasDogBun = food.plateHasHotdog = food.plateHasChili = meal == OrderItemType.ChiliDog;
                food.plateHasShrimp = meal == OrderItemType.ShrimpTempura;
                food.plateHasDonut = meal is OrderItemType.ChocolateDonut or OrderItemType.StrawberryDonut or OrderItemType.GalaxyDonut;
                food.donutGlaze = meal == OrderItemType.ChocolateDonut ? DonutGlaze.Chocolate
                    : meal == OrderItemType.StrawberryDonut ? DonutGlaze.Strawberry
                    : meal == OrderItemType.GalaxyDonut ? DonutGlaze.Galaxy : DonutGlaze.None;
                break;
        }
        return food;
    }

    // Meals that appear for the first time in this chapter.
    public static OrderItemType[] NewMeals(int chapter) =>
        Chapters[chapter].menu.Where(m => !Chapters.Take(chapter).Any(c => c.menu.Contains(m))).ToArray();

    public static bool Stocks(ItemType ingredient) => !IsActive || Current.menu.Any(m => Ingredients(m).Contains(ingredient));

    private static ItemType[] Ingredients(OrderItemType meal) => meal switch
    {
        OrderItemType.Burger => new[] { ItemType.Bun, ItemType.PattyRaw, ItemType.VeggieRaw },
        OrderItemType.Sandwich => new[] { ItemType.Bread, ItemType.HamRaw },
        OrderItemType.FriedChicken => new[] { ItemType.ChickenRaw },
        OrderItemType.Fries => new[] { ItemType.FrozenFries },
        OrderItemType.ChiliDog => new[] { ItemType.DogBun, ItemType.HotDogRaw },
        OrderItemType.ShrimpTempura => new[] { ItemType.ShrimpRaw },
        OrderItemType.ChocolateDonut or OrderItemType.StrawberryDonut or OrderItemType.GalaxyDonut => new[] { ItemType.DonutRaw },
        _ => Array.Empty<ItemType>() // drinks and ice cream come from their machines
    };

    // Story matches ignore the custom Match Rules so every chapter plays the same.
    public static void ApplyRules(OrderManager om)
    {
        if (!IsActive || om == null)
            return;

        mealBag.Clear();
        MatchRules.OrderTimeMultiplier = 1f;
        MatchRules.MoveSpeedMultiplier = 1f;
        MatchRules.UnlimitedStamina = false;
        MatchRules.PowerUpIntervalMultiplier = 1f;
        om.timeModeDuration = Current.seconds;
        om.timeModeQuota = Current.goal;
    }

    // Returns true when this win opened a new chapter.
    public static bool RecordResult(int stars)
    {
        if (!IsActive || stars <= 0)
            return false;

        if (stars > GetStars(CurrentIndex))
            PlayerPrefs.SetInt("Story_Stars_" + CurrentIndex, stars);

        bool opened = HasNext && UnlockedCount <= CurrentIndex + 1;
        if (opened)
            PlayerPrefs.SetInt(UnlockedKey, CurrentIndex + 2);
        PlayerPrefs.Save();
        return opened;
    }
}
