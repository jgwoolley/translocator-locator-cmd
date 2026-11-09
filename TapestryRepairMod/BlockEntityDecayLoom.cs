#nullable enable

namespace Nf3t.VintageStory.TapestryRepair;

/// <summary>The unraveling loom's state: the same machine as <see cref="BlockEntityTemporalLoom"/>, but it weaves decayed fiber.</summary>
public class BlockEntityDecayLoom : BlockEntityTemporalLoom
{
    public const string DecayedFiberCode = "nf3ttapestryrepair:decayedfiber";

    public override string OutputCode => DecayedFiberCode;
}
