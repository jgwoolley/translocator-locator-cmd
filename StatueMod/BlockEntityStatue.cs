using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Nf3t.VintageStory.Statue;

public class BlockEntityStatue : BlockEntity
{
    public const int EditPacketId = 1;
    /// <summary>The seraph is about 2 blocks tall at scale 1: small fits one block, large matches the Tobias statue.</summary>
    public const float SmallScale = 0.5f;
    public const float LargeScale = 1.75f;
    /// <summary>The head is only about 8 model pixels wide, so the bust is drawn bigger than life.</summary>
    public const float BustScale = 1.25f;
    public const string AttributesKey = "nf3tstatue";

    public string PlayerName = "";
    public string Emote = "";
    /// <summary>-1 picks the most expressive keyframe.</summary>
    public int Frame = -1;
    public StatueAppearance Appearance = new();
    public float MeshAngleY;

    private MeshData? mesh;
    private GuiDialogStatue? dialog;

    public bool IsConfigured => Emote != "";

    public static string[] EmotesOf(ICoreAPI api) =>
        api.World.GetEntityType(new AssetLocation("player"))?.Attributes?["emotes"]?.AsArray<string>()! ?? [];

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        WriteSettings(tree);
        tree.SetFloat("meshAngleY", MeshAngleY);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        ReadSettings(tree, worldForResolving);
        MeshAngleY = tree.GetFloat("meshAngleY");
        mesh = null;
    }

    private void WriteSettings(ITreeAttribute tree) => WriteSettings(tree, PlayerName, Emote, Frame, Appearance);

    public static void WriteSettings(ITreeAttribute tree, string playerName, string emote, int frame,
        StatueAppearance appearance)
    {
        tree.SetString("playerName", playerName);
        tree.SetString("emote", emote);
        tree.SetInt("frame", frame);
        appearance.Write(tree);
    }

    private void ReadSettings(ITreeAttribute tree, IWorldAccessor world)
    {
        PlayerName = tree.GetString("playerName", "");
        Emote = tree.GetString("emote", "");
        Frame = tree.GetInt("frame", -1);
        Appearance = StatueAppearance.Read(tree, world);
    }

    /// <summary>The settings travel with the item, so breaking and re-placing a statue keeps its pose.</summary>
    public void WriteToStack(ItemStack stack)
    {
        var tree = new TreeAttribute();
        WriteSettings(tree);
        stack.Attributes[AttributesKey] = tree;
    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        if (byItemStack?.Attributes[AttributesKey] is ITreeAttribute tree) ReadSettings(tree, Api.World);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        if (!IsConfigured || Api is not ICoreClientAPI capi) return false;

        mesh ??= StatueMesh.Build(capi, tessThreadTesselator, Block, Emote, Frame, Appearance,
            Block.Variant["size"] == "bust", ScaleOf(Block), MeshAngleY);
        if (mesh != null) mesher.AddMeshData(mesh);

        // The pedestal comes from the block's own shape.
        return false;
    }

    private static float ScaleOf(Block block) => block.Variant["size"] switch
    {
        "large" => LargeScale,
        "bust" => BustScale,
        _ => SmallScale,
    };

    public void OpenDialog()
    {
        if (Api is not ICoreClientAPI capi || dialog != null) return;

        dialog = new GuiDialogStatue(capi, Pos, Block, PlayerName, Emote, Frame, Appearance, MeshAngleY, EmotesOf(Api));
        dialog.OnClosed += () =>
        {
            dialog?.Dispose();
            dialog = null;
        };
        dialog.TryOpen();
    }

    public static byte[] EncodeEdit(string playerName, string emote, int frame, bool look, bool clothing, bool armor,
        StatueAppearance.HandMode rightHand, StatueAppearance.HandMode leftHand, float angleY)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(playerName);
            writer.Write(emote);
            writer.Write(frame);
            writer.Write(look);
            writer.Write(clothing);
            writer.Write(armor);
            writer.Write((byte)rightHand);
            writer.Write((byte)leftHand);
            writer.Write(angleY);
        }

        return stream.ToArray();
    }

    public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetId, byte[] data)
    {
        base.OnReceivedClientPacket(fromPlayer, packetId, data);
        if (packetId != EditPacketId || Api is not ICoreServerAPI sapi) return;
        if (!sapi.World.Claims.TryAccess(fromPlayer, Pos, EnumBlockAccessFlags.BuildOrBreak)) return;

        string playerName, emote;
        int frame;
        bool look, clothing, armor;
        StatueAppearance.HandMode rightHand, leftHand;
        float angleY;
        using (var reader = new BinaryReader(new MemoryStream(data)))
        {
            playerName = reader.ReadString().Trim();
            emote = reader.ReadString();
            frame = reader.ReadInt32();
            look = reader.ReadBoolean();
            clothing = reader.ReadBoolean();
            armor = reader.ReadBoolean();
            rightHand = (StatueAppearance.HandMode)reader.ReadByte();
            leftHand = (StatueAppearance.HandMode)reader.ReadByte();
            angleY = reader.ReadSingle();
        }

        if (float.IsFinite(angleY)) MeshAngleY = angleY;

        if (!System.Array.Exists(EmotesOf(sapi), e => e == emote)) return;

        var subject = StatueSkin.FindByName(sapi, playerName);
        PlayerName = subject?.PlayerName ?? playerName;
        Emote = emote;
        // Always keep a real frame number, so the statue shows the frame that was saved.
        Frame = StatueMesh.ResolveFrame(sapi, emote, frame, Block.Variant["size"] == "bust");
        var previous = Appearance;
        Appearance = subject == null
            ? new StatueAppearance { ShowLook = look, ShowClothing = clothing, ShowArmor = armor }
            : StatueAppearance.Capture(subject, look, clothing, armor);

        // The held items are copies of what the editing player holds: nothing is taken from them.
        var hands = fromPlayer.InventoryManager;
        Appearance.RightHand = StatueAppearance.PickHand(rightHand, previous.RightHand, hands.ActiveHotbarSlot);
        Appearance.LeftHand = StatueAppearance.PickHand(leftHand, previous.LeftHand, hands.OffhandHotbarSlot);

        if (subject == null && playerName != "" && fromPlayer is IServerPlayer serverPlayer)
        {
            serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup,
                Lang.GetL(serverPlayer.LanguageCode, "nf3tstatue:msg-offline", playerName),
                EnumChatType.Notification);
        }

        MarkDirty(true);
    }

    public override void GetBlockInfo(IPlayer forPlayer, System.Text.StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        dsc.AppendLine(IsConfigured
            ? Lang.Get("nf3tstatue:info-statue", PlayerName == "" ? "?" : PlayerName,
                Lang.Get("nf3tstatue:emote-" + Emote))
            : Lang.Get("nf3tstatue:info-blank"));
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        dialog?.TryClose();
    }
}
