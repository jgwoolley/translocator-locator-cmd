#nullable enable

namespace Nf3t.VintageStory.TapestryRepair;

/// <summary>
/// The unraveling loom: the temporal loom's inverse. It runs on rusty gears and weaves flax twine into decayed fiber,
/// which is crafted into temporal decay kits. All the behaviour is <see cref="BlockTemporalLoom"/>'s.
/// </summary>
public class BlockDecayLoom : BlockTemporalLoom
{
    public const string RustyGearCode = "game:gear-rusty";

    protected override string PowerCode => RustyGearCode;
}
