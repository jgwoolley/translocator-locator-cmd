using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// What the statue wears, as chosen when it was edited. Layers go on inner to outer: the player's look (hair, face,
/// beard), then clothing, then armor. Only the layers that were asked for are stored, so only those are synced.
/// </summary>
public class StatueAppearance
{
    public bool ShowLook = true;
    public bool ShowClothing = true;
    public bool ShowArmor = true;

    public string[] SkinShapes = [];
    public ItemStack[] Clothing = [];
    public ItemStack[] Armor = [];

    /// <summary>The items held in the hands, shown in the same stone as the rest of the statue.</summary>
    public const string NoMount = "";
    public const string ElkCow = "elkcow";
    public const string ElkBull = "elkbull";

    /// <summary>How the elk stands: one of <see cref="StatuePoses.ElkPoses"/>. Only used when there is an elk.</summary>
    public string ElkPose = StatuePoses.ElkStand;

    /// <summary>The frame of the elk's stride (-1 picks the best one). Only used when the elk moves.</summary>
    public int ElkFrame = -1;

    /// <summary>What the player rides: <see cref="NoMount"/>, <see cref="ElkCow"/> or <see cref="ElkBull"/>.</summary>
    public string Mount = NoMount;

    /// <summary>Only the three known mounts are accepted, and a bust (head only) cannot ride.</summary>
    public static bool IsValidMount(string mount, bool bust) =>
        mount is NoMount || (!bust && mount is ElkCow or ElkBull);

    /// <summary>The bull's antlers: 0 for none, then 1 (smallest) to <see cref="MaxAntlers"/> (biggest), as in the elk models.</summary>
    public const int MaxAntlers = 8;

    public const int DefaultAntlers = 4;

    public int Antlers = DefaultAntlers;

    /// <summary>Whether the statue stands without the stone pedestal (or, with an elk, the platform) under it.</summary>
    public bool NoPedestal;

    /// <summary>What the statue shows: <see cref="StatueTypes.Player"/>, or a special statue such as the robot.</summary>
    public string Type = StatueTypes.Player;

    /// <summary>The size of the antlers an elk wears, from the code of its antler item (<c>antler-elk-05</c> is 5), or 0.</summary>
    public static int AntlersFromItemCode(string? path) =>
        path != null && path.StartsWith("antler-") && int.TryParse(path[(path.LastIndexOf('-') + 1)..], out var size)
        && size is >= 1 and <= MaxAntlers ? size : 0;

    /// <summary>The tack on the mount, indexed by <see cref="StatueTack.Piece"/>.</summary>
    public string[] Tack = StatueTack.Empty();

    public ItemStack? RightHand;
    public ItemStack? LeftHand;

    /// <summary>Which hotbar slot a hand takes its item from. <see cref="HandEmpty"/> leaves the hand empty.</summary>
    public const int HandEmpty = -1;

    /// <summary>The hand takes the item of the statue that was copied (see <c>EditRequest.CopySlot</c>).</summary>
    public const int HandFromCopy = -2;

    /// <summary>A copy of one item from a hotbar slot: nothing is taken from the player.</summary>
    public static ItemStack? PickHotbar(IPlayer player, int slot)
    {
        var hotbar = player.InventoryManager?.GetHotbarInventory();
        if (hotbar == null || slot < 0 || slot >= hotbar.Count) return null;

        var held = hotbar[slot]?.Itemstack?.Clone();
        if (held != null) held.StackSize = 1;
        return held;
    }

    /// <summary>The same look, with only the layers that are asked for.</summary>
    public StatueAppearance WithLayers(bool look, bool clothing, bool armor) => new()
    {
        ShowLook = look,
        ShowClothing = clothing,
        ShowArmor = armor,
        SkinShapes = look ? SkinShapes : [],
        Clothing = clothing ? Clothing : [],
        Armor = armor ? Armor : [],
        NoPedestal = NoPedestal,
        Type = Type,
    };

    /// <summary>Copies the requested layers from a player who is online.</summary>
    public static StatueAppearance Capture(IPlayer player, bool look, bool clothing, bool armor)
    {
        var appearance = new StatueAppearance { ShowLook = look, ShowClothing = clothing, ShowArmor = armor };
        if (look) appearance.SkinShapes = StatueSkin.ShapesOf(player);

        var worn = WornGear(player);
        if (clothing) appearance.Clothing = worn.Where(g => !IsArmor(g.Type)).Select(g => g.Stack).ToArray();
        if (armor) appearance.Armor = worn.Where(g => IsArmor(g.Type)).Select(g => g.Stack).ToArray();
        return appearance;
    }

    private static bool IsArmor(EnumCharacterDressType type) =>
        type is EnumCharacterDressType.ArmorHead or EnumCharacterDressType.ArmorBody or EnumCharacterDressType.ArmorLegs;

    private static List<(EnumCharacterDressType Type, ItemStack Stack)> WornGear(IPlayer player)
    {
        var gear = new List<(EnumCharacterDressType, ItemStack)>();
        var inventory = player.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
        if (inventory == null) return gear;

        foreach (var slot in inventory)
        {
            if (slot is ItemSlotCharacter { Empty: false } character) gear.Add((character.Type, character.Itemstack!.Clone()));
        }

        return gear;
    }

    public void Write(ITreeAttribute tree)
    {
        tree.SetBool("showLook", ShowLook);
        tree.SetBool("showClothing", ShowClothing);
        tree.SetBool("showArmor", ShowArmor);
        tree.SetString("mount", Mount);
        tree.SetString("elkPose", ElkPose);
        tree.SetInt("elkFrame", ElkFrame);
        tree.SetString("tack", string.Join(';', Tack));
        tree.SetInt("antlers", Antlers);
        tree.SetBool("noPedestal", NoPedestal);
        tree.SetString("statueType", Type);
        tree.SetString("skinShapes", string.Join(';', SkinShapes));
        WriteStacks(tree, "clothing", Clothing);
        WriteStacks(tree, "armor", Armor);
        WriteStacks(tree, "hands", [RightHand, LeftHand]);
    }

    public static StatueAppearance Read(ITreeAttribute tree, IWorldAccessor world)
    {
        var skin = tree.GetString("skinShapes", "");
        return new StatueAppearance
        {
            ShowLook = tree.GetBool("showLook", true),
            ShowClothing = tree.GetBool("showClothing", true),
            ShowArmor = tree.GetBool("showArmor", true),
            Mount = tree.GetString("mount", NoMount),
            ElkPose = tree.GetString("elkPose", StatuePoses.ElkStand),
            ElkFrame = tree.GetInt("elkFrame", -1),
            Tack = ReadTack(tree.GetString("tack", "")),
            NoPedestal = tree.GetBool("noPedestal", false),
            Type = StatueTypes.IsKnown(tree.GetString("statueType", StatueTypes.Player)) ? tree.GetString("statueType", StatueTypes.Player) : StatueTypes.Player,
            Antlers = Math.Clamp(tree.GetInt("antlers", DefaultAntlers), 0, MaxAntlers),
            SkinShapes = skin == "" ? [] : skin.Split(';'),
            Clothing = ReadStacks(tree, "clothing", world),
            Armor = ReadStacks(tree, "armor", world),
            RightHand = ReadHand(tree, "hands0", world),
            LeftHand = ReadHand(tree, "hands1", world),
        };
    }

    /// <summary>Unknown names are dropped, so a damaged or modified save cannot ask for a shape that does not exist.</summary>
    private static string[] ReadTack(string text)
    {
        var parts = text.Split(';');
        var tack = StatueTack.Empty();
        for (var i = 0; i < StatueTack.PieceCount && i < parts.Length; i++)
        {
            if (StatueTack.IsValid((StatueTack.Piece)i, parts[i])) tack[i] = parts[i];
        }

        return tack;
    }

    private static void WriteStacks(ITreeAttribute tree, string key, ItemStack?[] stacks)
    {
        tree.SetInt(key + "Count", stacks.Length);
        for (var i = 0; i < stacks.Length; i++)
        {
            if (stacks[i] != null) tree.SetItemstack(key + i, stacks[i]!.Clone());
        }
    }

    private static ItemStack? ReadHand(ITreeAttribute tree, string key, IWorldAccessor world)
    {
        var stack = tree.GetItemstack(key);
        return stack != null && stack.ResolveBlockOrItem(world) ? stack : null;
    }

    private static ItemStack[] ReadStacks(ITreeAttribute tree, string key, IWorldAccessor world)
    {
        var stacks = new List<ItemStack>();
        for (var i = 0; i < tree.GetInt(key + "Count"); i++)
        {
            var stack = tree.GetItemstack(key + i);
            if (stack != null && stack.ResolveBlockOrItem(world)) stacks.Add(stack);
        }

        return stacks.ToArray();
    }
}
