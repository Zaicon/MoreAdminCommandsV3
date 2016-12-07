/*
 * Original plugin by DaGamesta.
 */

using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace MoreAdminCommands
{
    [ApiVersion(1, 26)]
    public class MAC : TerrariaPlugin
    {
        public static MACconfig config { get; set; }
        public static string savePath { get { return Path.Combine(TShock.SavePath, "MoreAdminCommands.json"); } }
        public static List<Mplayer> Players = new List<Mplayer>();

        private DateTime LastCheck = DateTime.UtcNow;
        private DateTime OtherLastCheck = DateTime.UtcNow;

        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;

        private Dictionary<int, List<DateTime>> itemspam;

        public static double timeToFreezeAt = 1000;
        public static int viewAllTeam = 4;

        public static bool timeFrozen = false;
        public static bool cansend = false;
        public static bool freezeDayTime = true;
        public static bool muteAll = false;

        public override string Name
        {
            get { return "MoreAdminCommands"; }
        }

        public override string Author
        {
            get { return "Zaicon"; }
        }

        public override string Description
        {
            get { return "Variety of commands to extend abilities on TShock"; }
        }

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        #region Initialize
        public override void Initialize()
        {
            var Hook = ServerApi.Hooks;

            Hook.GameInitialize.Register(this, OnInitialize);
            Hook.ServerChat.Register(this, OnChat);
            Hook.NetGreetPlayer.Register(this, OnJoin);
            Hook.ServerLeave.Register(this, OnLeave);
            Hook.NetGetData.Register(this, OnGetData);
        }
        #endregion

        #region Dispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var Hook = ServerApi.Hooks;

                Hook.GameInitialize.Deregister(this, OnInitialize);
                Hook.ServerChat.Deregister(this, OnChat);
                Hook.NetGreetPlayer.Deregister(this, OnJoin);
                Hook.ServerLeave.Deregister(this, OnLeave);
                Hook.NetGetData.Deregister(this, OnGetData);
            }

            base.Dispose(disposing);
        }
        #endregion

        public MAC(Main game)
            : base(game)
        {
            Order = -1;

            config = new MACconfig();
        }

        #region OnInitialize
        public void OnInitialize(EventArgs args)
        {
            SQLEditor = new SqlTableEditor(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            var table = new SqlTable("muteList",
                        new SqlColumn("Name", MySqlDbType.Text),
                        new SqlColumn("IP", MySqlDbType.Text));
            SQLWriter.EnsureTableStructure(table);

            #region Commands
            Commands.ChatCommands.Add(new Command("mac.kill", Cmds.KillAll, "killall", "kill*"));
            Commands.ChatCommands.Add(new Command("mac.kill", Cmds.AutoKill, "autokill"));
            Commands.ChatCommands.Add(new Command("mac.mute", Cmds.MuteAll, "muteall"));
            Commands.ChatCommands.Add(new Command("mac.spawn", Cmds.SpawnMobPlayer, "spawnmobplayer", "smp"));
            Commands.ChatCommands.Add(new Command("mac.spawn", Cmds.SpawnGroup, "spawngroup", "sg"));
            Commands.ChatCommands.Add(new Command("mac.spawn", Cmds.SpawnByMe, "spawnbyme", "sbm"));
            Commands.ChatCommands.Add(new Command("mac.search", Cmds.FindPerms, "findperm", "fperm"));
            Commands.ChatCommands.Add(new Command("mac.butcher", Cmds.ButcherAll, "butcherall", "butcher*"));
            Commands.ChatCommands.Add(new Command("mac.butcher", Cmds.ButcherFriendly, "butcherfriendly", "butcherf"));
            Commands.ChatCommands.Add(new Command("mac.butcher", Cmds.ButcherNPC, "butchernpc"));
            Commands.ChatCommands.Add(new Command("mac.butcher", Cmds.ButcherNear, "butchernear"));
            Commands.ChatCommands.Add(new Command("mac.heal", Cmds.AutoHeal, "autoheal"));
            Commands.ChatCommands.Add(new Command("mac.heal.all", Cmds.HealAll, "healall"));
            Commands.ChatCommands.Add(new Command("mac.moon", Cmds.MoonPhase, "moon"));
            Commands.ChatCommands.Add(new Command("mac.give", Cmds.ForceGive, "forcegive"));
            Commands.ChatCommands.Add(new Command("mac.view", Cmds.ViewAll, "view"));
            Commands.ChatCommands.Add(new Command("mac.reload", Cmds.ReloadMore, "reloadmore"));
            Commands.ChatCommands.Add(new Command("mac.freeze", Cmds.FreezeTime, "freezetime", "ft"));
            Commands.ChatCommands.Add(new Command(Cmds.TeamUnlock, "teamunlock"));
            #endregion

            Utils.SetUpConfig();

            itemspam = new Dictionary<int, List<DateTime>>();

            updateTimers.initializeTimers();
        }
        #endregion

        #region OnJoin
        public void OnJoin(GreetPlayerEventArgs args)
        {
            Players.Add(new Mplayer(args.Who));

            var player = TShock.Players[args.Who];
            var Mplayer = Utils.GetPlayers(args.Who);

            var readTableIP = SQLEditor.ReadColumn("muteList", "IP", new List<SqlValue>());

            if (readTableIP.Contains(player.IP))
            {
                Mplayer.muted = true;
                Mplayer.muteTime = -1;
                foreach (TSPlayer tsplr in TShock.Players)
                {
                    if ((tsplr.Group.HasPermission(Permissions.mute)) || (tsplr.Group.Name == "superadmin"))
                    {
                        tsplr.SendInfoMessage("A player that is on the perma-mute list is about to enter the server, and has been muted.");
                    }
                }
            }
            else
            {
                Mplayer.muteTime = -1;
                Mplayer.muted = false;
            }
        }
        #endregion

        #region OnLeave
        private void OnLeave(LeaveEventArgs args)
        {
            var player = Utils.GetPlayers(args.Who);

            Players.RemoveAll(pl => pl.Index == args.Who);
        }
        #endregion

        #region GetData
        void OnGetData(GetDataEventArgs e)
        {
            #region PlayerHP
            try
            {
                if (e.MsgID == PacketTypes.PlayerHp)
                {
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        var reader = new BinaryReader(data);
                        var playerID = reader.ReadByte();
                        var HP = reader.ReadInt16();
                        var MaxHP = reader.ReadInt16();

                        if (Utils.GetPlayers((int)playerID) != null)
                        {
                            var player = Utils.GetPlayers((int)playerID);

                            if (player.isHeal)
                            {
                                if (HP <= MaxHP / 2)
                                {
                                    player.TSPlayer.Heal(500);
                                    player.TSPlayer.SendSuccessMessage("You just got healed!");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception x)
            {
                TShock.Log.ConsoleError(x.ToString());
            }
            #endregion

            #region PlayerDamage
            if (e.MsgID == PacketTypes.PlayerDamage)
            {
                try
                {
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        var reader = new BinaryReader(data);
                        var ply = reader.ReadByte();
                        var hitDirection = reader.ReadByte();
                        var damage = reader.ReadInt16();


                        if ((damage > config.maxDamage || damage < 0) && !TShock.Players[e.Msg.whoAmI].Group.HasPermission(Permissions.ignoredamagecap) && e.Msg.whoAmI != ply)
                        {
                            if (config.maxDamageBan)
                            {
                                TShockAPI.TShock.Utils.Ban(TShock.Players[e.Msg.whoAmI], "You have exceeded the max damage limit.");
                            }
                            else if (config.maxDamageKick)
                            {
                                TShockAPI.TShock.Utils.Kick(TShock.Players[e.Msg.whoAmI], "You have exceeded the max damage limit.");
                            }
                            if (config.maxDamageIgnore)
                            {
                                e.Handled = true;
                            }

                        }
                    }
                }
                catch (Exception x)
                {
					TShock.Log.ConsoleError(x.ToString());
                }
            }
            #endregion

            #region NPCStrike
            if (e.MsgID == PacketTypes.NpcStrike)
            {
                try
                {
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        var reader = new BinaryReader(data);
                        var npcID = reader.ReadInt16();
                        var damage = reader.ReadInt16();
                        if ((damage > config.maxDamage || damage < 0) && !TShock.Players[e.Msg.whoAmI].Group.HasPermission(Permissions.ignoredamagecap))
                        {

                            if (config.maxDamageBan)
                            {
                                TShockAPI.TShock.Utils.Ban(TShock.Players[e.Msg.whoAmI], "You have exceeded the max damage limit.");
                            }
                            else if (config.maxDamageKick)
                            {
                                TShockAPI.TShock.Utils.Kick(TShock.Players[e.Msg.whoAmI], "You have exceeded the max damage limit.");
                            }
                            if (config.maxDamageIgnore)
                            {
                                e.Handled = true;
                            }
                        }
                    }
                }
                catch (Exception x)
                {
					TShock.Log.ConsoleError(x.ToString());
                }
            }
            #endregion

            #region PlayerTeam
            try
            {
                if (e.MsgID == PacketTypes.PlayerTeam)
                {
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        var reader = new BinaryReader(data);
                        var ply = reader.ReadByte();
                        var team = reader.ReadByte();

                        if (Utils.GetPlayers((int)ply) != null)
                        {
                            var player = Utils.GetPlayers((int)ply);

                            try
                            {
                                switch (team)
                                {
                                    case 1:
                                        if (config.redPass != "")
                                        {
                                            if ((!player.accessRed) && (TShock.Players[ply].Group.Name != "superadmin"))
                                            {
                                                e.Handled = true;
												TShock.Players[ply].SendErrorMessage("This team is locked, use {0}teamunlock red [password] to access it.", TShock.Config.CommandSpecifier);
                                                TShock.Players[ply].SetTeam(0);
                                            }
                                        }
                                        break;

                                    case 2:
                                        if (config.greenPass != "")
                                        {
                                            if ((!player.accessGreen) && (TShock.Players[ply].Group.Name != "superadmin"))
                                            {
                                                e.Handled = true;
												TShock.Players[ply].SendErrorMessage("This team is locked, use {0}teamunlock green [password] to access it.", TShock.Config.CommandSpecifier);
                                                TShock.Players[ply].SetTeam(0);
                                            }
                                        }
                                        break;

                                    case 3:
                                        if (config.bluePass != "")
                                        {
                                            if ((!player.accessBlue) && (TShock.Players[ply].Group.Name != "superadmin"))
                                            {
                                                e.Handled = true;
												TShock.Players[ply].SendErrorMessage("This team is locked, use {0}teamunlock blue [password] to access it.", TShock.Config.CommandSpecifier);
                                                TShock.Players[ply].SetTeam(0);
                                            }
                                        }
                                        break;

                                    case 4:
                                        if (config.yellowPass != "")
                                        {
                                            if ((!player.accessYellow) && (TShock.Players[ply].Group.Name != "superadmin"))
                                            {
                                                e.Handled = true;
                                                TShock.Players[ply].SendErrorMessage("This team is locked, use {0}teamunlock yellow [password] to access it.", TShock.Config.CommandSpecifier);
                                                TShock.Players[ply].SetTeam(0);
                                            }
                                        }
                                        break;
                                }
                            }
                            catch (Exception x)
                            {
								TShock.Log.ConsoleError(x.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception x)
            {
				TShock.Log.ConsoleError(x.ToString());
            }
            #endregion

            if (e.MsgID == PacketTypes.ItemDrop)
            {
                try
                {
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        var reader = new BinaryReader(data);
                        var itemid = reader.ReadInt16();
                        if (itemid < 400)
                            return;
                    }

                    List<DateTime> plrinfo = new List<DateTime>();
                    int index = e.Msg.whoAmI;

                    if (itemspam.ContainsKey(index))
                    {
                        plrinfo = itemspam[index];
                        plrinfo.Add(DateTime.Now);

                        if (plrinfo.Count > 10)
                        {
                            while (plrinfo.Count > 0 && (DateTime.Now - plrinfo[0]).TotalSeconds > 20)
                            {
                                plrinfo.RemoveAt(0);
                            }
                            if (plrinfo.Count > 10)
                            {
                                TShock.Players[index].SendData(PacketTypes.Status, "Do not spam items on this server!");
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                    else
                    {
                        itemspam.Add(index, new List<DateTime>() { DateTime.Now });
                    }
                }
                catch
                {

                }
            }
        }
        #endregion

        #region OnChat
        public void OnChat(ServerChatEventArgs args)
        {
            var Mplayer = Utils.GetPlayers(args.Who);

            if (muteAll && !TShock.Players[args.Who].Group.HasPermission(Permissions.mute))
            {
                var tsplr = TShock.Players[args.Who];
                if (args.Text.StartsWith(TShock.Config.CommandSpecifier) || args.Text.StartsWith(TShock.Config.CommandSilentSpecifier))
                {
                    Commands.HandleCommand(tsplr, args.Text);
                }
                else
                {
                    tsplr.SendErrorMessage("The server has been muted: " + config.muteAllReason);
                }
                args.Handled = true;
            }
        }
        #endregion
    }
}