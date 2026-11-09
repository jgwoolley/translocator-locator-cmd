using System.Linq;
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
    /// <summary>A rider on an elk is about 2.6 blocks tall at scale 1, so the scales are lower than for a standing statue.</summary>
    public const float RideSmallScale = 0.38f;
    public const float RideLargeScale = 1.3f;
    public const string NoEmote = StatuePoses.Standing;
    public const string AttributesKey = "nf3tstatue";

    public string PlayerName = "";
    public string Emote = "";
    /// <summary>-1 picks the most expressive keyframe.</summary>
    public int Frame = -1;
    public StatueAppearance Appearance = new();
    public float MeshAngleY;

    /// <summary>How much fire clay has been packed around the statue for a mold (0 for none).</summary>
    public int ClayLevel;

    private MeshData? mesh;
    private GuiDialogStatue? dialog;

    public bool IsConfigured => Emote != "" || Appearance.Type != StatueTypes.Player;

    public static string[] EmotesOf(ICoreAPI api) =>
        api.World.GetEntityType(new AssetLocation("player"))?.Attributes?["emotes"]?.AsArray<string>() is { } emotes ? emotes.Select(e => e!).ToArray() : [];

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        WriteSettings(tree);
        tree.SetFloat("meshAngleY", MeshAngleY);
        tree.SetInt("clayLevel", ClayLevel);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        ReadSettings(tree, worldForResolving);
        MeshAngleY = tree.GetFloat("meshAngleY");
        ClayLevel = tree.GetInt("clayLevel");
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

    public void ReadSettings(ITreeAttribute tree, IWorldAccessor world)
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
            Block.Variant["size"] == "bust", ScaleOf(Block, IsRiding), MeshAngleY);
        if (mesh != null) mesher.AddMeshData(mesh);
        if (mesh != null && ClayLevel > 0
            && StatueMesh.ClayFill(capi, tessThreadTesselator, Block, mesh, ClayLevel / (float)StatueMolding.ClayNeeded(Block.Variant["size"])) is { } clay)
            mesher.AddMeshData(clay);

        // A rider brings its own, larger platform. Otherwise the pedestal comes from the block's own shape.
        return mesh != null && (IsRiding || Appearance.NoPedestal);
    }

    /// <summary>Whether the statue is showing a riding pose (a bust never rides).</summary>
    private bool IsRiding => Block.Variant["size"] != "bust" && Appearance.Mount != StatueAppearance.NoMount;

    private static float ScaleOf(Block block, bool riding)
    {
        return block.Variant["size"] switch
        {
            "large" => riding ? RideLargeScale : LargeScale,
            "bust" => BustScale,
            _ => riding ? RideSmallScale : SmallScale,
        };
    }

    /// <summary>
    /// Packs one piece of fire clay in the player's hand around the statue, or, once it is full, hands over the mold with
    /// the statue's design and clears the clay. Returns whether the interaction was this one.
    /// </summary>
    public bool OnMoldInteract(IPlayer player)
    {
        if (Block.Variant["rock"] == null) return false;

        var size = Block.Variant["size"];
        var mold = Api.World.GetBlock(new AssetLocation(Block.Code.Domain, $"statuemold-{size}-fired"));
        var before = ClayLevel;
        var handled = StatueMolding.Interact(Api, player, Pos, Block, ref ClayLevel, StatueMolding.ClayNeeded(size), mold, WriteToStack);
        if (ClayLevel != before) MarkDirty(true);
        return handled;
    }

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

    /// <summary>
    /// Commits the design of a blank block: the block becomes the finished statue (which cannot be edited) and the
    /// chisel and hammer wear down. Everything is checked here again, because the client can send anything.
    /// </summary>
    private const double MaxCarveDistance = 8;

    public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetId, byte[] data)
    {
        base.OnReceivedClientPacket(fromPlayer, packetId, data);
        if (packetId != EditPacketId || Api is not ICoreServerAPI sapi) return;
        if (Block is not BlockStatueBlank blank || IsConfigured) return;
        if (!sapi.World.Claims.TryAccess(fromPlayer, Pos, EnumBlockAccessFlags.BuildOrBreak)) return;
        // The client can send this for any position, so the player has to be within reach of the block.
        if (fromPlayer.Entity.Pos.DistanceTo(Pos.ToVec3d().Add(0.5, 0.5, 0.5)) > MaxCarveDistance) return;
        if (!BlockStatueBlank.HasTools(fromPlayer))
        {
            Refuse(fromPlayer, "nf3tstatue:msg-commit-tools");
            return;
        }

        var request = EditRequest.Decode(data);
        var bust = Block.Variant["size"] == "bust";
        var special = request != null && request.StatueType != StatueTypes.Player;
        if (request == null || !StatueTypes.IsKnown(request.StatueType)
            || (!special && (!request.HasValidMount(bust)
                || (!StatuePoses.IsKnown(request.Emote) && !System.Array.Exists(EmotesOf(sapi), e => e == request.Emote))
                || !StatuePoses.IsKnownElkPose(request.ElkPose)))
            || blank.FinishedBlock(sapi.World) is not { } finished)
        {
            sapi.Logger.Warning("[nf3tstatue] Refused a statue design from {0}: {1}", fromPlayer.PlayerName, request);
            Refuse(fromPlayer, "nf3tstatue:msg-commit-invalid");
            return;
        }

        var source = CopySource(fromPlayer, request.CopySlot, sapi.World);
        if (request.CopySlot != EditRequest.NoCopy && source == null)
        {
            Refuse(fromPlayer, "nf3tstatue:msg-commit-copy");
            return;
        }

        var subject = special ? null : StatueSkin.FindByName(sapi, request.PlayerName);
        var playerName = special ? "" : subject?.PlayerName ?? request.PlayerName;

        // A copied statue brings its own layers (its player may be offline); naming another player takes theirs.
        var useSource = !special && source != null && string.Equals(source.PlayerName, playerName, System.StringComparison.OrdinalIgnoreCase);
        var appearance = special ? new StatueAppearance { Type = request.StatueType }
            : useSource ? source!.Appearance.WithLayers(request.Look, request.Clothing, request.Armor)
            : subject == null
                ? new StatueAppearance { ShowLook = request.Look, ShowClothing = request.Clothing, ShowArmor = request.Armor }
                : StatueAppearance.Capture(subject, request.Look, request.Clothing, request.Armor);

        appearance.NoPedestal = request.NoPedestal;
        var emote = request.Emote;
        var frame = -1;
        if (!special)
        {
            appearance.Mount = request.Mount;
            appearance.ElkPose = request.ElkPose;
            appearance.ElkFrame = StatueMesh.ResolveGaitFrame(sapi, request.ElkPose, request.ElkFrame);
            appearance.Antlers = request.Antlers;
            appearance.Tack = bust || request.Mount == StatueAppearance.NoMount ? StatueTack.Empty() : request.Tack;
            appearance.RightHand = PickHand(fromPlayer, request.RightHand, source?.Appearance.RightHand);
            appearance.LeftHand = PickHand(fromPlayer, request.LeftHand, source?.Appearance.LeftHand);

            // Always keep a real frame number, so the statue shows the frame that was saved.
            frame = StatueMesh.ResolveFrame(sapi, request.Emote, request.Frame, bust);
        }
        else
        {
            emote = "";
        }

        var angle = float.IsFinite(request.AngleY) ? request.AngleY : MeshAngleY;

        var stack = new ItemStack(finished);
        var design = new TreeAttribute();
        WriteSettings(design, playerName, emote, frame, appearance);
        stack.Attributes[AttributesKey] = design;

        var pos = Pos.Copy();
        sapi.World.BlockAccessor.SetBlock(finished.BlockId, pos, stack);
        if (sapi.World.BlockAccessor.GetBlockEntity(pos) is not BlockEntityStatue statue) return;

        // The block entity may have been created without the item, so make sure the design is on it.
        if (!statue.IsConfigured) statue.ReadSettings(design, sapi.World);
        statue.MeshAngleY = angle;
        statue.MarkDirty(true);

        sapi.World.PlaySoundAt(new AssetLocation("sounds/player/chisel"), pos, 0, fromPlayer, true, 32f, 1f);
        BlockStatueBlank.WearTools(sapi.World, fromPlayer);

        if (subject == null && playerName != "" && !useSource && fromPlayer is IServerPlayer serverPlayer)
        {
            serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup,
                Lang.GetL(serverPlayer.LanguageCode, "nf3tstatue:msg-offline", playerName),
                EnumChatType.Notification);
        }
    }

    /// <summary>Tells the player why the statue was not carved, instead of silently doing nothing.</summary>
    private static void Refuse(IPlayer player, string langKey)
    {
        if (player is IServerPlayer serverPlayer)
            serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.GetL(serverPlayer.LanguageCode, langKey), EnumChatType.Notification);
    }

    /// <summary>The held item a hand gets: a copy from a hotbar slot, the copied statue's own item, or nothing.</summary>
    private static ItemStack? PickHand(IPlayer player, int choice, ItemStack? fromCopy) => choice switch
    {
        StatueAppearance.HandFromCopy => fromCopy?.Clone(),
        StatueAppearance.HandEmpty => null,
        _ => StatueAppearance.PickHotbar(player, choice),
    };

    /// <summary>A finished statue in the sender's hotbar whose design is copied, read on the server.</summary>
    public static StatueDesign? CopySource(IPlayer player, int slot, IWorldAccessor world)
    {
        if (slot < 0) return null;

        var stack = player.InventoryManager?.GetHotbarInventory() is { } hotbar && slot < hotbar.Count ? hotbar[slot]?.Itemstack : null;
        return StatueDesign.FromStack(stack, world);
    }

    public override void GetBlockInfo(IPlayer forPlayer, System.Text.StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        if (Appearance.Type != StatueTypes.Player)
        {
            dsc.AppendLine(Lang.Get(StatueTypes.LangKey(Appearance.Type)));
            return;
        }

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
