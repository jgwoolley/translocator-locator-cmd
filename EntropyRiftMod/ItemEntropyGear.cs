#nullable enable

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.EntropyRift;

/// <summary>
/// The entropy gear: a temporal gear that drank a temporal storm. Like the temporal gear it turns slowly in the inventory, on
/// the ground and in the hand, but the other way round, and on the ground it sheds motes as the temporal gear does, in the
/// storm's orange and black instead of its cyan. It has no use of its own yet (see the mod's README).
/// </summary>
public class ItemEntropyGear : Item
{
    private SimpleParticleProperties? motes;

    private static float Angle(IWorldAccessor world) => GameMath.Mod(world.ElapsedMilliseconds / 50f, 360);

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        // The temporal gear's held/ground particles (ItemTemporalGear), colour aside.
        motes = new SimpleParticleProperties(1, 1, ColorUtil.ToRgba(50, 220, 220, 220), new Vec3d(), new Vec3d(),
            new Vec3f(-0.1f, -0.1f, -0.1f), new Vec3f(0.1f, 0.1f, 0.1f), 1.5f, 0, 0.5f, 0.75f, EnumParticleModel.Quad)
        {
            SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f),
            addLifeLength = 0.5f,
            RandomVelocityChange = true,
            MinSize = 0.2f,
            OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150),
        };
        motes.AddPos.Set(0.1, 0.1, 0.1);
    }

    /// <summary>Now and then one mote at <paramref name="pos"/>: mostly storm orange, sometimes near black.</summary>
    private void SpawnMote(IWorldAccessor world, Vec3d pos)
    {
        if (motes == null || world.Rand.NextDouble() <= 0.8) return;
        var orange = world.Rand.NextDouble() < 0.65;
        var hue = orange ? 10 + world.Rand.Next(12) : 0;          // ColorUtil hue 0-255: ~15 is orange
        var value = orange ? 170 + world.Rand.Next(70) : 15 + world.Rand.Next(25);
        motes.MinPos = pos;
        motes.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(hue, orange ? 230 : 60, value, 170));
        world.SpawnParticles(motes);
    }

    public override void InGuiIdle(IWorldAccessor world, ItemStack stack)
    {
        if (world is IClientWorldAccessor) GuiTransform.Rotation.Y = -Angle(world);
    }

    public override void OnGroundIdle(EntityItem entityItem)
    {
        base.OnGroundIdle(entityItem);
        if (entityItem.World is not IClientWorldAccessor) return;
        GroundTransform.Rotation.Y = Angle(entityItem.World);
        SpawnMote(entityItem.World, entityItem.Pos.XYZ);
    }

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
        base.OnHeldIdle(slot, byEntity);
        if (byEntity.World is not IClientWorldAccessor) return;
        FpHandTransform.Rotation.Y = Angle(byEntity.World);
        TpHandTransform.Rotation.Y = Angle(byEntity.World);
    }
}
