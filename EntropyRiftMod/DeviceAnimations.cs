#nullable enable

using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.EntropyRift;

/// <summary>
/// Client-side animations of a device whose state is its block variant (the gear in the cradle spins, the Entropy Rift's
/// tower turns). The animations live in each variant's shape; this plays the ones the current variant lists.
/// <para>
/// The block entity survives a variant change (<c>ExchangeBlock</c>), but the animator is built from one shape, so it is
/// rebuilt whenever the block at the position changes. That is noticed during tesselation (which runs after every block
/// change, on a worker thread) and done on the main thread, as the game requires once a renderer exists.
/// </para>
/// Requires the <c>Animatable</c> block entity behavior in the block JSON (it also hides the static mesh while animating).
/// </summary>
public class DeviceAnimations
{
    private readonly BlockEntity be;
    private readonly System.Func<Block, IEnumerable<string>> animationsFor;
    private readonly Dictionary<string, AnimationMetaData> playing = new();
    private string? builtFor;
    private float speed = 1;

    /// <param name="be">The device.</param>
    /// <param name="animationsFor">Animation codes to play for a block variant (none: the static model is shown).</param>
    public DeviceAnimations(BlockEntity be, System.Func<Block, IEnumerable<string>> animationsFor)
    {
        this.be = be;
        this.animationsFor = animationsFor;
    }

    private BlockEntityAnimationUtil? Util => be.GetBehavior<BEBehaviorAnimatable>()?.animUtil;

    /// <summary>Call from the block entity's <c>OnTesselation</c>.</summary>
    public void OnTesselation()
    {
        if (be.Api is not ICoreClientAPI capi) return;
        var code = capi.World.BlockAccessor.GetBlock(be.Pos).Code?.ToString();
        if (code == null || code == builtFor) return;
        builtFor = code;
        capi.Event.EnqueueMainThreadTask(Rebuild, "nf3tentropyrift-device-animation");
    }

    /// <summary>Re-plays the animations for the block's current state, after something the lookup depends on has changed (e.g. a
    /// siphon starting or stopping its charge). Runs on the main thread.</summary>
    public void Refresh()
    {
        if (be.Api is not ICoreClientAPI capi) return;
        capi.Event.EnqueueMainThreadTask(Rebuild, "nf3tentropyrift-device-animation-refresh");
    }

    /// <summary>Multiplies the speed of every playing animation (e.g. faster while the siphon charges).</summary>
    public void SetSpeed(float value)
    {
        speed = value;
        foreach (var meta in playing.Values) meta.AnimationSpeed = value;
    }

    private void Rebuild()
    {
        var util = Util;
        if (util == null || be.Api == null) return;
        var block = be.Api.World.BlockAccessor.GetBlock(be.Pos);

        foreach (var code in util.activeAnimationsByAnimCode.Keys.ToArray()) util.StopAnimation(code);
        playing.Clear();

        var codes = animationsFor(block).ToArray();
        if (codes.Length == 0) return;

        // One animator per variant: the cache key must differ, or the previous variant's shape would be reused.
        util.InitializeAnimator("nf3tentropyrift-" + block.Code.ToShortString());
        foreach (var code in codes)
        {
            var meta = new AnimationMetaData { Animation = code, Code = code, AnimationSpeed = speed, EaseInSpeed = 1, EaseOutSpeed = 1 };
            playing[code] = meta;
            util.StartAnimation(meta);
        }
    }
}
