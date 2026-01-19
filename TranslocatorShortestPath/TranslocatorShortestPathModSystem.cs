#nullable enable

using System.Linq;
using Nf3t.VintageStory.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.TranslocatorShortestPath;

public class TranslocatorShortestPathModSystem : ModSystem
{
    public Context? Context { get; set; }

    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Client;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        Context = new Context(api);

        Context.Load();

        api.Event.LeaveWorld += () => Context.Save();

        api.Event.RegisterGameTickListener(_ => Context.Save(), 5000);
        api.Event.RegisterGameTickListener(_ =>
        {
            if (!api.Input.MouseGrabbed || api.World.Player.Entity.State != EnumEntityState.Active) return;

            var selection = api.World.Player.CurrentBlockSelection;
            if (selection == null) return;

            if (api.World.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityStaticTranslocator
                translocator)
            {
                var source = new SimplePos
                    { X = selection.Position.X, Y = selection.Position.Y, Z = selection.Position.Z };

                if (translocator.TargetLocation != null)
                {
                    var target = new SimplePos
                    {
                        X = translocator.TargetLocation.X, Y = translocator.TargetLocation.Y,
                        Z = translocator.TargetLocation.Z
                    };
                    Context.AddTranslocator(source, target);
                    Context.AddTranslocator(target, source);
                }
                else
                {
                    Context.AddTranslocator(source, null);
                }
            }
        }, 200);
        
        api.ChatCommands.Create("pathtl")
            .WithDescription("Find shortest path to coordinates using known translocators, by specifying an optional target location, and start location. Or will fall back to previous target location, and current player position.")
            .WithArgs(api.ChatCommands.Parsers.OptionalWorldPosition("goal"), api.ChatCommands.Parsers.OptionalWorldPosition("start"))
            .HandleWith(args =>
            {
                var playerPos = Context.GetPlayerPos();
                
                var goalArg = Context.GetSimplePos((Vec3d)args[0]);
                var startArg = Context.GetSimplePos((Vec3d)args[1]);
                
                if (goalArg == startArg)
                {
                    if (Context.SaveData.LastTranslocatorPathPerSavegame.TryGetValue(
                            Context.ClientApi.World.SavegameIdentifier, out var path))
                    {
                        return CreateHandle(Context, playerPos, playerPos, path.GoalPos);
                    }

                    return TextCommandResult.Error("Did not find existing history, please provide at least one argument.");
                }
                
                return CreateHandle(Context, playerPos, startArg, goalArg);
            });
        
        api.ChatCommands.Create("pathtlhist")
            .WithDescription("Find shortest path to coordinates using known translocators with the previously given value.")
            .WithArgs()
            .HandleWith(_ =>
            {
                var playerPos = Context.GetPlayerPos();

                if (Context.SaveData.LastTranslocatorPathPerSavegame.TryGetValue(
                        Context.ClientApi.World.SavegameIdentifier, out var path))
                {
                    return CreateHandle(Context, playerPos, path.StartPos, path.GoalPos);
                }

                return TextCommandResult.Error("Did not find existing history.");
            });
        
        api.ChatCommands.Create("counttl")
            .WithDescription("Give a count of currently seen translocators.")
            .WithArgs()
            .HandleWith(_ => Context.GetCollectionPerSaveCount("translocators", Context.SaveData.TranslocatorsPerSavegame));
    }

    private static TextCommandResult CreateHandle(Context context, SimplePos playerPos, SimplePos startPos, SimplePos goalPos)
    {
        var result = context.CalculatePath(playerPos, goalPos);

        var birdsEyeDistance = result.GetBirdsEyeDistance();

        if (result.IsFounded())
        {
            var steps = string.Join(" \u21D2 ",
                result.Path.Select(p => p.ToRelativeString(context.DefaultSpawnPosition)));

            return TextCommandResult.Success(
                $"<strong>Next:</strong> {result.GetNextStep()?.ToRelativeString(context.DefaultSpawnPosition, playerPos)}\n<strong>Path distance:</strong> {result.GetTotalDistance()} block(s).\n<strong>Birds eye distance:</strong> {result.GetBirdsEyeDistance()} block(s).\n<strong>Path Count:</strong> {result.Path.Count}\n<strong>Full Path:</strong> {steps}");
        }

        return TextCommandResult.Success(
            $"No translocator shortcut found. Birds eye distance {birdsEyeDistance}");
    }
}