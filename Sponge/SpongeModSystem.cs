#nullable enable

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using ProtoBuf;

namespace Nf3t.VintageStory.Sponge;

public class SpongeConfig
{
    // 1 => 3x3x3, 2 => 5x5x5, etc.
    public int AbsorbRadius = 1;
}

[ProtoContract]
public class SpongeFxPacket
{
    [ProtoMember(1)] public int Dimension { get; set; }

    [ProtoMember(2)] public double X { get; set; }
    [ProtoMember(3)] public double Y { get; set; }
    [ProtoMember(4)] public double Z { get; set; }

    // If you keep normals:
    [ProtoMember(5)] public float Nx { get; set; }
    [ProtoMember(6)] public float Ny { get; set; }
    [ProtoMember(7)] public float Nz { get; set; }
}

public class SpongeModSystem : ModSystem
{
    public static SpongeConfig? Config;
    
    private static ICoreClientAPI? _clientApi;
    private static IClientNetworkChannel? _clientChannel;
    private static IServerNetworkChannel? _serverChannel;
    
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterItemClass("ItemSponge", typeof(ItemSponge));
        
        // Register channel + message type on BOTH sides in Start()
        var ch = api.Network.RegisterChannel("nf3tsponge")
            .RegisterMessageType<SpongeFxPacket>();

        if (api.Side == EnumAppSide.Client)
        {
            _clientChannel = ch as IClientNetworkChannel;
            _clientChannel?.SetMessageHandler<SpongeFxPacket>(OnFxPacketClient);
        }
        else
        {
            _serverChannel = ch as IServerNetworkChannel;
        }
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        _clientApi = api;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        Config = api.LoadModConfig<SpongeConfig>("Nf3tSponge.json");
        if (Config != null) return;
        Config = new SpongeConfig();
        api.StoreModConfig(Config, "Nf3tSponge.json");
    }
    
    public static void SendFxToPlayer(EntityAgent byEntity, BlockSelection blockSel)
    {
        if (_serverChannel == null) return;
        if (byEntity is not EntityPlayer ep) return;
        if (ep.Player is not IServerPlayer sp) return;
        if (blockSel?.Position == null) return;

        // HitPosition can be (0,0,0) in some interaction cases.
        // Fallback to block center so we never send zeros.
        var hitOffset = blockSel.HitPosition;
        if (hitOffset == null || (hitOffset.X == 0 && hitOffset.Y == 0 && hitOffset.Z == 0))
        {
            hitOffset = new Vec3d(0.5, 0.5, 0.5);
        }

        var hit = blockSel.Position.ToVec3d().Add(hitOffset);

        ep.World.Api.Logger.Notification(
            $"[nf3tsponge] SendFxToPlayer pos={blockSel.Position.X},{blockSel.Position.Y},{blockSel.Position.Z} " +
            $"hitOffset={hitOffset.X:F3},{hitOffset.Y:F3},{hitOffset.Z:F3} hit={hit.X:F3},{hit.Y:F3},{hit.Z:F3}"
        );

        _serverChannel.SendPacket(new SpongeFxPacket
        {
            Dimension = blockSel.Position.dimension,
            X = hit.X,
            Y = hit.Y,
            Z = hit.Z
        }, sp);
    }
    
    private static void OnFxPacketClient(SpongeFxPacket packet)
    {
        if (_clientApi == null) return;

        var pos = new Vec3d(packet.X, packet.Y, packet.Z);

        _clientApi.Logger.Notification($"SpongeFxPacket received at {packet.X}, {packet.Y}, {packet.Z} dim {packet.Dimension}");
        
        SpawnWaterDrops(_clientApi, pos);
    }

    private static void SpawnWaterDrops(ICoreClientAPI clientAPI, Vec3d pos)
    {
        // Blue-ish water
        var waterBlue = ColorUtil.ToRgba(200, 80, 140, 255);

        // Small "droplet" style: a bit upward + outward, then gravity pulls down
        clientAPI.World.SpawnParticles(
            quantity: 18,
            color: waterBlue,
            minPos: pos.AddCopy(-0.25, 0.00, -0.25),
            maxPos: pos.AddCopy( 0.25, 0.05,  0.25),
            minVelocity: new Vec3f(-0.25f, 0.15f, -0.25f),
            maxVelocity: new Vec3f( 0.25f, 0.55f,  0.25f),
            lifeLength: 0.35f,
            gravityEffect: 1.5f,
            scale: 0.7f,
            model: EnumParticleModel.Cube  // looks more like drops than quads
        );
    }
}