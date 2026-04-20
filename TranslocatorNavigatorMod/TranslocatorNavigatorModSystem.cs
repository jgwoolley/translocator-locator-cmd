#nullable enable

using System.Linq;
using Nf3t.VintageStory.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.TranslocatorShortestPath;

public class TranslocatorNavigatorModSystem : ModSystem
{
    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Client;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        var context = new Context(api);

        context.Load();

        CreateGameTickListeners(api, context);
        CreatePathCommand(api, context);
        CreatePathHistoryCommand(api, context);
        CreateCountTranslocatorCommand(api, context);
    }

    private static void CreateGameTickListeners(ICoreClientAPI api, Context context)
    {
        api.Event.LeaveWorld += () => context.Save();

        api.Event.RegisterGameTickListener(_ => context.Save(), 5000);
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
                    context.AddTranslocator(source, target);
                    context.AddTranslocator(target, source);
                }
                else
                {
                    context.AddTranslocator(source, null);
                }
            }
        }, 200);
    }

    private static void CreatePathCommand(ICoreClientAPI api, Context context)
    {
        api.ChatCommands.Create("pathtl")
            .WithDescription(
                "Find shortest path to coordinates using known translocators, by specifying an optional target location, and start location. Or will fall back to previous target location, and current player position.")
            .WithArgs(api.ChatCommands.Parsers.OptionalWorldPosition("goal"),
                api.ChatCommands.Parsers.OptionalWorldPosition("start"))
            .HandleWith(args =>
            {
                var playerPos = context.GetPlayerPos();

                var goalArg = Context.GetSimplePos((Vec3d)args[0]);
                var startArg = Context.GetSimplePos((Vec3d)args[1]);

                if (goalArg == startArg)
                {
                    if (context.SaveData.LastTranslocatorPathPerSavegame.TryGetValue(
                            context.ClientApi.World.SavegameIdentifier, out var path))
                        return CreateHandle(context, playerPos, path.GoalPos);

                    return TextCommandResult.Error(
                        "Did not find existing history, please provide at least one argument.");
                }

                return CreateHandle(context, playerPos, goalArg);
            });
    }

    private static void CreatePathHistoryCommand(ICoreClientAPI api, Context context)
    {
        api.ChatCommands.Create("pathtlhist")
            .WithDescription(
                "Find shortest path to coordinates using known translocators with the previously given start and target location. Fails if none found.")
            .WithArgs()
            .HandleWith(_ =>
            {
                var playerPos = context.GetPlayerPos();

                if (context.SaveData.LastTranslocatorPathPerSavegame.TryGetValue(
                        context.ClientApi.World.SavegameIdentifier, out var path))
                    return CreateHandle(context, playerPos, path.GoalPos);

                return TextCommandResult.Error("Did not find existing history.");
            });
    }

    private static void CreateCountTranslocatorCommand(ICoreClientAPI api, Context context)
    {
        api.ChatCommands.Create("counttl")
            .WithDescription(
                "Counts the translocators seen by this mod within this world, as well as any others. Translocators are stored on the client system to prevent duplication.")
            .WithArgs()
            .HandleWith(_ =>
                context.GetCollectionPerSaveCount("translocators", context.SaveData.TranslocatorsPerSavegame));
    }

    private static TextCommandResult CreateHandle(Context context, SimplePos playerPos,
        SimplePos goalPos)
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