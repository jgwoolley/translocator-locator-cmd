#nullable enable

using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.TapestryRepair;

/// <summary>
/// The temporal loom's state: flax twine waiting to be woven, charge from temporal gears (the loom's power) and the temporal
/// fiber it has made. While it has both twine and charge it weaves one fiber per <see cref="SecondsPerFiber"/>.
/// </summary>
public class BlockEntityTemporalLoom : BlockEntity
{
    public const int MaxFlax = 64;
    public const int MaxFiber = 64;
    public const int MaxCharge = 64;
    public const int ChargePerGear = 16;
    public const float SecondsPerFiber = 3f;
    public const string FiberCode = "nf3ttapestryrepair:temporalfiber";

    /// <summary>The item this loom weaves; the unraveling loom overrides it.</summary>
    public virtual string OutputCode => FiberCode;

    public int Flax { get; private set; }
    public int Fiber { get; private set; }
    public int Charge { get; private set; }
    private float progress;

    public bool Working => Flax > 0 && Charge > 0 && Fiber < MaxFiber;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        if (api.Side == EnumAppSide.Server) RegisterGameTickListener(OnTick, 500);
    }

    private void OnTick(float dt)
    {
        if (!Working)
        {
            progress = 0;
            return;
        }

        // A long server lag must not weave a burst of fiber at once.
        progress += Math.Min(dt, 1f);
        var woven = false;
        while (progress >= SecondsPerFiber && Working)
        {
            progress -= SecondsPerFiber;
            Flax--;
            Charge--;
            Fiber++;
            woven = true;
        }

        if (!woven) return;
        MarkDirty();
        Api.World.PlaySoundAt(new AssetLocation("sounds/effect/gears"), Pos, 0.5, null, true, 12, 0.3f);
    }

    /// <summary>Adds up to <paramref name="amount"/> twine; returns how many fit.</summary>
    public int AddFlax(int amount)
    {
        var added = Math.Clamp(MaxFlax - Flax, 0, amount);
        if (added == 0) return 0;
        Flax += added;
        MarkDirty();
        return added;
    }

    public bool AddGear()
    {
        if (Charge + ChargePerGear > MaxCharge) return false;
        Charge += ChargePerGear;
        MarkDirty();
        return true;
    }

    /// <summary>Takes out all the finished fiber (null when there is none).</summary>
    public ItemStack? TakeFiber()
    {
        var item = Api.World.GetItem(new AssetLocation(OutputCode));
        if (item == null || Fiber == 0) return null;
        var stack = new ItemStack(item, Fiber);
        Fiber = 0;
        MarkDirty();
        return stack;
    }

    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);
        if (Api.Side != EnumAppSide.Server) return;

        var flax = Api.World.GetItem(new AssetLocation(BlockTemporalLoom.FlaxCode));
        if (flax != null && Flax > 0) Api.World.SpawnItemEntity(new ItemStack(flax, Flax), Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        var fiber = TakeFiber();
        if (fiber != null) Api.World.SpawnItemEntity(fiber, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        dsc.AppendLine(Lang.Get("nf3ttapestryrepair:loom-info-flax", Flax, MaxFlax));
        dsc.AppendLine(Lang.Get("nf3ttapestryrepair:loom-info-charge", Charge, MaxCharge));
        dsc.AppendLine(Lang.Get("nf3ttapestryrepair:loom-info-fiber", Fiber, MaxFiber));
        if (Working) dsc.AppendLine(Lang.Get("nf3ttapestryrepair:loom-info-working"));
        else if (Flax > 0 && Charge == 0) dsc.AppendLine(Lang.Get("nf3ttapestryrepair:loom-info-nopower"));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetInt("flax", Flax);
        tree.SetInt("fiber", Fiber);
        tree.SetInt("charge", Charge);
        tree.SetFloat("progress", progress);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        Flax = tree.GetInt("flax");
        Fiber = tree.GetInt("fiber");
        Charge = tree.GetInt("charge");
        progress = tree.GetFloat("progress");
    }
}
