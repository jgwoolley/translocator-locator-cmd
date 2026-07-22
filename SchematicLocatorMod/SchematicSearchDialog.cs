using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.SchematicLocator;

/// <summary>
/// 
/// </summary>
/// <param name="capi"></param>
public class SchematicSearchDialog(ICoreClientAPI capi) : GuiDialog(capi)
{
    public override string ToggleKeyCombinationCode => null;

    // State to hold user input 
    private readonly SchematicSearchRequest _searchRequest = new();

    // Table Data & Sorting
    private List<SchematicSearchResult> _searchResults = new();
    private string _currentSort = "Count";
    private bool _ascendingSort = false;

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
            .AddDialogTitleBar(SchematicLocatorModSystem.Title, OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .BeginChildElements(contentBounds);

        var labelFont = CairoFont.WhiteSmallText();
        var inputFont = CairoFont.TextInput();

        // --- ROW 1: Search Block Prefix & Autocomplete Suggestions ---
        var searchLblBounds = ElementBounds.Fixed(0, 5, 120, 20);
        SingleComposer.AddStaticText("Search Block:", labelFont, searchLblBounds);
        SingleComposer.AddHoverText("The block code prefix to search for (e.g. 'loosegears-1')", labelFont, 250, searchLblBounds);
        
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 0, 200, 30), OnSearchBlockTextChanged, inputFont, "searchBlockPrefix");
        SingleComposer.AddDynamicText("", labelFont, ElementBounds.Fixed(340, 5, 250, 20), "autocompleteHint");
        
        // --- ROW 2: Tree Key ---
        var treeKeyLblBounds = ElementBounds.Fixed(0, 40, 120, 20);
        SingleComposer.AddStaticText("Tree Key:", labelFont, treeKeyLblBounds);
        SingleComposer.AddHoverText("The block entity attribute tree key to filter by", labelFont, 250, treeKeyLblBounds);
        
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 35, 200, 30), (text) => _searchRequest.TreeKey = text, inputFont, "treeKey");
        SingleComposer.AddSmallButton("Select", OnSelectAutocompleteClicked, ElementBounds.Fixed(515, 35, 75, 25)); // Moved to Tree Key Row

        // --- ROW 3: Tree Value ---
        var treeValLblBounds = ElementBounds.Fixed(0, 75, 120, 20);
        SingleComposer.AddStaticText("Tree Value:", labelFont, treeValLblBounds);
        SingleComposer.AddHoverText("The specific value expected for the given Tree Key", labelFont, 250, treeValLblBounds);
        
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 70, 200, 30), (text) => _searchRequest.TreeValue = text, inputFont, "treeValue");

        // --- ROW 4: Domain & Search Button ---
        var domainLblBounds = ElementBounds.Fixed(0, 110, 120, 20);
        SingleComposer.AddStaticText("Schematic Domain:", labelFont, domainLblBounds);
        SingleComposer.AddHoverText("The domain of the schematic (e.g., 'game' or 'yourmod')", labelFont, 250, domainLblBounds);
        
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 105, 200, 30), (text) => _searchRequest.Domain = text, inputFont, "domain");
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
            SingleComposer.AddButton(row.AssetLocation[Math.Max(0, row.AssetLocation.Length - 25)..], () => OnAssetLocationClicked(row.AssetLocation), ElementBounds.Fixed(0, yOffset, 250, 22), labelFont, EnumButtonStyle.None);            
            
            // Try resolving block or item for the icon
            var assetLoc = new AssetLocation(row.MatchedBlock);
            var block = capi.World.GetBlock(assetLoc);
            ItemStack stack = null;

            if (block != null)
            {
                var dropStack = block.Drops?.FirstOrDefault(d => d.ResolvedItemstack != null)?.ResolvedItemstack;
                
                if (dropStack != null)
                {
                    stack = dropStack.Clone();
                    stack.StackSize = 1;
                }
                else
                {
                    // Special explicit fix for paintings and flat wall decor components
                    if (assetLoc.Path.StartsWith("painting"))
                    {
                        // Find any generic painting item registered in the game database
                        var paintingItem = capi.World.Items.FirstOrDefault(i => i.Code != null && i.Code.Path.StartsWith("painting"));
                        if (paintingItem != null)
                        {
                            stack = new ItemStack(paintingItem);
                        }
                    }

                    stack ??= new ItemStack(block);
                }
            }
            else
            {
                var item = capi.World.GetItem(assetLoc);
                if (item != null)
                {
                    stack = new ItemStack(item);
                }
            }

            if (stack != null)
            {
                // Render pure item icon without standard slot background
                SingleComposer.AddInteractiveElement(new ItemIconElement(capi, ElementBounds.Fixed(252, yOffset - 1, 24, 24), stack));
            }

            // Pushed start X to 285 and reduced width to 205 to clear the icon safely
            SingleComposer.AddButton(row.MatchedBlock[Math.Max(0, row.MatchedBlock.Length - 20)..], () => OnBlockLocationClicked(row.MatchedBlock), ElementBounds.Fixed(285, yOffset + 2, 205, 20), labelFont, EnumButtonStyle.None);
            SingleComposer.AddStaticText(row.Count.ToString()[Math.Max(0, row.Count.ToString().Length - 12)..], labelFont, ElementBounds.Fixed(515, yOffset + 2, 75, 20));
            yOffset += 26;
        }

        // --- PAGINATION CONTROLS ---
        var totalPages = (int)Math.Ceiling(_searchResults.Count / (double)ItemsPerPage);
        if (totalPages == 0) totalPages = 1; // Prevent "Page 1 of 0" if empty

        const int paginationY = 425;

        // First and Previous Buttons
        if (_currentPage > 0)
        {
            SingleComposer.AddSmallButton("<<", OnFirstPage, ElementBounds.Fixed(140, paginationY, 30, 25));
            SingleComposer.AddSmallButton("<", OnPrevPage, ElementBounds.Fixed(175, paginationY, 30, 25));
        }

        // Page Tracker Text
        var pageText = $"Page {_currentPage + 1} of {totalPages} ({_searchResults.Count} total)";
        SingleComposer.AddStaticText(pageText, labelFont, ElementBounds.Fixed(210, paginationY + 3, 200, 20));

        // Next and Last Buttons
        if (_currentPage < totalPages - 1)
        {
            SingleComposer.AddSmallButton(">", OnNextPage, ElementBounds.Fixed(415, paginationY, 30, 25));
            SingleComposer.AddSmallButton(">>", OnLastPage, ElementBounds.Fixed(450, paginationY, 30, 25));
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

    private bool OnFirstPage()
    {
        if (_currentPage > 0)
        {
            _currentPage = 0;
            RecomposeTable();
        }
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
        int totalPages = (int)Math.Ceiling(_searchResults.Count / (double)ItemsPerPage);
        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            RecomposeTable();
        }
        return true;
    }

    private bool OnLastPage()
    {
        int totalPages = (int)Math.Ceiling(_searchResults.Count / (double)ItemsPerPage);
        if (totalPages == 0) totalPages = 1;

        if (_currentPage < totalPages - 1)
        {
            _currentPage = totalPages - 1;
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
        _searchResults.Clear();
        RecomposeTable();
        capi.Network.GetChannel(SchematicLocatorModSystem.NetworkChannel).SendPacket(_searchRequest);
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
            _searchResults
                = _ascendingSort 
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

    public void Update(SchematicSearchResults searchResults)
    {
        _searchResults.Clear();
        _searchResults.AddRange(searchResults.Results);
        
        // Reset to page 1 whenever a new search is performed
        _currentPage = 0; 
        SortTable(_currentSort, forceRetainDirection: true);
    }
}
/// <summary>
/// A lightweight GUI element exclusively for rendering item/block models directly without drawing
/// the standard inventory slot background box.
/// </summary>
public class ItemIconElement(ICoreClientAPI capi, ElementBounds bounds, ItemStack stack) : GuiElement(capi, bounds)
{
    private readonly DummySlot _slot = new(stack);

    public override void RenderInteractiveElements(float deltaTime)
    {
        // Standard VS Item rendering expects the X/Y coordinates to represent the center.
        double x = Bounds.renderX + Bounds.InnerWidth / 2.0;
        double y = Bounds.renderY + Bounds.InnerHeight / 2.0;
        
        api.Render.RenderItemstackToGui(
            inSlot: _slot, 
            posX: x, 
            posY: y, 
            posZ: 100, 
            size: (float)Bounds.InnerWidth, 
            color: ColorUtil.WhiteArgb, 
            dt: deltaTime,
            shading: true, 
            rotate: false, 
            showStackSize: false
        );
    }
}