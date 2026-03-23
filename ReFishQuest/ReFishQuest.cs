using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace ReFishQuest;

[ApiVersion(2, 1)]
public class ReFishQuest : TerrariaPlugin
{
    public override string Name => "ReFishQuest";
    public override string Author => "Neoslyke, 羽学, Onusai";
    public override Version Version => new Version(2, 1, 0);
    public override string Description => "Allows unlimited angler quests after completing one.";

    private static readonly HashSet<string> CompletedPlayers = new();
    public static Configuration Config { get; private set; } = new();

    public ReFishQuest(Main game) : base(game) { }

    public override void Initialize()
    {
        Config = Configuration.Load();

        GeneralHooks.ReloadEvent += OnReload;
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        ServerApi.Hooks.WorldSave.Register(this, OnWorldSave);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= OnReload;
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            ServerApi.Hooks.WorldSave.Deregister(this, OnWorldSave);
        }
        base.Dispose(disposing);
    }

    private void OnReload(ReloadEventArgs args)
    {
        Config = Configuration.Load();
        args.Player?.SendSuccessMessage("[ReFishQuest] Configuration reloaded.");
    }

    private void OnWorldSave(WorldSaveEventArgs args)
    {
        if (Config.ClearOnWorldSave)
        {
            CompletedPlayers.Clear();
        }
    }

    private void OnJoin(JoinEventArgs args)
    {
        var player = TShock.Players[args.Who];
        if (player == null) return;

        if (CompletedPlayers.Contains(player.Name))
        {
            Main.anglerWhoFinishedToday.Remove(player.Name);
        }

        NetMessage.SendAnglerQuest(player.Index);
    }

    private void OnGetData(GetDataEventArgs args)
    {
        if (!Config.Enable) return;
        if (args.MsgID != PacketTypes.CompleteAnglerQuest) return;

        var player = TShock.Players[args.Msg.whoAmI];
        if (player == null || !player.IsLoggedIn) return;

        if (Config.SwapQuestOnComplete)
        {
            SwapAnglerQuest(player);
        }

        CompletedPlayers.Add(player.Name);
        Main.anglerWhoFinishedToday.Remove(player.Name);
        NetMessage.SendAnglerQuest(player.Index);
    }

    private static void SwapAnglerQuest(TSPlayer player)
    {
        Main.anglerWhoFinishedToday.Remove(player.Name);
        Main.anglerQuestFinished = false;

        bool hasBossProgress = NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3 ||
                               Main.hardMode || NPC.downedSlimeKing || NPC.downedQueenBee;

        bool needsReroll;
        do
        {
            needsReroll = false;
            Main.anglerQuest = Main.rand.Next(Main.anglerQuestItemNetIDs.Length);
            int questItem = Main.anglerQuestItemNetIDs[Main.anglerQuest];

            needsReroll = questItem switch
            {
                2454 => !Main.hardMode || WorldGen.crimson,
                2457 => WorldGen.crimson,
                2485 => WorldGen.crimson,
                2463 => !Main.hardMode || !WorldGen.crimson,
                2477 => !WorldGen.crimson,
                2462 => !Main.hardMode,
                2465 => !Main.hardMode,
                2468 => !Main.hardMode,
                2471 => !Main.hardMode,
                2473 => !Main.hardMode,
                2480 => !Main.hardMode,
                2483 => !Main.hardMode,
                2484 => !Main.hardMode,
                2453 or 2476 => !hasBossProgress,
                _ => false
            };
        } while (needsReroll);

        NetMessage.SendAnglerQuest(player.Index);
    }
}