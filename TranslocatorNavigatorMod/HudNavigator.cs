#nullable enable

using System;
using Cairo;
using Nf3t.VintageStory.Common;
using Vintagestory.API.Client;

namespace Nf3t.VintageStory.TranslocatorShortestPath;

/// <summary>
/// A bar near the top of the screen with the next step of the route being followed, and an arrow on its right that
/// points to the step's target relative to where the player looks (up is straight ahead).
/// </summary>
public class HudNavigator : HudElement
{
    private const int ArrivedShownMs = 5000;

    /// <summary>Below the game's block info tooltip, which sits at the top of the screen.</summary>
    private const double TopOffset = 120;

    private const double TextWidth = 520;
    private const double ArrowSize = 32;

    /// <summary>Redraw the arrow only when it turns by more than this (radians, about 3 degrees).</summary>
    private const double ArrowRedrawStep = 0.05;

    private readonly Navigator navigator;
    private string text = "";
    private long arrivedCallback;

    /// <summary>Clockwise from straight ahead, in radians; null hides the arrow.</summary>
    private double? arrowAngle;

    public HudNavigator(ICoreClientAPI capi, Navigator navigator) : base(capi)
    {
        this.navigator = navigator;

        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterTop)
            .WithFixedAlignmentOffset(0, TopOffset);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding / 2);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        SingleComposer = capi.Gui.CreateCompo("nf3ttlnavhud", dialogBounds)
            .AddShadedDialogBG(bgBounds, false)
            .BeginChildElements(bgBounds)
            .AddDynamicText("", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 0, TextWidth, 40), "text")
            .AddDynamicCustomDraw(ElementBounds.Fixed(TextWidth + 8, 2, ArrowSize, ArrowSize), DrawArrow, "arrow")
            .EndChildElements()
            .Compose();
    }

    public override string? ToggleKeyCombinationCode => null;
    public override bool Focusable => false;

    /// <summary>Shows, updates or hides the HUD to match the navigator. Called on every tick while guiding.</summary>
    public void Refresh()
    {
        var guidance = navigator.Guidance;
        if (guidance == null)
        {
            CancelArrived();
            if (IsOpened()) TryClose();
            text = "";
            arrowAngle = null;
            return;
        }

        string next;
        double? angle = null;
        var current = guidance.Current;
        if (current == null)
        {
            next = "Translocator Navigator: arrived.";
            if (arrivedCallback == 0)
                arrivedCallback = capi.Event.RegisterCallback(_ =>
                {
                    arrivedCallback = 0;
                    navigator.StopGuidance();
                }, ArrivedShownMs);
        }
        else
        {
            CancelArrived();
            var player = navigator.Context.GetPlayerPos();
            next = $"Step {guidance.Index + 1}/{guidance.Steps.Count}: " + navigator.DescribeLive(current.Value, player);
            angle = ArrowAngle(current.Value.To, player);
        }

        if (next != text)
        {
            text = next;
            SingleComposer.GetDynamicText("text").SetNewText(text);
        }

        UpdateArrow(angle);

        if (!IsOpened()) TryOpen();
    }

    /// <summary>The target's direction relative to the camera, or null when there is none (looking straight down, or there).</summary>
    private double? ArrowAngle(SimplePos target, SimplePos player)
    {
        var entity = capi.World.Player?.Entity;
        if (entity == null) return null;
        var view = entity.Pos.GetViewVector();
        return Bearing.Relative(view.X, view.Z, target.X - player.X, target.Z - player.Z);
    }

    private void UpdateArrow(double? angle)
    {
        // Looking straight down has no direction: keep the last arrow rather than flicker.
        if (angle == null && navigator.Guidance?.Current != null) return;
        if (angle == null && arrowAngle == null) return;
        if (angle != null && arrowAngle != null && Math.Abs(Wrap(angle.Value - arrowAngle.Value)) < ArrowRedrawStep) return;
        arrowAngle = angle;
        SingleComposer.GetCustomDraw("arrow").Redraw();
    }

    private static double Wrap(double a) => Math.IEEERemainder(a, 2 * Math.PI);

    private void DrawArrow(Cairo.Context ctx, ImageSurface surface, ElementBounds bounds)
    {
        if (arrowAngle is not { } angle) return;

        var r = bounds.InnerWidth / 2 * 0.9;
        // A dynamic custom draw paints into a surface of its own, the size of the element, so its center is the
        // surface's center; drawX/drawY (the element's place in the whole dialog) would land outside it. Fall back to
        // them only if the game ever hands over the dialog's shared surface instead.
        var own = surface.Width <= Math.Ceiling(bounds.InnerWidth) + 1;
        var cx = (own ? 0 : bounds.drawX) + bounds.InnerWidth / 2;
        var cy = (own ? 0 : bounds.drawY) + bounds.InnerHeight / 2;
        ctx.Save();
        ctx.Translate(cx, cy);
        ctx.Rotate(angle); // Cairo's y points down, so a positive angle turns clockwise, like the bearing.
        ctx.MoveTo(0, -r);
        ctx.LineTo(r * 0.7, r * 0.6);
        ctx.LineTo(0, r * 0.25);
        ctx.LineTo(-r * 0.7, r * 0.6);
        ctx.ClosePath();
        ctx.SetSourceRGBA(1, 1, 1, 0.95);
        ctx.FillPreserve();
        ctx.SetSourceRGBA(0, 0, 0, 0.6);
        ctx.LineWidth = 1.5;
        ctx.Stroke();
        ctx.Restore();
    }

    private void CancelArrived()
    {
        if (arrivedCallback == 0) return;
        capi.Event.UnregisterCallback(arrivedCallback);
        arrivedCallback = 0;
    }
}
