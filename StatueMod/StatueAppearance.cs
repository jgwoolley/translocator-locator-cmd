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
    public ItemStack? RightHand;
    public ItemStack? LeftHand;

    /// <summary>What a hand does when the statue is saved. The item is only copied, never taken from the player.</summary>
    public enum HandMode
    {
        Keep = 0,
        CopyHeld = 1,
        Empty = 2,
    }

    public static ItemStack? PickHand(HandMode mode, ItemStack? current, ItemSlot? heldSlot)
    {
        switch (mode)
        {
            case HandMode.CopyHeld:
                var held = heldSlot?.Itemstack?.Clone();
                if (held != null) held.StackSize = 1;
                return held;
            case HandMode.Empty:
                return null;
            default:
                return current;
        }
    }

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
            SkinShapes = skin == "" ? [] : skin.Split(';'),
            Clothing = ReadStacks(tree, "clothing", world),
            Armor = ReadStacks(tree, "armor", world),
            RightHand = ReadHand(tree, "hands0", world),
            LeftHand = ReadHand(tree, "hands1", world),
        };
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
