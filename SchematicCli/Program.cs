using Spectre.Console.Cli;

namespace Nf3t.VintageStory.SchematicCli;

public static class Program
{
    public static void Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config => { config.AddCommand<SearchSchematicsCommand>("schematics"); });
        app.Run(args);
    }
}