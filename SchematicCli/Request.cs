namespace Nf3t.VintageStory.SchematicCli;

public record Request (string Domain, string PartialPath, string? TreeKey = null, string? TreeValue = null);