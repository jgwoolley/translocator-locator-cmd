#nullable enable

using Vintagestory.API.Common;

namespace Nf3t.VintageStory.TapestryRepair;

/// <summary>
/// The inverse of <see cref="ItemTemporalSewingKit"/>: right-click a tapestry to make it rotten. It is crafted from decayed
/// fiber made at the <see cref="BlockDecayLoom"/>; the effect itself happens in <see cref="BlockRepairableTapestry"/>.
/// </summary>
public class ItemTemporalDecayKit : Item
{
    public const string KitCode = "nf3ttapestryrepair:temporaldecaykit";

    public static bool IsKit(ItemSlot slot) =>
        !slot.Empty && slot.Itemstack.Collectible is ItemTemporalDecayKit;
}
