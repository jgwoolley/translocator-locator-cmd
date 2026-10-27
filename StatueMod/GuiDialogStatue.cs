using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Statue;

public class GuiDialogStatue : GuiDialog
{
    private const int PlaybackFramesPerSecond = 30;

    private readonly BlockPos pos;
    private readonly Block block;
    private readonly bool bust;
    private readonly StatueAppearance savedAppearance;
    private string emote;
    private string mount;
    private string elkPose;
    private string playerName;
    private float angleY;
    /// <summary>The way the placed statue faces now. Picking an arrow only changes this once Save is pressed.</summary>
    private readonly float placedAngleY;
    /// <summary>How far the preview is turned from the statue's own facing. Preview only; never saved.</summary>
    private float viewAngle;

    private StatueAppearance.HandMode rightHandMode;
    private StatueAppearance.HandMode leftHandMode;

    private (string Name, bool Look, bool Clothing, bool Armor, StatueAppearance.HandMode Right, StatueAppearance.HandMode Left, string Mount, string ElkPose)? appearanceKey;
    private StatueAppearance previewAppearance;
    private (string Emote, int Frame, string Name, bool Look, bool Clothing, bool Armor, StatueAppearance.HandMode Right, StatueAppearance.HandMode Left, float AngleY, string Mount, string ElkPose, int ElkFrame)? slotKey;
    private ItemSlot? previewSlot;

    public GuiDialogStatue(ICoreClientAPI capi, BlockPos pos, Block block, string playerName, string emote, int frame,
        StatueAppearance appearance, float angleY, string[] emotes) : base(capi)
    {
        this.pos = pos;
        this.block = block;
        this.savedAppearance = appearance;
        this.previewAppearance = appearance;
        this.bust = block.Variant["size"] == "bust";
        this.emote = emote != "" ? emote : emotes.FirstOrDefault() ?? "";
        this.mount = bust ? StatueAppearance.NoMount : appearance.Mount;
        this.elkPose = appearance.ElkPose;
        this.angleY = angleY;
        this.placedAngleY = angleY;
        if (playerName == "") playerName = capi.World.Player.PlayerName;
        this.playerName = playerName;
        var players = PlayerChoices(playerName);
        if (frame < 0) frame = StatueMesh.PeakFrame(capi, this.emote, bust);
        var elkFrame = StatueMesh.ResolveGaitFrame(capi, elkPose, appearance.ElkFrame);

        var self = capi.World.Player;
        var handValues = new[] { "keep", "copy", "empty" };
        const double width = 320;
        const double previewX = 340;
        var label = CairoFont.WhiteSmallText();
        var playerLabel = ElementBounds.Fixed(0, 30, width, 20);
        var playerInput = ElementBounds.Fixed(0, 52, width, 30);
        var emoteLabel = ElementBounds.Fixed(0, 94, width, 20);
        var emoteInput = ElementBounds.Fixed(0, 116, width, 30);
        var frameLabel = ElementBounds.Fixed(0, 158, width, 20);
        var frameInput = ElementBounds.Fixed(0, 180, 200, 30);
        var bestFrame = ElementBounds.Fixed(210, 180, 110, 30);
        var layersLabel = ElementBounds.Fixed(0, 222, width, 20);
        var lookSwitch = ElementBounds.Fixed(0, 248, 25, 25);
        var clothingSwitch = ElementBounds.Fixed(0, 280, 25, 25);
        var armorSwitch = ElementBounds.Fixed(0, 312, 25, 25);
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
        var spinSwitch = ElementBounds.Fixed(previewX, 372, 25, 25);
        var playSwitch = ElementBounds.Fixed(previewX, 402, 25, 25);
        var rightHandLabel = ElementBounds.Fixed(previewX, 438, 220, 20);
        var rightHandInput = ElementBounds.Fixed(previewX, 460, 220, 30);
        var leftHandLabel = ElementBounds.Fixed(previewX, 498, 220, 20);
        var leftHandInput = ElementBounds.Fixed(previewX, 520, 220, 30);
        var cancel = ElementBounds.Fixed(0, 564, 0, 0).WithFixedPadding(10, 2);
        var save = ElementBounds.Fixed(0, 564, 0, 0).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10, 2);

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
        var elkPoseLabel = ElementBounds.Fixed(0, 410, width, 20);
        var elkPoseInput = ElementBounds.Fixed(0, 432, width, 30);
        var elkFrameLabel = ElementBounds.Fixed(0, 472, width, 20);
        var elkFrameInput = ElementBounds.Fixed(0, 494, 200, 30);
        var bestElkFrame = ElementBounds.Fixed(210, 494, 110, 30);
        var mountLabel = ElementBounds.Fixed(0, 348, width, 20);
        var mountInput = ElementBounds.Fixed(0, 370, width, 30);
        var compo = capi.Gui.CreateCompo("nf3tstatue", dialog)
            .AddShadedDialogBG(background)
            .AddDialogTitleBar(Lang.Get("nf3tstatue:dialog-title"), () => TryClose())
            .BeginChildElements(background)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-player"), label, playerLabel)
            .AddDropDown(players, players, Math.Max(0, Array.IndexOf(players, playerName)),
                (code, _) => this.playerName = code, playerInput, "player")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-emote"), label, emoteLabel)
            .AddDropDown(emoteChoices, names, Math.Max(0, Array.IndexOf(emoteChoices, this.emote)), OnEmoteChanged, emoteInput,
                "emote")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-frame"), label, frameLabel)
            .AddNumberInput(frameInput, null, CairoFont.WhiteSmallText(), "frame")
            .AddSmallButton(Lang.Get("nf3tstatue:dialog-best-frame"), OnBestFrame, bestFrame)
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
                .AddSmallButton(Lang.Get("nf3tstatue:dialog-best-frame"), OnBestElkFrame, bestElkFrame);
        }

        SingleComposer = compo
            .AddInset(preview, 4)
            .AddInteractiveElement(new GuiElementStatuePreview(capi, preview, PreviewSlot,
                () => Spinning), "preview")
            .AddDynamicText("", CairoFont.WhiteSmallishText(), facingPlaced, "facingPlaced")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-turn"), label, turnLabel)
            .AddSmallButton("<", () => Turn(-1), turnLeft)
            .AddDynamicText("", CairoFont.WhiteSmallishText(), EnumTextOrientation.Center, turnName, "turnName")
            .AddSmallButton(">", () => Turn(1), turnRight)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-turn-view"), label, viewLabel)
            .AddSmallButton("<", () => TurnView(-1), viewLeft, EnumButtonStyle.Normal, "viewLeft")
            .AddDynamicText("", CairoFont.WhiteSmallishText(), EnumTextOrientation.Center, viewName, "viewName")
            .AddSmallButton(">", () => TurnView(1), viewRight, EnumButtonStyle.Normal, "viewRight")
            .AddSwitch(_ => UpdateViewButtons(), spinSwitch, "spin", 25, 3)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-spin"), label, spinSwitch.RightCopy(10, 3).WithFixedWidth(width - 40))
            .AddSwitch(null, playSwitch, "play", 25, 3)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-play"), label, playSwitch.RightCopy(10, 3).WithFixedWidth(width - 40))
            .AddStaticText(Lang.Get("nf3tstatue:dialog-right-hand"), label, rightHandLabel)
            .AddDropDown(handValues, HandNames(appearance.RightHand, self.InventoryManager.ActiveHotbarSlot.Itemstack), 0,
                (code, _) => rightHandMode = ParseHand(code), rightHandInput, "rightHand")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-left-hand"), label, leftHandLabel)
            .AddDropDown(handValues, HandNames(appearance.LeftHand, self.InventoryManager.OffhandHotbarSlot.Itemstack), 0,
                (code, _) => leftHandMode = ParseHand(code), leftHandInput, "leftHand")
            .AddSmallButton(Lang.Get("nf3tstatue:dialog-cancel"), OnCancel, cancel)
            .AddSmallButton(Lang.Get("nf3tstatue:dialog-save"), OnSave, save)
            .EndChildElements()
            .Compose();

        SingleComposer.GetSwitch("spin").On = true;
        UpdateViewButtons();
        ShowFacing();
        SingleComposer.GetNumberInput("frame").SetValue(frame);
        SingleComposer.GetNumberInput("elkFrame")?.SetValue(elkFrame);
        SingleComposer.GetSwitch("look").On = appearance.ShowLook;
        SingleComposer.GetSwitch("clothing").On = appearance.ShowClothing;
        SingleComposer.GetSwitch("armor").On = appearance.ShowArmor;
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

    private static StatueAppearance.HandMode ParseHand(string code) => code switch
    {
        "copy" => StatueAppearance.HandMode.CopyHeld,
        "empty" => StatueAppearance.HandMode.Empty,
        _ => StatueAppearance.HandMode.Keep,
    };

    /// <summary>The choices for a hand: leave it, copy what the player holds now, or empty it.</summary>
    private static string[] HandNames(ItemStack? current, ItemStack? held) =>
    [
        Lang.Get("nf3tstatue:hand-keep", current?.GetName() ?? Lang.Get("nf3tstatue:hand-nothing")),
        Lang.Get("nf3tstatue:hand-copy", held?.GetName() ?? Lang.Get("nf3tstatue:hand-nothing")),
        Lang.Get("nf3tstatue:hand-empty"),
    ];

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

        var key = (emote, frame, name, look, clothing, armor, rightHandMode, leftHandMode, ShownAngle, mount, elkPose, elkFrame);
        if (key == slotKey) return previewSlot;
        slotKey = key;

        var appearanceChanged = (name, look, clothing, armor, rightHandMode, leftHandMode, mount, elkPose) != appearanceKey;
        appearanceKey = (name, look, clothing, armor, rightHandMode, leftHandMode, mount, elkPose);
        if (appearanceChanged) previewAppearance = PreviewAppearance(name, look, clothing, armor);

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
        var appearance = string.Equals(self.PlayerName, name, StringComparison.OrdinalIgnoreCase)
            ? StatueAppearance.Capture(self, look, clothing, armor)
            : new StatueAppearance
            {
                ShowLook = look,
                ShowClothing = clothing,
                ShowArmor = armor,
                SkinShapes = look ? savedAppearance.SkinShapes : [],
                Clothing = clothing ? savedAppearance.Clothing : [],
                Armor = armor ? savedAppearance.Armor : [],
            };

        appearance.Mount = mount;
        appearance.ElkPose = elkPose;

        // The same choice the server will make when saving.
        appearance.RightHand = StatueAppearance.PickHand(rightHandMode, savedAppearance.RightHand,
            self.InventoryManager.ActiveHotbarSlot);
        appearance.LeftHand = StatueAppearance.PickHand(leftHandMode, savedAppearance.LeftHand,
            self.InventoryManager.OffhandHotbarSlot);
        return appearance;
    }

    private bool OnCancel()
    {
        TryClose();
        return true;
    }

    private bool OnSave()
    {
        capi.Network.SendBlockEntityPacket(pos, BlockEntityStatue.EditPacketId,
            BlockEntityStatue.EncodeEdit(playerName, emote, InputFrame, SingleComposer.GetSwitch("look").On,
                SingleComposer.GetSwitch("clothing").On, SingleComposer.GetSwitch("armor").On, rightHandMode, leftHandMode, angleY, mount, elkPose, InputElkFrame));
        TryClose();
        return true;
    }
}
