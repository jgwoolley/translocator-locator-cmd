using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;

namespace Nf3t.VintageStory.SchematicLocator;

public class SchematicSearchResult
{
    public string AssetLocation { get; set; }
    public string MatchedBlock { get; set; }
    public int Count { get; set; }
}

public class StringTableDialog(ICoreClientAPI capi) : GuiDialog(capi)
{
    public override string ToggleKeyCombinationCode => null;

    // State to hold user input 
    private string _searchBlockPrefix = "";
    private string _treeKey = "";
    private string _treeValue = "";
    private string _domain = "game";

    // Table Data & Sorting
    private List<SchematicSearchResult> _searchResults = new();
    private string _currentSort = "AssetLocation";
    private bool _ascendingSort = true;

    // Pagination State
    private int _currentPage = 0;
    private const int ItemsPerPage = 9;

    public override void OnGuiOpened()
    {
        BuildAndComposeDialog();
    }

    private void BuildAndComposeDialog()
    {
        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterMiddle);

        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        // Bounds for the whole window (600 width, 460 height)
        var contentBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, 600, 460);

        SingleComposer = capi.Gui.CreateCompo("schematicsearch", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar("Schematic Locator", OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .BeginChildElements(contentBounds);

        var labelFont = CairoFont.WhiteSmallText();
        var inputFont = CairoFont.TextInput();

        // --- ROW 1: Search Block Prefix & Autocomplete Suggestions ---
        SingleComposer.AddStaticText("Search Block:", labelFont, ElementBounds.Fixed(0, 5, 120, 20));
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 0, 200, 30), OnSearchBlockTextChanged, inputFont, "searchBlockPrefix");
        SingleComposer.AddDynamicText("", labelFont, ElementBounds.Fixed(340, 5, 250, 20), "autocompleteHint");

        // --- ROW 2: Tree Key ---
        SingleComposer.AddStaticText("Tree Key:", labelFont, ElementBounds.Fixed(0, 40, 120, 20));
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 35, 200, 30), (text) => _treeKey = text, inputFont, "treeKey");

        // --- ROW 3: Tree Value ---
        SingleComposer.AddStaticText("Tree Value:", labelFont, ElementBounds.Fixed(0, 75, 120, 20));
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 70, 200, 30), (text) => _treeValue = text, inputFont, "treeValue");

        // --- ROW 4: Domain & Search Button ---
        SingleComposer.AddStaticText("Domain:", labelFont, ElementBounds.Fixed(0, 110, 120, 20));
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 105, 200, 30), (text) => _domain = text, inputFont, "domain");
        SingleComposer.AddSmallButton("Search", OnSearchClicked, ElementBounds.Fixed(350, 105, 120, 30));

        // --- TABLE HEADERS ---
        SingleComposer.AddSmallButton("Asset Location", () => SortTable("AssetLocation"), ElementBounds.Fixed(0, 160, 250, 25));
        SingleComposer.AddSmallButton("Matched Block", () => SortTable("MatchedBlock"), ElementBounds.Fixed(260, 160, 220, 25));
        SingleComposer.AddSmallButton("Count", () => SortTable("Count"), ElementBounds.Fixed(490, 160, 70, 25));

        // --- PAGINATED DATA TABLE ---
        int yOffset = 190;
        
        // Grab only the items for the current page
        var pagedResults = _searchResults.Skip(_currentPage * ItemsPerPage).Take(ItemsPerPage).ToList();

        foreach (var row in pagedResults)
        {
            SingleComposer.AddStaticText(row.AssetLocation, labelFont, ElementBounds.Fixed(5, yOffset, 245, 20));
            SingleComposer.AddStaticText(row.MatchedBlock, labelFont, ElementBounds.Fixed(265, yOffset, 215, 20));
            SingleComposer.AddStaticText(row.Count.ToString(), labelFont, ElementBounds.Fixed(495, yOffset, 65, 20));
            yOffset += 25;
        }

        // --- PAGINATION CONTROLS ---
        int totalPages = (int)Math.Ceiling(_searchResults.Count / (double)ItemsPerPage);
        if (totalPages == 0) totalPages = 1; // Prevent "Page 1 of 0" if empty

        int paginationY = 425;

        // Previous Button
        if (_currentPage > 0)
        {
            SingleComposer.AddSmallButton("<", OnPrevPage, ElementBounds.Fixed(180, paginationY, 30, 25));
        }

        // Page Tracker
        string pageText = $"Page {_currentPage + 1} of {totalPages} ({_searchResults.Count} total)";
        SingleComposer.AddStaticText(pageText, labelFont, ElementBounds.Fixed(230, paginationY + 3, 200, 20));

        // Next Button
        if (_currentPage < totalPages - 1)
        {
            SingleComposer.AddSmallButton(">", OnNextPage, ElementBounds.Fixed(390, paginationY, 30, 25));
        }

        // Finish composing the entire window
        SingleComposer.EndChildElements().EndChildElements().Compose();

        // Restore input states
        SingleComposer.GetTextInput("searchBlockPrefix").SetValue(_searchBlockPrefix);
        SingleComposer.GetTextInput("treeKey").SetValue(_treeKey);
        SingleComposer.GetTextInput("treeValue").SetValue(_treeValue);
        SingleComposer.GetTextInput("domain").SetValue(_domain);
    }

    private bool OnPrevPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RecomposeTable();
        }
        return true;
    }

    private bool OnNextPage()
    {
        int totalPages = (int)Math.Ceiling(_searchResults.Count / (double)ItemsPerPage);
        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            RecomposeTable();
        }
        return true;
    }

    private void OnSearchBlockTextChanged(string text)
    {
        _searchBlockPrefix = text;

        var hintElement = SingleComposer.GetDynamicText("autocompleteHint");
        if (hintElement == null) return;

        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
        {
            hintElement.SetNewText("");
            return;
        }

        var matches = capi.World.Blocks
            .Where(b => b.Code != null && b.Code.Path.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
            .Select(b => b.Code.Path)
            .Distinct()
            .Take(3)
            .ToList();

        if (matches.Count > 0)
        {
            hintElement.SetNewText("Matches: " + string.Join(", ", matches));
        }
        else
        {
            hintElement.SetNewText("No matches found.");
        }
    }

    private bool OnSearchClicked()
    {
        // ... Send network packet to Server here ...
        
        // Placeholder simulation containing enough data to trigger multiple pages:
        _searchResults.Clear();
        var block = string.IsNullOrEmpty(_searchBlockPrefix) ? "any" : _searchBlockPrefix;
        
        for (int i = 0; i < 45; i++)
        {
            _searchResults.Add(new SchematicSearchResult 
            { 
                AssetLocation = $"ruins/house_{i}", 
                MatchedBlock = $"{block}-variant-{i}", 
                Count = (i * 14) + 1 
            });
        }

        // Reset to page 1 whenever a new search is performed
        _currentPage = 0; 
        SortTable(_currentSort, forceRetainDirection: true);
        return true;
    }

    private bool SortTable(string sortType, bool forceRetainDirection = false)
    {
        if (!forceRetainDirection)
        {
            if (_currentSort == sortType)
            {
                _ascendingSort = !_ascendingSort;
            }
            else
            {
                _currentSort = sortType;
                _ascendingSort = true;
            }
        }

        if (_currentSort == "AssetLocation")
        {
            _searchResults = _ascendingSort 
                ? _searchResults.OrderBy(x => x.AssetLocation).ToList() 
                : _searchResults.OrderByDescending(x => x.AssetLocation).ToList();
        }
        else if (_currentSort == "MatchedBlock")
        {
            _searchResults = _ascendingSort 
                ? _searchResults.OrderBy(x => x.MatchedBlock).ToList() 
                : _searchResults.OrderByDescending(x => x.MatchedBlock).ToList();
        }
        else if (_currentSort == "Count")
        {
            _searchResults = _ascendingSort 
                ? _searchResults.OrderBy(x => x.Count).ToList() 
                : _searchResults.OrderByDescending(x => x.Count).ToList();
        }

        // Jump back to page 1 when the user changes the sort order
        if (!forceRetainDirection) 
        {
            _currentPage = 0;
        }

        RecomposeTable();
        return true;
    }

    private void RecomposeTable()
    {
        SingleComposer?.Dispose();
        BuildAndComposeDialog();
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }
}