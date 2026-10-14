#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.Sponge;

/// <summary>
/// Works client-only: `.panmap` centres the map. If the server also has this mod, players can share a
/// location, and everyone with the mod gets a clickable link in chat. The server never relays raw chat
/// markup (the server escapes it); it sends a packet and each client builds the link locally.
/// </summary>
public class CenterMapModSystem : ModSystem
{
    public const string NetworkChannel = "nf3tcentermap";
    private const int MaxNameLength = 48;
    private const int ShareCooldownMs = 2000;

    private readonly Dictionary<string, long> _lastShareByPlayer = new();

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.Network.RegisterChannel(NetworkChannel)
            .RegisterMessageType<ShareLocationPacket>()
            .RegisterMessageType<LocationSharedPacket>();
    }

    // Server

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Network.GetChannel(NetworkChannel)
            .SetMessageHandler<ShareLocationPacket>((player, packet) => OnShareRequested(api, player, packet));
    }

    private void OnShareRequested(ICoreServerAPI api, IServerPlayer player, ShareLocationPacket packet)
    {
        var now = Environment.TickCount64;
        lock (_lastShareByPlayer)
        {
            if (_lastShareByPlayer.TryGetValue(player.PlayerUID, out var last) && now - last < ShareCooldownMs) return;
            _lastShareByPlayer[player.PlayerUID] = now;
        }

        // The sender's name comes from the connection, never from the packet.
        api.Network.GetChannel(NetworkChannel).BroadcastPacket(new LocationSharedPacket
        {
            PlayerName = CleanText(player.PlayerName),
            X = packet.X,
            Y = packet.Y,
            Z = packet.Z,
            Name = CleanText(packet.Name)
        });
    }

    // Client

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Network.GetChannel(NetworkChannel)
            .SetMessageHandler<LocationSharedPacket>(packet => OnLocationShared(api, packet));

        api.RegisterLinkProtocol("panmap", component => HandlePanMapProtocol(api, component));
        api.RegisterLinkProtocol("panmapshare", component => HandleShareProtocol(api, component));

        api.ChatCommands.Create("panmap")
            .WithDescription(
                "Pans the open world map to specific coordinates and marks them, with an optional name. Does not open the map; click the link in chat for that.")
            .WithArgs(api.ChatCommands.Parsers.OptionalWorldPosition("coordinates"),
                api.ChatCommands.Parsers.OptionalAll("name"))
            .HandleWith(args => HandlePanMap(api, args));

        api.ChatCommands.Create("panmapshare")
            .WithDescription(
                "Shares coordinates with everyone on the server (needs this mod on the server too). Players with the mod get a clickable link.")
            .WithArgs(api.ChatCommands.Parsers.OptionalWorldPosition("coordinates"),
                api.ChatCommands.Parsers.OptionalAll("name"))
            .HandleWith(args => HandleShare(api, args));
    }

    private static TextCommandResult HandlePanMap(ICoreClientAPI capi, TextCommandCallingArgs args)
    {
        var coordinates = (Vec3d)args[0];
        var name = args.Parsers.Count > 1 ? args[1] as string : null;
        var (x, y, z) = ((int)coordinates.X, (int)coordinates.Y, (int)coordinates.Z);
        var clean = CleanText(name);

        // Echoed to the local chat only: sending markup with SendChatMessage does not work, since the
        // server escapes markup in player chat.
        var shareLink = PanLink(x, y, z, clean, "click here", "panmapshare");
        var panned = PanToCoordinates(capi, coordinates, clean, openIfClosed: false) != null;

        capi.ShowChatMessage(panned
            ? $"Centered on {Describe(capi, coordinates, clean)}. To share with other players, {shareLink}."
            : $"The world map is not open, {PanLink(x, y, z, clean, "click here to open selected location")}. " +
              $"To share with other players, {shareLink}.");
        return TextCommandResult.Success();
    }

    private static TextCommandResult HandleShare(ICoreClientAPI capi, TextCommandCallingArgs args)
    {
        var coordinates = (Vec3d)args[0];
        var name = args.Parsers.Count > 1 ? args[1] as string : null;
        return Share(capi, coordinates, name);
    }

    private static TextCommandResult Share(ICoreClientAPI capi, Vec3d coordinates, string? name)
    {
        var channel = capi.Network.GetChannel(NetworkChannel);
        if (!channel.Connected)
            return TextCommandResult.Error("This server does not have the Center Map mod, so locations can't be shared.");

        channel.SendPacket(new ShareLocationPacket
        {
            X = (int)coordinates.X,
            Y = (int)coordinates.Y,
            Z = (int)coordinates.Z,
            Name = CleanText(name)
        });
        return TextCommandResult.Success($"You shared a location: {Describe(capi, coordinates, name)}");
    }

    private static void OnLocationShared(ICoreClientAPI capi, LocationSharedPacket packet)
    {
        // Clean again: the server is trusted, but another mod's server could be older or different.
        var coordinates = new Vec3d(packet.X, packet.Y, packet.Z);
        var label = CleanText(packet.Name);
        capi.ShowChatMessage(
            $"<strong>{CleanText(packet.PlayerName)}</strong> shared a location: " +
            PanLink(packet.X, packet.Y, packet.Z, label, Describe(capi, coordinates, label)));
    }

    // Shown only until the map is closed (WaypointMapLayer clears temporary markers then), and never saved.
    private static void AddTemporaryMarker(GuiDialogWorldMap mapDialog, Vec3d coordinates, string? name)
    {
        var waypointLayer = mapDialog.MapLayers.FirstOrDefault(l => l is WaypointMapLayer) as WaypointMapLayer;
        waypointLayer?.AddTemporaryWaypoint(new Waypoint
        {
            Position = coordinates.Clone(),
            Title = string.IsNullOrWhiteSpace(name) ? "Panmap" : name.Trim(),
            Icon = "circle",
            Color = ColorUtil.Hex2Int("#4682B4"),
            Temporary = true,
            Guid = Guid.NewGuid().ToString()
        });
    }

    /// <returns>The centred position, or null if the map could not be centred.</returns>
    private static BlockPos? PanToCoordinates(ICoreClientAPI capi, Vec3d coordinates, string? name, bool openIfClosed)
    {
        var mapDialog = capi.Gui.LoadedGuis.Find(g => g is GuiDialogWorldMap) as GuiDialogWorldMap;
        if (mapDialog == null) return null;

        // The minimap (HUD) counts as closed: only the full map dialog is centred.
        var fullMapOpen = mapDialog.IsOpened() && mapDialog.DialogType == EnumDialogType.Dialog;
        if (!fullMapOpen)
        {
            if (!openIfClosed) return null;
            mapDialog.Open(EnumDialogType.Dialog);
        }

        if (mapDialog.SingleComposer?.GetElement("mapElem") is not GuiElementMap mapElem) return null;
        var targetPos = new BlockPos((int)coordinates.X, (int)coordinates.Y, (int)coordinates.Z);
        mapElem.CenterMapTo(targetPos);
        AddTemporaryMarker(mapDialog, coordinates, name);
        return targetPos;
    }

    // Link formats: panmap://x,y,z[,url-encoded-name] and panmapshare://x,y,z[,url-encoded-name]
    private static bool TryParseLink(string href, string protocol, out Vec3d coordinates, out string? name)
    {
        coordinates = new Vec3d();
        name = null;

        var parts = href.Replace(protocol + "://", "").Split(",", 4);
        if (parts.Length is not (3 or 4) ||
            !double.TryParse(parts[0], out var x) ||
            !double.TryParse(parts[1], out var y) ||
            !double.TryParse(parts[2], out var z))
            return false;

        coordinates = new Vec3d(x, y, z);
        if (parts.Length == 4) name = Uri.UnescapeDataString(parts[3]);
        return true;
    }

    private static void HandlePanMapProtocol(ICoreClientAPI capi, LinkTextComponent component)
    {
        if (!TryParseLink(component.Href, "panmap", out var coordinates, out var name))
            capi.ShowChatMessage("[ERROR] Invalid panmap link. Expected panmap://x,y,z[,name]");
        else if (PanToCoordinates(capi, coordinates, CleanText(name), openIfClosed: true) == null)
            capi.ShowChatMessage("[ERROR] Could not open the world map.");
    }

    private static void HandleShareProtocol(ICoreClientAPI capi, LinkTextComponent component)
    {
        if (!TryParseLink(component.Href, "panmapshare", out var coordinates, out var name))
            capi.ShowChatMessage("[ERROR] Invalid panmapshare link. Expected panmapshare://x,y,z[,name]");
        else
            capi.ShowChatMessage(Share(capi, coordinates, name).StatusMessage ?? "");
    }

    private static string PanLink(int x, int y, int z, string? name, string text, string protocol = "panmap")
    {
        var suffix = string.IsNullOrWhiteSpace(name) ? "" : "," + Uri.EscapeDataString(name.Trim());
        return $"<a href=\"{protocol}://{x},{y},{z}{suffix}\">{text}</a>";
    }

    // Coordinates are shown relative to the world spawn (like the in-game map and commands use),
    // with the real height. Links always carry absolute coordinates.
    private static string Describe(ICoreClientAPI capi, Vec3d coordinates, string? name)
    {
        var spawn = capi.World.DefaultSpawnPosition.AsBlockPos;
        var label = string.IsNullOrWhiteSpace(name) ? "" : " " + CleanText(name);
        return $"[{(int)coordinates.X - spawn.X}, {(int)coordinates.Y}, {(int)coordinates.Z - spawn.Z}]{label}";
    }

    /// <summary>Removes characters that could be read as chat markup, and bounds the length.</summary>
    private static string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var cleaned = new string(text.Where(c => !char.IsControl(c) && c is not ('<' or '>' or '&' or '"')).ToArray())
            .Trim();
        return cleaned.Length > MaxNameLength ? cleaned[..MaxNameLength] : cleaned;
    }
}
