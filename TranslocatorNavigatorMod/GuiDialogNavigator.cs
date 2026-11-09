#nullable enable

using System;
using System.Linq;
using Nf3t.VintageStory.Common;
using Vintagestory.API.Client;

namespace Nf3t.VintageStory.TranslocatorShortestPath;

/// <summary>Pick a start and a destination, see the route through known translocators, and start HUD guidance.</summary>
public class GuiDialogNavigator(ICoreClientAPI capi, Navigator navigator) : GuiDialog(capi)
{
    private const int StepsPerPage = 8;
    private const double Width = 520;

    // Spawn-relative coordinates as typed, kept between recompositions: start x, y, z then destination x, y, z.
    private readonly string[] coords = new string[6];
    private bool coordsFilled;
    private string? error;
    private int page;

    public override string? ToggleKeyCombinationCode => null;

    public override void OnGuiOpened()
    {
        if (!coordsFilled)
        {
            coordsFilled = true;
            SetCoords(0, navigator.Context.GetPlayerPos());
            if (navigator.Context.SaveData.LastTranslocatorPathPerSavegame.TryGetValue(
                    capi.World.SavegameIdentifier, out var last))
                SetCoords(3, last.GoalPos);
        }

        Compose();
    }

    /// <summary>Rebuilds the dialog if it is open, e.g. after the guided step changed.</summary>
    public void Refresh()
    {
        if (IsOpened()) Compose();
    }

    private void SetCoords(int offset, SimplePos pos)
    {
        var spawn = navigator.Context.DefaultSpawnPosition;
        coords[offset] = (pos.X - spawn.X).ToString();
        coords[offset + 1] = pos.Y.ToString();
        coords[offset + 2] = (pos.Z - spawn.Z).ToString();
    }

    private bool TryGetCoords(int offset, out SimplePos pos)
    {
        pos = default;
        if (!int.TryParse(coords[offset]?.Trim(), out var x) ||
            !int.TryParse(coords[offset + 1]?.Trim(), out var y) ||
            !int.TryParse(coords[offset + 2]?.Trim(), out var z))
            return false;
        pos = RouteSteps.FromRelative(x, y, z, navigator.Context.DefaultSpawnPosition);
        return true;
    }

    private void Compose()
    {
        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        var contentBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, Width, 450);

        var label = CairoFont.WhiteSmallText();
        var input = CairoFont.TextInput();

        var composer = capi.Gui.CreateCompo("nf3ttlnav", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar("Translocator Navigator", () => TryClose())
            .BeginChildElements(bgBounds)
            .BeginChildElements(contentBounds);

        AddCoordsRow(composer, "Start (X, Y, Z):", 0, 0, label, input);
        AddCoordsRow(composer, "Destination:", 40, 3, label, input);

        composer.AddSmallButton("Find route", OnFindRoute, ElementBounds.Fixed(0, 80, 120, 30));
        composer.AddSmallButton("Swap", OnSwap, ElementBounds.Fixed(130, 80, 80, 30));
        var guiding = navigator.Guidance != null;
        if (guiding)
            composer.AddSmallButton("Stop guidance", () =>
            {
                navigator.StopGuidance();
                return true;
            }, ElementBounds.Fixed(220, 80, 140, 30));
        else if (navigator.Steps.Count > 0)
            composer.AddSmallButton("Start guidance", () =>
            {
                navigator.StartGuidance();
                return true;
            }, ElementBounds.Fixed(220, 80, 140, 30));

        composer.AddStaticText(Summary(), label, ElementBounds.Fixed(0, 122, Width, 44));

        var steps = navigator.Steps;
        var pages = Math.Max(1, (int)Math.Ceiling(steps.Count / (double)StepsPerPage));
        page = Math.Clamp(page, 0, pages - 1);
        var y = 172.0;
        for (var i = page * StepsPerPage; i < Math.Min(steps.Count, (page + 1) * StepsPerPage); i++)
        {
            var step = steps[i];
            var marker = navigator.Guidance?.Index == i ? "> " : "";
            var text = $"{marker}{i + 1}. {navigator.Describe(step)}";
            var textBounds = ElementBounds.Fixed(0, y + 4, Width - 70, 22);
            composer.AddStaticText(text, label, textBounds);
            composer.AddHoverText(text, label, 400, textBounds);
            var target = navigator.Coords(step.To);
            composer.AddSmallButton("Copy", () => OnCopy(target), ElementBounds.Fixed(Width - 60, y, 60, 26));
            y += 30;
        }

        if (pages > 1)
        {
            const double pagingY = 418;
            if (page > 0)
                composer.AddSmallButton("<", () => ChangePage(-1), ElementBounds.Fixed(170, pagingY, 30, 25));
            composer.AddStaticText($"Page {page + 1} of {pages}", label,
                ElementBounds.Fixed(210, pagingY + 3, 110, 20));
            if (page < pages - 1)
                composer.AddSmallButton(">", () => ChangePage(1), ElementBounds.Fixed(320, pagingY, 30, 25));
        }

        SingleComposer = composer.EndChildElements().EndChildElements().Compose();

        for (var i = 0; i < coords.Length; i++)
            SingleComposer.GetTextInput(InputKey(i)).SetValue(coords[i] ?? "");
    }

    private void AddCoordsRow(GuiComposer composer, string title, double y, int offset, CairoFont label,
        CairoFont input)
    {
        composer.AddStaticText(title, label, ElementBounds.Fixed(0, y + 5, 130, 20));
        for (var i = 0; i < 3; i++)
        {
            var index = offset + i;
            composer.AddTextInput(ElementBounds.Fixed(135 + i * 80, y, 72, 30), text => coords[index] = text, input,
                InputKey(index));
        }

        composer.AddSmallButton("Here", () =>
        {
            SetCoords(offset, navigator.Context.GetPlayerPos());
            Compose();
            return true;
        }, ElementBounds.Fixed(380, y, 70, 30));
    }

    private static string InputKey(int index)
    {
        return "coord" + index;
    }

    private string Summary()
    {
        if (error != null) return error;
        var result = navigator.Result;
        if (result == null) return "Enter a start and a destination, then press Find route.";

        var straight = result.GetBirdsEyeDistance();
        if (!navigator.Steps.Any(s => s.Kind == StepKind.Teleport))
        {
            return $"No translocator shortcut found: walk {straight} m " +
                   $"{navigator.Goal.GetCompassDirection(navigator.Start)}.";
        }

        var total = result.GetTotalDistance();
        return $"Route: {total} m walking instead of {straight} m (saves {Math.Max(0, straight - total)} m).\n" +
               $"{navigator.Steps.Count(s => s.Kind == StepKind.Teleport)} translocator jump(s).";
    }

    private bool OnFindRoute()
    {
        if (!TryGetCoords(0, out var start) || !TryGetCoords(3, out var goal))
        {
            error = "Start and destination need whole numbers for X, Y and Z.";
            Compose();
            return true;
        }

        error = null;
        page = 0;
        navigator.FindRoute(start, goal); // Raises Changed, which recomposes the dialog.
        return true;
    }

    private bool OnSwap()
    {
        for (var i = 0; i < 3; i++) (coords[i], coords[i + 3]) = (coords[i + 3], coords[i]);
        Compose();
        return true;
    }

    private bool OnCopy(string text)
    {
        capi.Forms.SetClipboardText(text);
        capi.ShowChatMessage($"[Translocator Navigator] Copied {text} to the clipboard.");
        return true;
    }

    private bool ChangePage(int delta)
    {
        page += delta;
        Compose();
        return true;
    }
}
