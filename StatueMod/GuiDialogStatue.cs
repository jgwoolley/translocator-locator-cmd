using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.Statue;

public class GuiDialogStatue : GuiDialog
{
    private const int PlaybackFramesPerSecond = 30;
    private const int HotbarSlots = 10;

    private readonly BlockPos pos;
    private readonly Block block;
    private readonly bool bust;
    private readonly StatueAppearance savedAppearance;
    private string emote;
    private string mount;
    private string elkPose;
    private string playerName;
    private float angleY;
    /// <summary>The way the placed statue faces now. Picking an arrow only changes this once the statue is committed.</summary>
    private readonly float placedAngleY;
    /// <summary>How far the preview is turned from the statue's own facing. Preview only; never saved.</summary>
    private float viewAngle;

    /// <summary>Where each hand's item comes from: a hotbar slot, <see cref="StatueAppearance.HandEmpty"/> or <see cref="StatueAppearance.HandFromCopy"/>.</summary>
    private int rightHand = StatueAppearance.HandEmpty;
    private int leftHand = StatueAppearance.HandEmpty;
    private readonly string[] tack = StatueTack.Empty();
    private int antlers;
    private string statueType = StatueTypes.Player;
    private List<Entity> nearbyElks = [];
    private const float NearbyRange = 16;

    /// <summary>The finished statue in the hotbar whose design was copied into the dialog, if any.</summary>
    private StatueDesign? copied;
    private int copySlot = EditRequest.NoCopy;

    private string? appearanceKey;
    private StatueAppearance previewAppearance;
    private string? slotKey;
    private ItemSlot? previewSlot;

    public GuiDialogStatue(ICoreClientAPI capi, BlockPos pos, Block block, string playerName, string emote, int frame,
        StatueAppearance appearance, float angleY, string[] emotes) : base(capi)
    {
        this.pos = pos;
        // A blank is a plain cube, which would hide the statue, so the preview shows the finished statue block.
        this.block = (block as BlockStatueBlank)?.FinishedBlock(capi.World) ?? block;
        this.savedAppearance = appearance;
        this.previewAppearance = appearance;
        this.bust = block.Variant["size"] == "bust";
        this.emote = emote != "" ? emote : emotes.FirstOrDefault() ?? "";
        this.mount = bust ? StatueAppearance.NoMount : appearance.Mount;
        this.elkPose = appearance.ElkPose;
        this.antlers = appearance.Antlers;
        this.angleY = angleY;
        this.placedAngleY = angleY;
        if (playerName == "") playerName = capi.World.Player.PlayerName;
        this.playerName = playerName;
        var players = PlayerChoices(playerName);
        if (frame < 0) frame = StatueMesh.PeakFrame(capi, this.emote, bust);
        var elkFrame = StatueMesh.ResolveGaitFrame(capi, elkPose, appearance.ElkFrame);

        var self = capi.World.Player;
        var (handValues, handNames) = HandChoices(self);
        var (copyValues, copyNames) = CopyChoices(self);
        var (nearbyValues, nearbyNames) = NearbyChoices();
        var typeValues = StatueTypes.All;
        var typeNames = typeValues.Select(t => Lang.Get(StatueTypes.LangKey(t))).ToArray();
        var antlerValues = Enumerable.Range(0, StatueAppearance.MaxAntlers + 1).Select(i => i.ToString()).ToArray();
        var antlerNames = antlerValues.Select(v => Lang.Get(v == "0" ? "nf3tstatue:antlers-none"
            : v == "1" ? "nf3tstatue:antlers-smallest" : v == StatueAppearance.MaxAntlers.ToString() ? "nf3tstatue:antlers-biggest"
            : "nf3tstatue:antlers-size", v)).ToArray();
        const double width = 320;
        const double previewX = 340;
        var label = CairoFont.WhiteSmallText();
        var typeLabel = ElementBounds.Fixed(0, 30, width, 20);
        var typeInput = ElementBounds.Fixed(0, 52, width, 30);
        var playerLabel = ElementBounds.Fixed(0, 82, width, 20);
        var playerInput = ElementBounds.Fixed(0, 104, width, 30);
        var emoteLabel = ElementBounds.Fixed(0, 146, width, 20);
        var emoteInput = ElementBounds.Fixed(0, 168, width, 30);
        var frameLabel = ElementBounds.Fixed(0, 210, width, 20);
        var frameInput = ElementBounds.Fixed(0, 232, 200, 30);
        var bestFrame = ElementBounds.Fixed(210, 232, 110, 30);
        var layersLabel = ElementBounds.Fixed(0, 274, width, 20);
        var lookSwitch = ElementBounds.Fixed(0, 300, 25, 25);
        var clothingSwitch = ElementBounds.Fixed(0, 332, 25, 25);
        var armorSwitch = ElementBounds.Fixed(0, 364, 25, 25);
        var preview = ElementBounds.Fixed(previewX, 30, 220, 180);
        // Under the preview: the placed statue's facing, then two rows of < > turn buttons with the compass direction
        // between them: one for the saved facing, one that only turns the preview.
        var facingPlaced = ElementBounds.Fixed(previewX, 218, 220, 22);
        var turnLabel = ElementBounds.Fixed(previewX, 246, 220, 20);
        var turnLeft = ElementBounds.Fixed(previewX, 268, 40, 28);
        var turnName = ElementBounds.Fixed(previewX + 46, 273, 128, 22);
        var turnRight = ElementBounds.Fixed(previewX + 180, 268, 40, 28);
        var viewLabel = ElementBounds.Fixed(previewX, 306, 220, 20);
        var viewLeft = ElementBounds.Fixed(previewX, 328, 40, 28);
        var viewName = ElementBounds.Fixed(previewX + 46, 333, 128, 22);
        var viewRight = ElementBounds.Fixed(previewX + 180, 328, 40, 28);
        var pedestalSwitch = ElementBounds.Fixed(previewX, 432, 25, 25);
        var spinSwitch = ElementBounds.Fixed(previewX, 372, 25, 25);
        var playSwitch = ElementBounds.Fixed(previewX, 402, 25, 25);
        // A third column: the design to copy, what the hands hold and what the elk wears.
        const double thirdX = 580;
        var copyLabel = ElementBounds.Fixed(thirdX, 30, 220, 20);
        var copyInput = ElementBounds.Fixed(thirdX, 52, 220, 30);
        var rightHandLabel = ElementBounds.Fixed(thirdX, 94, 220, 20);
        var rightHandInput = ElementBounds.Fixed(thirdX, 116, 220, 30);
        var leftHandLabel = ElementBounds.Fixed(thirdX, 158, 220, 20);
        var leftHandInput = ElementBounds.Fixed(thirdX, 180, 220, 30);
        var antlersLabel = ElementBounds.Fixed(thirdX, 430, 220, 20);
        var antlersInput = ElementBounds.Fixed(thirdX, 452, 220, 30);
        var warning = ElementBounds.Fixed(thirdX, 494, 220, 56);
        // Plain fixed bounds (no alignment), so the buttons are exactly where they are drawn.
        var save = ElementBounds.Fixed(thirdX, 554, 220, 32);
        var cancel = ElementBounds.Fixed(thirdX, 594, 220, 32);

        var background = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        background.BothSizing = ElementSizing.FitToChildren;
        var dialog = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

        // Standing first, then the emotes, then riding (a bust is only a head, so it cannot ride).
        var emoteChoices = emotes.Prepend(StatuePoses.Standing).Concat(bust ? [] : [StatuePoses.Riding]).ToArray();
        var names = emoteChoices.Select(e => Lang.Get("nf3tstatue:emote-" + e)).ToArray();
        var mountValues = new[] { StatueAppearance.NoMount, StatueAppearance.ElkCow, StatueAppearance.ElkBull };
        var mountNames = new[]
        {
            Lang.Get("nf3tstatue:mount-none"), Lang.Get("nf3tstatue:mount-elk-cow"), Lang.Get("nf3tstatue:mount-elk-bull"),
        };
        var elkPoseNames = StatuePoses.ElkPoses.Select(p => Lang.Get("nf3tstatue:elkpose-" + p)).ToArray();
        var elkPoseLabel = ElementBounds.Fixed(0, 462, width, 20);
        var elkPoseInput = ElementBounds.Fixed(0, 484, width, 30);
        var elkFrameLabel = ElementBounds.Fixed(0, 524, width, 20);
        var elkFrameInput = ElementBounds.Fixed(0, 546, 200, 30);
        var bestElkFrame = ElementBounds.Fixed(210, 546, 110, 30);
        var nearbyLabel = ElementBounds.Fixed(0, 584, width, 20);
        var nearbyInput = ElementBounds.Fixed(0, 606, 200, 30);
        var nearbyRefresh = ElementBounds.Fixed(210, 606, 110, 30);
        var mountLabel = ElementBounds.Fixed(0, 400, width, 20);
        var mountInput = ElementBounds.Fixed(0, 422, width, 30);
        var compo = capi.Gui.CreateCompo("nf3tstatue", dialog)
            .AddShadedDialogBG(background)
            .AddDialogTitleBar(Lang.Get("nf3tstatue:dialog-title"), () => TryClose())
            .BeginChildElements(background)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-type"), label, typeLabel)
            .AddDropDown(typeValues, typeNames, Math.Max(0, Array.IndexOf(typeValues, statueType)), OnTypeChanged, typeInput, "type")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-player"), label, playerLabel)
            .AddDropDown(players, players, Math.Max(0, Array.IndexOf(players, playerName)),
                (code, _) => this.playerName = code, playerInput, "player")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-emote"), label, emoteLabel)
            .AddDropDown(emoteChoices, names, Math.Max(0, Array.IndexOf(emoteChoices, this.emote)), OnEmoteChanged, emoteInput,
                "emote")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-frame"), label, frameLabel)
            .AddNumberInput(frameInput, null, CairoFont.WhiteSmallText(), "frame")
            .AddSmallButton(Lang.Get("nf3tstatue:dialog-best-frame"), OnBestFrame, bestFrame, EnumButtonStyle.Normal, "bestFrame")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-layers"), label, layersLabel)
            .AddSwitch(null, lookSwitch, "look", 25, 3)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-layer-look"), label, lookSwitch.RightCopy(10, 3).WithFixedWidth(width - 40))
            .AddSwitch(null, clothingSwitch, "clothing", 25, 3)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-layer-clothing"), label, clothingSwitch.RightCopy(10, 3).WithFixedWidth(width - 40))
            .AddSwitch(null, armorSwitch, "armor", 25, 3)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-layer-armor"), label, armorSwitch.RightCopy(10, 3).WithFixedWidth(width - 40));

        // A bust is only a head, so it has nothing to ride.
        if (!bust)
        {
            compo = compo.AddStaticText(Lang.Get("nf3tstatue:dialog-mount"), label, mountLabel)
                .AddDropDown(mountValues, mountNames, Math.Max(0, Array.IndexOf(mountValues, mount)),
                    OnMountChanged, mountInput, "mount")
                .AddStaticText(Lang.Get("nf3tstatue:dialog-elk-pose"), label, elkPoseLabel)
                .AddDropDown(StatuePoses.ElkPoses, elkPoseNames, Math.Max(0, Array.IndexOf(StatuePoses.ElkPoses, elkPose)),
                    OnElkPoseChanged, elkPoseInput, "elkPose")
                .AddStaticText(Lang.Get("nf3tstatue:dialog-elk-frame"), label, elkFrameLabel)
                .AddNumberInput(elkFrameInput, null, CairoFont.WhiteSmallText(), "elkFrame")
                .AddSmallButton(Lang.Get("nf3tstatue:dialog-best-frame"), OnBestElkFrame, bestElkFrame, EnumButtonStyle.Normal, "bestElkFrame")
                .AddStaticText(Lang.Get("nf3tstatue:dialog-nearby-elk"), label, nearbyLabel)
                .AddDropDown(nearbyValues, nearbyNames, 0, OnNearbyElkChanged, nearbyInput, "nearby")
                .AddSmallButton(Lang.Get("nf3tstatue:dialog-refresh"), OnRefreshNearby, nearbyRefresh)
                .AddStaticText(Lang.Get("nf3tstatue:dialog-antlers"), label, antlersLabel)
                .AddDropDown(antlerValues, antlerNames, Math.Clamp(appearance.Antlers, 0, StatueAppearance.MaxAntlers),
                    (code, _) => antlers = int.Parse(code), antlersInput, "antlers");

            for (var i = 0; i < StatueTack.PieceCount; i++)
            {
                var piece = (StatueTack.Piece)i;
                var types = StatueTack.Choices(piece);
                var y = 222 + i * 52;
                compo = compo.AddStaticText(Lang.Get("nf3tstatue:dialog-tack-" + piece.ToString().ToLowerInvariant()), label,
                        ElementBounds.Fixed(thirdX, y, 220, 20))
                    .AddDropDown(types, types.Select(t => Lang.Get(StatueTack.LangKey(piece, t), t.Replace("face", ""))).ToArray(),
                        0, (code, _) => tack[(int)piece] = code, ElementBounds.Fixed(thirdX, y + 22, 220, 30), "tack" + i);
            }
        }

        SingleComposer = compo
            .AddInset(preview, 4)
            .AddInteractiveElement(new GuiElementStatuePreview(capi, preview, PreviewSlot,
                () => Spinning), "preview")
            .AddDynamicText("", CairoFont.WhiteSmallishText(), facingPlaced, "facingPlaced")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-turn"), label, turnLabel)
            .AddSmallButton("<", () => Turn(-1), turnLeft)
            .AddDynamicText("", CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), turnName, "turnName")
            .AddSmallButton(">", () => Turn(1), turnRight)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-turn-view"), label, viewLabel)
            .AddSmallButton("<", () => TurnView(-1), viewLeft, EnumButtonStyle.Normal, "viewLeft")
            .AddDynamicText("", CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), viewName, "viewName")
            .AddSmallButton(">", () => TurnView(1), viewRight, EnumButtonStyle.Normal, "viewRight")
            .AddSwitch(_ => UpdateViewButtons(), spinSwitch, "spin", 25, 3)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-spin"), label, spinSwitch.RightCopy(10, 3).WithFixedWidth(width - 40))
            .AddSwitch(null, playSwitch, "play", 25, 3)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-play"), label, playSwitch.RightCopy(10, 3).WithFixedWidth(width - 40))
            .AddSwitch(null, pedestalSwitch, "noPedestal", 25, 3)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-no-pedestal"), label, pedestalSwitch.RightCopy(10, 3).WithFixedWidth(width - 40))
            .AddStaticText(Lang.Get("nf3tstatue:dialog-copy"), label, copyLabel)
            .AddDropDown(copyValues, copyNames, 0, OnCopyChanged, copyInput, "copy")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-right-hand"), label, rightHandLabel)
            .AddDropDown(handValues, handNames, 0, (code, _) => rightHand = int.Parse(code), rightHandInput, "rightHand")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-left-hand"), label, leftHandLabel)
            .AddDropDown(handValues, handNames, 0, (code, _) => leftHand = int.Parse(code), leftHandInput, "leftHand")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-final-warning"), CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor), warning)
            .AddSmallButton(Lang.Get("nf3tstatue:dialog-cancel"), OnCancel, cancel)
            .AddSmallButton(Lang.Get("nf3tstatue:dialog-commit"), OnCommit, save)
            .EndChildElements()
            .Compose();

        SingleComposer.GetSwitch("spin").On = true;
        UpdateControls();
        UpdateViewButtons();
        ShowFacing();
        SingleComposer.GetNumberInput("frame").SetValue(frame);
        SingleComposer.GetNumberInput("elkFrame")?.SetValue(elkFrame);
        SingleComposer.GetSwitch("look").On = appearance.ShowLook;
        SingleComposer.GetSwitch("clothing").On = appearance.ShowClothing;
        SingleComposer.GetSwitch("armor").On = appearance.ShowArmor;
        SingleComposer.GetSwitch("noPedestal").On = appearance.NoPedestal;
    }

    public override string ToggleKeyCombinationCode => null!;

    /// <summary>The players who can be picked: everyone online, plus the name the statue already has.</summary>
    private string[] PlayerChoices(string current)
    {
        var names = capi.World.AllOnlinePlayers.Select(p => p.PlayerName).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        if (!names.Contains(current, StringComparer.OrdinalIgnoreCase)) names.Insert(0, current);
        return names.ToArray();
    }

    /// <summary>Turns the statue one step to the left (-1) or right (+1), as seen from above; the new facing is saved with it.</summary>
    private bool Turn(int rightSteps)
    {
        angleY = StatueFacing.TurnedRight(angleY, rightSteps);
        ShowFacing();
        return true;
    }

    private bool Spinning => SingleComposer.GetSwitch("spin").On;

    /// <summary>
    /// The preview cannot be turned by hand while it turns by itself, so its buttons look (and are) disabled then.
    /// </summary>
    private void UpdateViewButtons()
    {
        SingleComposer.GetButton("viewLeft").Enabled = !Spinning;
        SingleComposer.GetButton("viewRight").Enabled = !Spinning;
    }

    /// <summary>Turns only the picture in the preview, the same way.</summary>
    private bool TurnView(int rightSteps)
    {
        if (Spinning) return true;

        viewAngle = StatueFacing.TurnedRight(viewAngle, rightSteps);
        ShowFacing();
        return true;
    }

    /// <summary>The angle the preview draws the statue at: its facing, plus the preview-only turn.</summary>
    private float ShownAngle => angleY + viewAngle;

    /// <summary>
    /// Says in words which way the placed statue faces now (this never changes while the dialog is open), which way
    /// it will face after saving, and which way the preview currently shows it.
    /// </summary>
    private void ShowFacing()
    {
        string Name(float angle) => Lang.Get("nf3tstatue:compass-" + StatueFacing.PointOf(angle));

        SingleComposer.GetDynamicText("facingPlaced").SetNewText(Lang.Get("nf3tstatue:dialog-facing-placed", Name(placedAngleY)));
        SingleComposer.GetDynamicText("turnName").SetNewText(Name(angleY));
        SingleComposer.GetDynamicText("viewName").SetNewText(Lang.Get("nf3tstatue:dialog-view-shows", Name(ShownAngle)));
    }

    /// <summary>The choices for a hand: nothing, the copied statue's item, or an item from one of the hotbar slots.</summary>
    private static (string[] Values, string[] Names) HandChoices(IPlayer player)
    {
        var values = new List<string> { StatueAppearance.HandEmpty.ToString(), StatueAppearance.HandFromCopy.ToString() };
        var names = new List<string> { Lang.Get("nf3tstatue:hand-empty"), Lang.Get("nf3tstatue:hand-copied") };
        var hotbar = player.InventoryManager.GetHotbarInventory();
        for (var i = 0; i < Math.Min(hotbar.Count, HotbarSlots); i++)
        {
            if (hotbar[i]?.Itemstack is not { } held) continue;

            values.Add(i.ToString());
            names.Add(Lang.Get("nf3tstatue:hand-slot", i + 1, held.GetName()));
        }

        return (values.ToArray(), names.ToArray());
    }

    /// <summary>"No copy", then every finished statue in the hotbar.</summary>
    private (string[] Values, string[] Names) CopyChoices(IPlayer player)
    {
        var values = new List<string> { EditRequest.NoCopy.ToString() };
        var names = new List<string> { Lang.Get("nf3tstatue:copy-none") };
        var hotbar = player.InventoryManager.GetHotbarInventory();
        for (var i = 0; i < Math.Min(hotbar.Count, HotbarSlots); i++)
        {
            if (StatueDesign.FromStack(hotbar[i]?.Itemstack, capi.World) is not { } design) continue;

            values.Add(i.ToString());
            names.Add(design.Type != StatueTypes.Player
                ? Lang.Get("nf3tstatue:copy-special", i + 1, Lang.Get(StatueTypes.LangKey(design.Type)))
                : Lang.Get("nf3tstatue:copy-slot", i + 1, design.PlayerName == "" ? "?" : design.PlayerName,
                    Lang.Get("nf3tstatue:emote-" + design.Emote)));
        }

        return (values.ToArray(), names.ToArray());
    }

    /// <summary>Takes every setting of a statue from the hotbar into the dialog, to be committed as it is or changed first.</summary>
    private void OnCopyChanged(string code, bool selected)
    {
        copySlot = int.Parse(code);
        var design = copySlot < 0 ? null : StatueDesign.FromStack(
            capi.World.Player.InventoryManager.GetHotbarInventory()[copySlot]?.Itemstack, capi.World);
        copied = design;
        if (design == null) return;

        // Copying a special statue switches to its type; copying a player statue switches back to the player type.
        statueType = design.Type;
        SingleComposer.GetDropDown("type").SetSelectedValue(statueType);
        UpdateControls();
        if (design.Type != StatueTypes.Player) return;

        var appearance = design.Appearance;
        playerName = design.PlayerName;
        var players = PlayerChoices(playerName);
        SingleComposer.GetDropDown("player").SetList(players, players);
        SingleComposer.GetDropDown("player").SetSelectedValue(playerName);

        emote = design.Emote;
        SingleComposer.GetDropDown("emote").SetSelectedValue(emote);
        SingleComposer.GetNumberInput("frame").SetValue(design.Frame < 0 ? StatueMesh.PeakFrame(capi, emote, bust) : design.Frame);
        SingleComposer.GetSwitch("look").On = appearance.ShowLook;
        SingleComposer.GetSwitch("clothing").On = appearance.ShowClothing;
        SingleComposer.GetSwitch("armor").On = appearance.ShowArmor;
        SingleComposer.GetSwitch("noPedestal").On = appearance.NoPedestal;

        rightHand = appearance.RightHand != null ? StatueAppearance.HandFromCopy : StatueAppearance.HandEmpty;
        leftHand = appearance.LeftHand != null ? StatueAppearance.HandFromCopy : StatueAppearance.HandEmpty;
        SingleComposer.GetDropDown("rightHand").SetSelectedValue(rightHand.ToString());
        SingleComposer.GetDropDown("leftHand").SetSelectedValue(leftHand.ToString());

        if (bust) return;

        mount = appearance.Mount;
        antlers = appearance.Antlers;
        SingleComposer.GetDropDown("antlers").SetSelectedValue(antlers.ToString());
        UpdateControls();
        elkPose = appearance.ElkPose;
        SingleComposer.GetDropDown("mount").SetSelectedValue(mount);
        SingleComposer.GetDropDown("elkPose").SetSelectedValue(elkPose);
        SingleComposer.GetNumberInput("elkFrame").SetValue(StatueMesh.ResolveGaitFrame(capi, elkPose, appearance.ElkFrame));
        for (var i = 0; i < StatueTack.PieceCount; i++)
        {
            tack[i] = appearance.Tack[i];
            SingleComposer.GetDropDown("tack" + i).SetSelectedValue(tack[i]);
        }
    }

    /// <summary>The item a hand will hold, as the server will pick it when the statue is committed.</summary>
    private ItemStack? HandStack(int choice, ItemStack? fromCopy) => choice switch
    {
        StatueAppearance.HandFromCopy => fromCopy,
        StatueAppearance.HandEmpty => null,
        _ => StatueAppearance.PickHotbar(capi.World.Player, choice),
    };

    private static bool IsElk(Entity entity) => entity.Alive && entity.Variant["gender"] != null && entity.Code.Path.Contains("elk");

    /// <summary>"Pick an elk", then the loaded elk near the player, closest first.</summary>
    private (string[] Values, string[] Names) NearbyChoices()
    {
        var at = capi.World.Player.Entity.Pos.XYZ;
        nearbyElks = capi.World.GetEntitiesAround(at, NearbyRange, NearbyRange, IsElk)
            .OrderBy(e => e.Pos.XYZ.DistanceTo(at)).Take(20).ToList();

        var values = new List<string> { "-1" };
        var names = new List<string> { Lang.Get(nearbyElks.Count == 0 ? "nf3tstatue:nearby-none" : "nf3tstatue:nearby-pick") };
        for (var i = 0; i < nearbyElks.Count; i++)
        {
            values.Add(i.ToString());
            names.Add(Lang.Get("nf3tstatue:nearby-elk",
                Lang.Get(nearbyElks[i].Variant["gender"] == "male" ? "nf3tstatue:elk-bull" : "nf3tstatue:elk-cow"),
                (int)nearbyElks[i].Pos.XYZ.DistanceTo(at)));
        }

        return (values.ToArray(), names.ToArray());
    }

    private bool OnRefreshNearby()
    {
        var (values, names) = NearbyChoices();
        var list = SingleComposer.GetDropDown("nearby");
        list.SetList(values, names);
        list.SetSelectedIndex(0);
        return true;
    }

    /// <summary>
    /// Fills in the elk (cow or bull), its antlers and its tack from a loaded elk. Only the elk is read here; the
    /// server still checks the names it receives against the same list as for anything else.
    /// </summary>
    private void OnNearbyElkChanged(string code, bool selected)
    {
        var index = int.Parse(code);
        if (index < 0 || index >= nearbyElks.Count) return;

        var elk = nearbyElks[index];
        mount = elk.Variant["gender"] == "male" ? StatueAppearance.ElkBull : StatueAppearance.ElkCow;
        SingleComposer.GetDropDown("mount").SetSelectedValue(mount);
        UpdateControls();

        // The antlers grow through eight sizes, which are the eight antler models; a cow or an elk without them has none.
        var worn = elk.GetBehavior<EntityBehaviorAttachable>()?.Inventory;
        for (var i = 0; i < StatueTack.PieceCount; i++) tack[i] = StatueTack.None;
        if (worn != null)
        {
            foreach (var slot in worn)
            {
                if (slot?.Itemstack?.Collectible.Code.Path is { } path && StatueTack.FromWearableCode(path) is { } found)
                    tack[(int)found.Piece] = found.Type;
            }
        }

        antlers = StatueAppearance.AntlersFromItemCode(
            elk.GetBehavior<EntityBehaviorAntlerGrowth>()?.Inventory[0]?.Itemstack?.Collectible.Code.Path);
        SingleComposer.GetDropDown("antlers").SetSelectedValue(antlers.ToString());
        for (var i = 0; i < StatueTack.PieceCount; i++) SingleComposer.GetDropDown("tack" + i).SetSelectedValue(tack[i]);
    }

    private void OnTypeChanged(string code, bool selected)
    {
        statueType = code;
        UpdateControls();
    }

    /// <summary>
    /// Greys out what does not apply: everything about a player for a special statue, and everything about the elk
    /// (and its tack and antlers) while there is no elk.
    /// </summary>
    private void UpdateControls()
    {
        var player = statueType == StatueTypes.Player;
        SingleComposer.GetDropDown("player").Enabled = player;
        SingleComposer.GetDropDown("emote").Enabled = player;
        SingleComposer.GetDropDown("rightHand").Enabled = player;
        SingleComposer.GetDropDown("leftHand").Enabled = player;
        SingleComposer.GetNumberInput("frame").Enabled = player;
        SingleComposer.GetButton("bestFrame").Enabled = player;
        foreach (var layer in new[] { "look", "clothing", "armor" }) SingleComposer.GetSwitch(layer).Enabled = player;
        if (bust) return;

        var elk = player && mount != StatueAppearance.NoMount;
        SingleComposer.GetDropDown("mount").Enabled = player;
        SingleComposer.GetDropDown("nearby").Enabled = player;
        SingleComposer.GetDropDown("elkPose").Enabled = elk;
        SingleComposer.GetNumberInput("elkFrame").Enabled = elk;
        SingleComposer.GetButton("bestElkFrame").Enabled = elk;
        for (var i = 0; i < StatueTack.PieceCount; i++) SingleComposer.GetDropDown("tack" + i).Enabled = elk;
        SingleComposer.GetDropDown("antlers").Enabled = elk && mount == StatueAppearance.ElkBull;
    }

    private bool HasElk => !bust && mount != StatueAppearance.NoMount;

    private int FrameCount => StatueMesh.FrameCount(capi, emote);

    private int GaitFrameCount => StatueMesh.GaitFrameCount(capi, elkPose);

    private int InputFrame => Math.Clamp((int)SingleComposer.GetNumberInput("frame").GetValue(), 0, FrameCount - 1);

    /// <summary>The elk's frame; a bust has no elk, so it has no such field.</summary>
    private int InputElkFrame =>
        Math.Clamp((int)(SingleComposer.GetNumberInput("elkFrame")?.GetValue() ?? 0), 0, GaitFrameCount - 1);

    private void OnEmoteChanged(string code, bool selected)
    {
        emote = code;
        OnBestFrame();
    }

    private void OnMountChanged(string code, bool selected)
    {
        mount = code;
        UpdateControls();
    }

    private void OnElkPoseChanged(string code, bool selected)
    {
        elkPose = code;
        OnBestElkFrame();
    }

    private bool OnBestFrame()
    {
        SingleComposer.GetNumberInput("frame").SetValue(StatueMesh.PeakFrame(capi, emote, bust));
        return true;
    }

    private bool OnBestElkFrame()
    {
        SingleComposer.GetNumberInput("elkFrame")?.SetValue(StatueMesh.GaitPeakFrame(capi, elkPose));
        return true;
    }

    /// <summary>
    /// The statue item the preview draws. It is only rebuilt when a setting changes, and while playing the frame
    /// follows the clock.
    /// </summary>
    private ItemSlot? PreviewSlot()
    {
        var playing = SingleComposer.GetSwitch("play").On;
        var tick = (int)(capi.ElapsedMilliseconds / 1000.0 * PlaybackFramesPerSecond);
        var frame = playing ? tick % FrameCount : InputFrame;
        var elkFrame = playing ? tick % GaitFrameCount : InputElkFrame;
        var name = playerName;
        var look = SingleComposer.GetSwitch("look").On;
        var clothing = SingleComposer.GetSwitch("clothing").On;
        var armor = SingleComposer.GetSwitch("armor").On;

        var tackKey = string.Join(';', tack);
        var noPedestal = SingleComposer.GetSwitch("noPedestal").On;
        var key = string.Join('|', noPedestal, statueType, emote, frame, name, look, clothing, armor, rightHand, leftHand, ShownAngle, mount, elkPose,
            elkFrame, tackKey, antlers, copySlot);
        if (key == slotKey) return previewSlot;
        slotKey = key;

        var appearanceKey = string.Join('|', noPedestal, statueType, name, look, clothing, armor, rightHand, leftHand, mount, elkPose, tackKey, antlers, copySlot);
        if (appearanceKey != this.appearanceKey) previewAppearance = PreviewAppearance(name, look, clothing, armor);
        this.appearanceKey = appearanceKey;

        previewAppearance.ElkFrame = elkFrame;

        var stack = new ItemStack(block);
        var tree = new TreeAttribute();
        BlockEntityStatue.WriteSettings(tree, name, emote, frame, previewAppearance);
        tree.SetFloat("previewAngle", ShownAngle);
        stack.Attributes[BlockEntityStatue.AttributesKey] = tree;
        return previewSlot = new DummySlot(stack);
    }

    /// <summary>
    /// What the preview wears. Only the player using the dialog can be read on the client, so any other name
    /// previews what the statue already has (the server copies the player's look when saving).
    /// </summary>
    private StatueAppearance PreviewAppearance(string name, bool look, bool clothing, bool armor)
    {
        var self = capi.World.Player;
        if (statueType != StatueTypes.Player)
            return new StatueAppearance { Type = statueType, NoPedestal = SingleComposer.GetSwitch("noPedestal").On };

        var appearance = copied != null && string.Equals(copied.PlayerName, name, StringComparison.OrdinalIgnoreCase)
            ? copied.Appearance.WithLayers(look, clothing, armor)
            : string.Equals(self.PlayerName, name, StringComparison.OrdinalIgnoreCase)
                ? StatueAppearance.Capture(self, look, clothing, armor)
                : savedAppearance.WithLayers(look, clothing, armor);

        appearance.Mount = mount;
        appearance.ElkPose = elkPose;
        appearance.Antlers = antlers;
        appearance.NoPedestal = SingleComposer.GetSwitch("noPedestal").On;
        appearance.Tack = mount == StatueAppearance.NoMount ? StatueTack.Empty() : (string[])tack.Clone();

        // The same choice the server will make when committing.
        appearance.RightHand = HandStack(rightHand, copied?.Appearance.RightHand);
        appearance.LeftHand = HandStack(leftHand, copied?.Appearance.LeftHand);
        return appearance;
    }

    private bool OnCancel()
    {
        TryClose();
        return true;
    }

    /// <summary>Sends the design to the server, which turns the block into the finished statue.</summary>
    private bool OnCommit()
    {
        var request = new EditRequest(playerName, emote, InputFrame, SingleComposer.GetSwitch("look").On,
            SingleComposer.GetSwitch("clothing").On, SingleComposer.GetSwitch("armor").On, rightHand, leftHand, angleY, mount,
            elkPose, InputElkFrame, (string[])tack.Clone(), copySlot, antlers, statueType, SingleComposer.GetSwitch("noPedestal").On);
        capi.Network.SendBlockEntityPacket(pos, BlockEntityStatue.EditPacketId, request.Encode());
        TryClose();
        return true;
    }
}
