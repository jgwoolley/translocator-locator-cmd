using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// Draws a statue item inside a dialog, turning slowly unless spinning is switched off. The figure comes from the item's settings, see
/// <see cref="BlockStatue.OnBeforeRender"/>.
/// </summary>
public class GuiElementStatuePreview(ICoreClientAPI capi, ElementBounds bounds, Func<ItemSlot?> getSlot, Func<bool> isSpinning)
    : GuiElement(capi, bounds)
{
    public override void RenderInteractiveElements(float deltaTime)
    {
        var slot = getSlot();
        if (slot == null) return;

        api.Render.RenderItemstackToGui(slot, Bounds.renderX + Bounds.OuterWidth / 2,
            Bounds.renderY + Bounds.OuterHeight / 2, 450, (float)(Bounds.OuterHeight * 0.6), ColorUtil.WhiteArgb,
            deltaTime, true, isSpinning(), false);
    }
}
