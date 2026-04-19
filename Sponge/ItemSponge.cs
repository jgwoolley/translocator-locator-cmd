#nullable enable
using System;
using System.Linq;
using System.Text.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Sponge;

public class ItemSponge : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            var world = byEntity.World;
            var state = slot.Itemstack?.ItemAttributes?["spongeState"]?.AsString("dry") ?? "dry";
            
            if (world.Api.Side != EnumAppSide.Server)
            {
                handHandling = EnumHandHandling.PreventDefault;
                MakeSplashClient(world, blockSel);
                return;
            }
            
            if (state == "wet")
            {
                // Wring out: convert to dry sponge
                if (TrySetSponge(slot, world, "nf3tsponge:sponge-dry"))
                {
                    handHandling = EnumHandHandling.PreventDefault;
                    MakeSplashServer(world, blockSel);
                }
                return;
            }

            // Dry sponge: must have a target block position
            if (blockSel == null)
            {
                world.Api.Logger.Warning("Could not get BlockSelection");
                return;
            }

            var radius = SpongeModSystem.Config?.AbsorbRadius ?? 1;
            radius = GameMath.Clamp(radius, 0, 16);
            
            var removed = AbsorbSourceWater(world, blockSel.Position, radius);
            
            if (removed <= 0)
            {
                handHandling = EnumHandHandling.PreventDefault;
                return;
            }
            
            // Become wet after successful use
            if (TrySetSponge(slot, world, "nf3tsponge:sponge-wet"))
            {
                MakeSplashServer(world, blockSel);
            }
        }

        /// <summary>
        ///     Particles only work on the client of the Player who used the item
        /// </summary>
        /// <param name="world"></param>
        /// <param name="blockSel"></param>
        private static void MakeSplashClient(IWorldAccessor world, BlockSelection blockSel)
        {
            if (blockSel == null) return;
            
            var clientApi = SpongeModSystem.ClientAPI;
            if (clientApi == null)
            {
                world.Logger.Warning("Could not get ClientAPI");
                return;
            }
            
            var x = blockSel.Position.X + 0.5;
            var y = blockSel.Position.Y + 0.8;
            var z = blockSel.Position.Z + 0.5;
            
            var props = new SimpleParticleProperties(
                minQuantity: 6,
                maxQuantity: 12,
                color: ColorUtil.ToRgba(220, 140, 170, 255), // pink-ish for debugging visibility
                minPos: new Vec3d(x - 0.25, y, z - 0.25),
                maxPos: new Vec3d(x + 0.25, y + 0.15, z + 0.25),
                minVelocity: new Vec3f(-0.6f, 0.8f, -0.6f),
                maxVelocity: new Vec3f(0.6f, 1.5f, 0.6f),
                lifeLength: 1.0f,
                gravityEffect: 1.0f,
                minSize: 0.12f,
                maxSize: 0.25f,
                model: EnumParticleModel.Quad
            );
            
            // These evolvers are what often make it actually visible
            props.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -120f);
            props.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.8f);

            props.AddPos.Set(0, 0, 0);
            props.VertexFlags = 0;
            /*
            world.Logger.Debug($"Tried to make particle at {x}, {y}, {z}");
            clientApi.World.SpawnParticles(props);
            clientApi.World.SpawnCubeParticles(
                blockPos: clientApi.World.Player.Entity.Pos.AsBlockPos,
                pos: clientApi.World.Player.Entity.Pos.XYZ, 
                radius: 0.5f,
                quantity: 50);
            world.SpawnParticles(props);
            */
            world.SpawnCubeParticles(
                blockPos: clientApi.World.Player.Entity.Pos.AsBlockPos,
                pos: clientApi.World.Player.Entity.Pos.XYZ, 
                radius: 0.5f,
                quantity: 50);
        }
        
        private static void MakeSplashServer(IWorldAccessor world, BlockSelection blockSel)
        {
            if (blockSel == null) return;
            var x = blockSel.Position.X;
            var y = blockSel.Position.Y;
            var z = blockSel.Position.Z;
            
            var splashSoundAssetLocation = new AssetLocation("game:sounds/environment/smallsplash");
            world.PlaySoundAt(splashSoundAssetLocation, x + 0.5, y + 0.5, z + 0.5, range: 16f);
        }
        
        private static int AbsorbSourceWater(IWorldAccessor world, BlockPos center, int radius)
        {
            var removed = 0;
            
            var tmp = new BlockPos(center.dimension);

            var ba = world.BlockAccessor;
            
            for (var dx = -radius; dx <= radius; dx++)
            for (var dy = -radius; dy <= radius; dy++)
            for (var dz = -radius; dz <= radius; dz++)
            {
                tmp.Set(center.X + dx, center.Y + dy, center.Z + dz);
                
                var b = ba.GetBlock(tmp, BlockLayersAccess.Fluid);
                if (!IsSourceWater(b)) continue;

                ba.SetBlock(0, tmp, BlockLayersAccess.Fluid);
                ba.TriggerNeighbourBlockUpdate(tmp); // helps liquid reflow/update
                
                removed++;
            }

            ba.Commit();
            
            return removed;
        }

        private static bool IsSourceWater(Block block)
        {
            if (block == null) return false;

            var liquid = block.LiquidCode;
            if (string.IsNullOrEmpty(liquid)) return false;

            return liquid.Equals("water", StringComparison.OrdinalIgnoreCase)
                   || liquid.Equals("saltwater", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TrySetSponge(ItemSlot slot, IWorldAccessor world, string assetCode)
        {
            var spongeItem = world.GetItem(new AssetLocation(assetCode));
            if (spongeItem == null) return false;

            slot.Itemstack = new ItemStack(spongeItem);
            slot.MarkDirty();
            return true;
        }
    }