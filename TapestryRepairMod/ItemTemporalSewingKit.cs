#nullable enable

using Vintagestory.API.Common;

namespace Nf3t.VintageStory.TapestryRepair;

/// <summary>
/// The temporal sewing kit: a durable tool, crafted from temporal fiber made at the <see cref="BlockTemporalLoom"/>, that
/// repairs ruined tapestries. The repair itself happens in <see cref="BlockRepairableTapestry"/>.
/// </summary>
public class ItemTemporalSewingKit : Item
{
    public const string KitCode = "nf3ttapestryrepair:temporalsewingkit";
    public const int DurabilityPerRepair = 1;

    public static bool IsKit(ItemSlot slot) =>
        !slot.Empty && slot.Itemstack.Collectible is ItemTemporalSewingKit;

    // The tooltip text is the item's itemdesc-temporalsewingkit lang entry, which the base class shows.
}
