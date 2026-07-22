using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;

namespace Nf3t.VintageStory.SchematicLocator;

public class StringTableDialog(ICoreClientAPI capi, IClientNetworkChannel requestChannel) : GuiDialog(capi)
{
    public override string ToggleKeyCombinationCode => null;

    // State to hold user input 
    private readonly SchematicSearchRequest _searchRequest = new();

    // Table Data & Sorting
    private readonly SchematicSearchResults _searchResults = new();
    private string _currentSort = "AssetLocation";
    private bool _ascendingSort = true;

    // Pagination State
    private int _currentPage = 0;
    private List<string> _autocompleteMatches = [];
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
        SingleComposer.AddSmallButton("Select", OnSelectAutocompleteClicked, ElementBounds.Fixed(515, 2, 75, 25));
        
        // --- ROW 2: Tree Key ---
        SingleComposer.AddStaticText("Tree Key:", labelFont, ElementBounds.Fixed(0, 40, 120, 20));
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 35, 200, 30), (text) => _searchRequest.TreeKey = text, inputFont, "treeKey");

        // --- ROW 3: Tree Value ---
        SingleComposer.AddStaticText("Tree Value:", labelFont, ElementBounds.Fixed(0, 75, 120, 20));
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 70, 200, 30), (text) => _searchRequest.TreeValue = text, inputFont, "treeValue");

        // --- ROW 4: Domain & Search Button ---
        SingleComposer.AddStaticText("Schematic Domain:", labelFont, ElementBounds.Fixed(0, 110, 120, 20));
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 105, 200, 30), (text) => _searchRequest.Domain = text, inputFont, "domain");
        SingleComposer.AddSmallButton("Search", OnSearchClicked, ElementBounds.Fixed(350, 105, 120, 30));

        // --- TABLE HEADERS ---
        SingleComposer.AddSmallButton("Asset Location", () => SortTable("AssetLocation"), ElementBounds.Fixed(0, 160, 250, 25));
        SingleComposer.AddSmallButton("Matched Block", () => SortTable("MatchedBlock"), ElementBounds.Fixed(260, 160, 220, 25));
        SingleComposer.AddSmallButton("Count", () => SortTable("Count"), ElementBounds.Fixed(490, 160, 70, 25));

        // --- PAGINATED DATA TABLE ---
        int yOffset = 190;
        
        // Grab only the items for the current page
        var pagedResults = _searchResults.Results.Skip(_currentPage * ItemsPerPage).Take(ItemsPerPage).ToList();

        foreach (var row in pagedResults)
        {
            SingleComposer.AddButton(row.AssetLocation[Math.Max(0, row.AssetLocation.Length - 25)..], () => OnAssetLocationClicked(row.AssetLocation), ElementBounds.Fixed(0, yOffset, 260, 22), labelFont, EnumButtonStyle.None);            
            SingleComposer.AddButton(row.MatchedBlock[Math.Max(0, row.MatchedBlock.Length - 20)..], () => OnBlockLocationClicked(row.MatchedBlock), ElementBounds.Fixed(275, yOffset + 2, 225, 20), labelFont, EnumButtonStyle.None);
            SingleComposer.AddStaticText(row.Count.ToString()[Math.Max(0, row.Count.ToString().Length - 12)..], labelFont, ElementBounds.Fixed(515, yOffset + 2, 75, 20));
            yOffset += 26;
        }

        // --- PAGINATION CONTROLS ---
        var totalPages = (int)Math.Ceiling(_searchResults.Results.Count / (double)ItemsPerPage);
        if (totalPages == 0) totalPages = 1; // Prevent "Page 1 of 0" if empty

        const int paginationY = 425;

        // Previous Button
        if (_currentPage > 0)
        {
            SingleComposer.AddSmallButton("<", OnPrevPage, ElementBounds.Fixed(180, paginationY, 30, 25));
        }

        // Page Tracker Text (shifted right to give breathing room)
        string pageText = $"Page {_currentPage + 1} of {totalPages} ({_searchResults.Results.Count} total)";
        SingleComposer.AddStaticText(pageText, labelFont, ElementBounds.Fixed(195, paginationY + 3, 200, 20));

        // Next Button (shifted right past the tracker text)
        if (_currentPage < totalPages - 1)
        {
            SingleComposer.AddSmallButton(">", OnNextPage, ElementBounds.Fixed(410, paginationY, 30, 25));
        }

        // Finish composing the entire window
        SingleComposer.EndChildElements().EndChildElements().Compose();

        // Restore input states
        SingleComposer.GetTextInput("searchBlockPrefix").SetValue(_searchRequest.SearchBlockPrefix);
        SingleComposer.GetTextInput("treeKey").SetValue(_searchRequest.TreeKey);
        SingleComposer.GetTextInput("treeValue").SetValue(_searchRequest.TreeValue);
        SingleComposer.GetTextInput("domain").SetValue(_searchRequest.Domain);
    }
    
    private bool OnBlockLocationClicked(string rowMatchedBlock)
    {
        capi.Forms.SetClipboardText(rowMatchedBlock);
        capi.TriggerChatMessage($"[Schematic Locator] Copied '{rowMatchedBlock}' to clipboard!");
        return true;
    }
    
    private bool OnAssetLocationClicked(string assetLocation)
    {
        capi.Forms.SetClipboardText(assetLocation);
        capi.TriggerChatMessage($"[Schematic Locator] Copied '{assetLocation}' to clipboard!");
        return true;
    }

    private bool OnSelectAutocompleteClicked()
    {
        // Safely check for null and empty
        if (_autocompleteMatches == null || _autocompleteMatches.Count == 0)
            return true;

        // Update the state
        _searchRequest.SearchBlockPrefix = _autocompleteMatches[0];

        // Recompose the UI to force the text input to redraw without keyboard focus
        RecomposeTable();

        // RecomposeTable triggers OnSearchBlockTextChanged which sets the hint to "Matches: ...", 
        // so we overwrite it back to "Selected" immediately after recomposing.
        SingleComposer.GetDynamicText("autocompleteHint")?.SetNewText("Selected: " + _searchRequest.SearchBlockPrefix);

        return true;
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
        int totalPages = (int)Math.Ceiling(_searchResults.Results.Count / (double)ItemsPerPage);
        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            RecomposeTable();
        }
        return true;
    }

    private void OnSearchBlockTextChanged(string text)
    {
        _searchRequest.SearchBlockPrefix = text;

        var hintElement = SingleComposer.GetDynamicText("autocompleteHint");
        if (hintElement == null) return;

        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
        {
            hintElement.SetNewText("");
            return;
        }

        _autocompleteMatches = capi.World.Blocks
            .Where(b => b.Code != null && b.Code.Path.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
            .Select(b => b.Code.Path)
            .Distinct()
            .Take(3)
            .ToList();

        if (_autocompleteMatches.Count > 0)
        {
            hintElement.SetNewText($"Matches: {_autocompleteMatches[0]}");
        }
        else
        {
            hintElement.SetNewText("No matches found.");
        }
    }

    private bool OnSearchClicked()
    {
        capi.Logger.Debug($"Searching for schematic: {_searchRequest}");
        requestChannel.SendPacket(_searchRequest);
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
            _searchResults.Results = _ascendingSort 
                ? _searchResults.Results.OrderBy(x => x.AssetLocation).ToList() 
                : _searchResults.Results.OrderByDescending(x => x.AssetLocation).ToList();
        }
        else if (_currentSort == "MatchedBlock")
        {
            _searchResults.Results = _ascendingSort 
                ? _searchResults.Results.OrderBy(x => x.MatchedBlock).ToList() 
                : _searchResults.Results.OrderByDescending(x => x.MatchedBlock).ToList();
        }
        else if (_currentSort == "Count")
        {
            _searchResults.Results = _ascendingSort 
                ? _searchResults.Results.OrderBy(x => x.Count).ToList() 
                : _searchResults.Results.OrderByDescending(x => x.Count).ToList();
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

    public void Update(SchematicSearchResults searchResults)
    {
        _searchResults.Results.Clear();
        _searchResults.Results.AddRange(searchResults.Results);
        
        // Reset to page 1 whenever a new search is performed
        _currentPage = 0; 
        SortTable(_currentSort, forceRetainDirection: true);
    }
}