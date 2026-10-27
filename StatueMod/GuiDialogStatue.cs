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
    private string playerName;
    private float angleY;

    private StatueAppearance.HandMode rightHandMode;
    private StatueAppearance.HandMode leftHandMode;

    private (string Name, bool Look, bool Clothing, bool Armor, StatueAppearance.HandMode Right, StatueAppearance.HandMode Left)? appearanceKey;
    private StatueAppearance previewAppearance;
    private (string Emote, int Frame, string Name, bool Look, bool Clothing, bool Armor, StatueAppearance.HandMode Right, StatueAppearance.HandMode Left, float AngleY)? slotKey;
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
        this.angleY = angleY;
        if (playerName == "") playerName = capi.World.Player.PlayerName;
        this.playerName = playerName;
        var players = PlayerChoices(playerName);
        if (frame < 0) frame = StatueMesh.PeakFrame(capi, this.emote, bust);

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
        var preview = ElementBounds.Fixed(previewX, 30, 220, 200);
        var turnLeft = ElementBounds.Fixed(previewX, 238, 40, 28);
        var turnRight = ElementBounds.Fixed(previewX + 50, 238, 40, 28);
        var spinSwitch = ElementBounds.Fixed(previewX + 110, 240, 25, 25);
        var playSwitch = ElementBounds.Fixed(previewX, 278, 25, 25);
        var rightHandLabel = ElementBounds.Fixed(previewX, 314, 220, 20);
        var rightHandInput = ElementBounds.Fixed(previewX, 336, 220, 30);
        var leftHandLabel = ElementBounds.Fixed(previewX, 376, 220, 20);
        var leftHandInput = ElementBounds.Fixed(previewX, 398, 220, 30);
        var cancel = ElementBounds.Fixed(0, 440, 0, 0).WithFixedPadding(10, 2);
        var save = ElementBounds.Fixed(0, 440, 0, 0).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10, 2);

        var background = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        background.BothSizing = ElementSizing.FitToChildren;
        var dialog = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

        var names = emotes.Select(e => Lang.Get("nf3tstatue:emote-" + e)).ToArray();
        SingleComposer = capi.Gui.CreateCompo("nf3tstatue", dialog)
            .AddShadedDialogBG(background)
            .AddDialogTitleBar(Lang.Get("nf3tstatue:dialog-title"), () => TryClose())
            .BeginChildElements(background)
            .AddStaticText(Lang.Get("nf3tstatue:dialog-player"), label, playerLabel)
            .AddDropDown(players, players, Math.Max(0, Array.IndexOf(players, playerName)),
                (code, _) => this.playerName = code, playerInput, "player")
            .AddStaticText(Lang.Get("nf3tstatue:dialog-emote"), label, emoteLabel)
            .AddDropDown(emotes, names, Math.Max(0, Array.IndexOf(emotes, this.emote)), OnEmoteChanged, emoteInput,
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
            .AddStaticText(Lang.Get("nf3tstatue:dialog-layer-armor"), label, armorSwitch.RightCopy(10, 3).WithFixedWidth(width - 40))
            .AddInset(preview, 4)
            .AddInteractiveElement(new GuiElementStatuePreview(capi, preview, PreviewSlot,
                () => SingleComposer.GetSwitch("spin").On), "preview")
            .AddSmallButton("<", () => Turn(-1), turnLeft)
            .AddSmallButton(">", () => Turn(1), turnRight)
            .AddSwitch(null, spinSwitch, "spin", 25, 3)
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
        SingleComposer.GetNumberInput("frame").SetValue(frame);
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

    private bool Turn(int direction)
    {
        angleY = (float)((angleY + direction * GameMath.PIHALF / 4) % GameMath.TWOPI);
        return true;
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

    private int FrameCount => StatueMesh.FrameCount(capi, emote);

    private int InputFrame => Math.Clamp((int)SingleComposer.GetNumberInput("frame").GetValue(), 0, FrameCount - 1);

    private void OnEmoteChanged(string code, bool selected)
    {
        emote = code;
        OnBestFrame();
    }

    private bool OnBestFrame()
    {
        SingleComposer.GetNumberInput("frame").SetValue(StatueMesh.PeakFrame(capi, emote, bust));
        return true;
    }

    /// <summary>
    /// The statue item the preview draws. It is only rebuilt when a setting changes, and while playing the frame
    /// follows the clock.
    /// </summary>
    private ItemSlot? PreviewSlot()
    {
        var playing = SingleComposer.GetSwitch("play").On;
        var frame = playing
            ? (int)(capi.ElapsedMilliseconds / 1000.0 * PlaybackFramesPerSecond) % FrameCount
            : InputFrame;
        var name = playerName;
        var look = SingleComposer.GetSwitch("look").On;
        var clothing = SingleComposer.GetSwitch("clothing").On;
        var armor = SingleComposer.GetSwitch("armor").On;

        var key = (emote, frame, name, look, clothing, armor, rightHandMode, leftHandMode, angleY);
        if (key == slotKey) return previewSlot;
        slotKey = key;

        var appearanceChanged = (name, look, clothing, armor, rightHandMode, leftHandMode) != appearanceKey;
        appearanceKey = (name, look, clothing, armor, rightHandMode, leftHandMode);
        if (appearanceChanged) previewAppearance = PreviewAppearance(name, look, clothing, armor);

        var stack = new ItemStack(block);
        var tree = new TreeAttribute();
        BlockEntityStatue.WriteSettings(tree, name, emote, frame, previewAppearance);
        tree.SetFloat("previewAngle", angleY);
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
                SingleComposer.GetSwitch("clothing").On, SingleComposer.GetSwitch("armor").On, rightHandMode, leftHandMode, angleY));
        TryClose();
        return true;
    }
}
