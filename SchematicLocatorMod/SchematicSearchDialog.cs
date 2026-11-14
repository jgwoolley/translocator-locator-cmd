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

        // Bounds for the whole window (Increased width to 640 to pad the right edge)
        var contentBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, 640, 540);

        SingleComposer = capi.Gui.CreateCompo("schematicsearch", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(SchematicLocatorModSystem.Title, OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .BeginChildElements(contentBounds);

        var labelFont = CairoFont.WhiteSmallText();
        var inputFont = CairoFont.TextInput();

        // --- ROW 1: Search Block Prefix & I'm Feeling Lucky ---
        var searchLblBounds = ElementBounds.Fixed(0, 5, 120, 20);
        SingleComposer.AddStaticText("Search Block:", labelFont, searchLblBounds);
        SingleComposer.AddHoverText("The block code prefix to search for (e.g. 'loosegears-1')", labelFont, 250, searchLblBounds);
        
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 0, 200, 30), OnSearchBlockTextChanged, inputFont, "searchBlockPrefix");

        // The new I'm Feeling Lucky button
        var luckyBounds = ElementBounds.Fixed(340, 0, 150, 30);
        SingleComposer.AddSmallButton("I'm Feeling Lucky", OnFeelingLuckyClicked, luckyBounds);
        SingleComposer.AddHoverText("Populates the block field with the first match", labelFont, 250, luckyBounds);
        
        // --- ROW 2: Tree Key ---
        var treeKeyLblBounds = ElementBounds.Fixed(0, 40, 120, 20);
        SingleComposer.AddStaticText("Tree Key:", labelFont, treeKeyLblBounds);
        SingleComposer.AddHoverText("The block entity attribute tree key to filter by", labelFont, 250, treeKeyLblBounds);
        
        // YOU ARE MISSING THIS LINE:
        SingleComposer.AddTextInput(ElementBounds.Fixed(130, 35, 200, 30), (text) => _searchRequest.TreeKey = text, inputFont, "treeKey");
        
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
            // 1. ASSET LOCATION (Truncated with "..." at the start to keep the filename visible)
            string displayAsset = row.AssetLocation.Length > 27 
                ? "..." + row.AssetLocation.Substring(row.AssetLocation.Length - 24) 
                : row.AssetLocation;

            var assetBounds = ElementBounds.Fixed(0, yOffset, 250, 22);
            SingleComposer.AddButton(displayAsset, () => OnAssetLocationClicked(row.AssetLocation), assetBounds, labelFont, EnumButtonStyle.None);
            SingleComposer.AddHoverText(row.AssetLocation, labelFont, 300, assetBounds);
            
            // The block as this client knows it: its north-facing variant for the icon, else the variant itself. A schematic can
            // name a block this client does not have (a mod that is not installed here): then show its code and no icon.
            var block = capi.World.GetBlock(new AssetLocation(Utils.NorthifyAssetLocationPath(row.MatchedBlock)))
                        ?? capi.World.GetBlock(new AssetLocation(row.MatchedBlock));
            var stack = block == null ? null : new ItemStack(block);
            var blockName = stack?.GetName() ?? row.MatchedBlock;
            
            // 2. MATCHED BLOCK NAME (Truncated with "..." at the end to keep the prefix visible)
            string displayBlock = blockName.Length > 22 
                ? blockName.Substring(0, 19) + "..." 
                : blockName;

            var blockBounds = ElementBounds.Fixed(260, yOffset + 2, 220, 20);
            SingleComposer.AddButton(displayBlock, () => OnBlockLocationClicked(row.MatchedBlock), blockBounds, labelFont, EnumButtonStyle.None);
            SingleComposer.AddHoverText(blockName, labelFont, 300, blockBounds); // Added hover text here too!

            // 3. COUNT (Placed at X=490, taking up 60 width)
            SingleComposer.AddStaticText(row.Count.ToString(), labelFont, ElementBounds.Fixed(490, yOffset + 2, 60, 20));
            
            // 4. BLOCK ICON (Moved to the FAR RIGHT)
            // The content bounds is 600 wide. Placing the icon at 560 tucks it safely 
            // after the Count text and right before the window edge.
            // 4. BLOCK ICON (Moved to the FAR RIGHT)
            if (stack != null)
                SingleComposer.AddInteractiveElement(new ItemIconElement(capi, ElementBounds.Fixed(560, yOffset - 1, 24, 24), stack));

            // Increase this from 26 to 34 to give the icons more breathing room
            yOffset += 34; 
        }

        // --- PAGINATION CONTROLS ---
        var totalPages = (int)Math.Ceiling(_searchResults.Count / (double)ItemsPerPage);
        if (totalPages == 0) totalPages = 1; // Prevent "Page 1 of 0" if empty

        const int paginationY = 505; // Pushed down to make room for taller rows

        // First and Previous Buttons (Shifted X by +20)
        if (_currentPage > 0)
        {
            SingleComposer.AddSmallButton("<<", OnFirstPage, ElementBounds.Fixed(160, paginationY, 30, 25));
            SingleComposer.AddSmallButton("<", OnPrevPage, ElementBounds.Fixed(195, paginationY, 30, 25));
        }

        // Page Tracker Text (Shifted X by +20)
        var pageText = $"Page {_currentPage + 1} of {totalPages} ({_searchResults.Count} total)";
        SingleComposer.AddStaticText(pageText, labelFont, ElementBounds.Fixed(230, paginationY + 3, 200, 20));

        // Next and Last Buttons (Shifted X by +20)
        if (_currentPage < totalPages - 1)
        {
            SingleComposer.AddSmallButton(">", OnNextPage, ElementBounds.Fixed(435, paginationY, 30, 25));
            SingleComposer.AddSmallButton(">>", OnLastPage, ElementBounds.Fixed(470, paginationY, 30, 25));
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
        var request = new SchematicContentRequest
        {
            AssetLocation = assetLocation
        };
        var networkChannel = capi.Network.GetChannel(SchematicLocatorModSystem.NetworkChannel);
        networkChannel.SendPacket(request);
        capi.Logger.Debug($"SchematicContentRequest (Client): Sent '{request.AssetLocation}' request to server!");
        
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

    private bool OnFeelingLuckyClicked()
    {
        var text = _searchRequest.SearchBlockPrefix;

        // Skip the search if the text is empty or too short
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
            return true;

        // Perform the search query on demand
        var match = capi.World.Blocks 
            .FirstOrDefault(b => b.Code != null && Utils.MatchAssetLocation(capi, b.Code, text));

        // If we found something, apply it and refresh the window
        if (match == null) return true;
        
        // Extract the string path from the AssetLocation object
        _searchRequest.SearchBlockPrefix = Utils.NormalizeAssetLocationPath(match.Code.Path); 
        RecomposeTable();

        return true;
    }
    
    private void OnSearchBlockTextChanged(string text)
    {
        _searchRequest.SearchBlockPrefix = text;
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
        var x = Bounds.renderX + Bounds.InnerWidth / 2.0;
        var y = Bounds.renderY + Bounds.InnerHeight / 2.0;
        
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