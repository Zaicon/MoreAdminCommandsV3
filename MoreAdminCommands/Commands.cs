using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using MySql.Data.MySqlClient;
using Terraria;
using TerrariaApi;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Timers;

namespace MoreAdminCommands
{
    public class Cmds
    {
        //Searching
        #region FindCommand
        public static void FindCommand(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                List<string> commandNameList = new List<string>();

                foreach (Command command in Commands.ChatCommands)
                {
                    for (int i = 0; i < command.Permissions.Count; i++)
                    {
                        if (args.Player.Group.HasPermission(command.Permissions[i]))
                        {
                            foreach (string commandName in command.Names)
                            {
                                bool showCommand = true;
                                foreach (string searchParameter in args.Parameters)
                                {
                                    if (!commandName.Contains(searchParameter))
                                    {
                                        showCommand = false;
                                        break;
                                    }
                                }
                                if (showCommand && !commandNameList.Contains(commandName))
                                {
                                    commandNameList.Add(command.Name);
                                }
                            }
                        }
                    }
                }
                if (commandNameList.Count > 0)
                {
                    args.Player.SendMessage("The following commands matched your search:", Color.Yellow);
                    for (int i = 0; i < commandNameList.Count && i < 6; i++)
                    {
                        string returnLine = "";
                        for (int j = 0; j < commandNameList.Count - i * 5 && j < 5; j++)
                        {
                            if (i * 5 + j + 1 < commandNameList.Count)
                            {
                                returnLine += commandNameList[i * 5 + j] + ", ";
                            }
                            else
                            {
                                returnLine += commandNameList[i * 5 + j] + ".";
                            }
                        }
                        args.Player.SendInfoMessage(returnLine);
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("No Commands matched your search term(s).");
                }
            }
            else
            {
                args.Player.SendErrorMessage("Invalid syntax. Try /findcommand [term 1] (optional[term 2] [term 3] etc)");
            }
        }
        #endregion

        #region FindPermission
        public static void FindPerms(CommandArgs args)
        {
            if (args.Parameters.Count == 1)
            {
                foreach (Command cmd in TShockAPI.Commands.ChatCommands)
                {
                    if (cmd.Names.Contains(args.Parameters[0]))
                    {
                        args.Player.SendInfoMessage(string.Format("Permission to use {0}: {1}",
                            cmd.Name, cmd.Permissions[0] != "" ? cmd.Permissions[0] : "Nothing"));
                        return;
                    }
                }
                args.Player.SendErrorMessage("Command not be found.");
            }
            else
            {
                args.Player.SendErrorMessage("Invalid syntax. Try /findperm [command]");
            }
        }
        #endregion

        #region FindItem
        public static void FindItem(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax: /fitem item name");
            }
            else
            {
                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    var item = string.Join(" ", args.Parameters[i]);

                    var itemMatches = TShock.Utils.GetItemByName(item);

                    if (itemMatches.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, itemMatches.Select(I => I.name));
                    }

                    else if (itemMatches.Count == 1)
                    {
                        args.Player.SendSuccessMessage("One item matches your query: " + itemMatches[0].name);
                    }
                }
            }
        }
        #endregion

        #region FindMob
        public static void FindMob(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax: /fmob mob name");
            }
            else
            {
                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    var mob = string.Join(" ", args.Parameters[i]);

                    var mobMatches = TShock.Utils.GetNPCByName(mob);

                    if (mobMatches.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, mobMatches.Select(m => m.name));
                    }

                    else if (mobMatches.Count == 1)
                    {
                        args.Player.SendSuccessMessage("One mob matches your query: " + mobMatches[0].name);
                    }
                }
            }
        }
        #endregion


        //Kills
        #region AutoKill
        public static void AutoKill(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                var plyList = TShockAPI.TShock.Utils.FindPlayer(args.Parameters[0]);

                if (plyList.Count > 1)
                    TShock.Utils.SendMultipleMatchError(args.Player, plyList.Select(p => p.Name));
                else if (plyList.Count < 1)
                    args.Player.SendErrorMessage(plyList.Count.ToString() + " players matched.");

                else
                    if (!plyList[0].Group.HasPermission("autokill") || args.Player == plyList[0])
                    {
                        var player = Utils.GetPlayers(plyList[0].Index);

                        player.autoKill = !player.autoKill;

                        args.Player.SendSuccessMessage(string.Format("{0}abled auto-killed on {1}",
                            player.autoKill ? "En" : "Dis", player.name));
                        player.TSPlayer.SendInfoMessage(string.Format("You are {0} being auto-killed",
                            player.autoKill ? "now" : "no longer"));

                        if (player.autoKill)
                        {
                            if (!updateTimers.autoKillTimer.Enabled)
                                updateTimers.autoKillTimer.Enabled = true;
                        }
                    }
                    else
                        args.Player.SendErrorMessage("You cannot autokill someone with the autokill permission.");
            }
            else
                args.Player.SendErrorMessage("Invalid syntax: /autokill <playerName>");
        }
        #endregion

        #region KillAll
        public static void KillAll(CommandArgs args)
        {
            foreach (TSPlayer plr in TShock.Players)
            {
                if (plr != null)
                {
                    if (plr != args.Player)
                    {
                        plr.DamagePlayer(999999);
                    }
                }
            }
            TSPlayer.All.SendInfoMessage(args.Player.Name + " killed everyone!");
            args.Player.SendSuccessMessage("You killed everyone!");
        }
        #endregion

        #region TeamUnlock
        public static void TeamUnlock(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                string str = string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));

                var player = Utils.GetPlayers(args.Player.Index);

                switch (args.Parameters[0].ToLower())
                {
                    case "red":
                        if (str == MAC.config.redPass)
                        {
                            player.accessRed = true;
                            args.Player.SendErrorMessage("Red team unlocked.");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Incorrect password.");
                        }
                        break;

                    case "blue":
                        if (str == MAC.config.bluePass)
                        {
                            player.accessBlue = true;
                            args.Player.SendMessage("Blue team unlocked.", Color.LightBlue);
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Incorrect password.");
                        }
                        break;

                    case "green":
                        if (str == MAC.config.greenPass)
                        {
                            player.accessGreen = true;
                            args.Player.SendSuccessMessage("Green team unlocked.");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Incorrect password.");
                        }
                        break;

                    case "yellow":
                        if (str == MAC.config.yellowPass)
                        {
                            player.accessYellow = true;
                            args.Player.SendInfoMessage("Yellow unlocked.");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Incorrect password.");
                        }
                        break;

                    default:
                        args.Player.SendErrorMessage("Invalid team color.");
                        break;
                }
            }
            else
            {
                args.Player.SendMessage("Improper Syntax.  Proper Syntax: /teamunlock teamcolor password", Color.Red);
            }
        }
        #endregion


        //Misc server/player related
        #region FreezeTime
        public static void FreezeTime(CommandArgs args)
        {
            MAC.timeFrozen = !MAC.timeFrozen;
            MAC.freezeDayTime = Main.dayTime;
            MAC.timeToFreezeAt = Main.time;

            if (MAC.timeFrozen)
            {
                TSPlayer.All.SendInfoMessage(args.Player.Name.ToString() + " froze time.");
                if (!updateTimers.timeTimer.Enabled)
                    updateTimers.timeTimer.Enabled = true;
            }
            else
            {
                TSPlayer.All.SendInfoMessage(args.Player.Name.ToString() + " unfroze time.");
                if (updateTimers.timeTimer.Enabled)
                    updateTimers.timeTimer.Enabled = false;
            }
        }
        #endregion

        #region ForceGive
        public static void ForceGive(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage(
                    "Invalid syntax! Proper syntax: /forcegive <item type/id> <player> [item amount] [prefix id/name]");
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendErrorMessage("Missing item name/id.");
                return;
            }
            if (args.Parameters[1].Length == 0)
            {
                args.Player.SendErrorMessage("Missing player name.");
                return;
            }
            int itemAmount = 0;
            int prefix = 0;
            var items = TShock.Utils.GetItemByIdOrName(args.Parameters[0]);
            args.Parameters.RemoveAt(0);
            string plStr = args.Parameters[0];
            args.Parameters.RemoveAt(0);
            if (args.Parameters.Count == 1)
                int.TryParse(args.Parameters[0], out itemAmount);
            else if (args.Parameters.Count == 2)
            {
                int.TryParse(args.Parameters[0], out itemAmount);
                var found = TShock.Utils.GetPrefixByIdOrName(args.Parameters[1]);
                if (found.Count == 1)
                    prefix = found[0];
            }


            if (items.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid item type!");
            }
            else if (items.Count > 1)
            {
                args.Player.SendErrorMessage(string.Format("More than one ({0}) item matched!", items.Count));
            }
            else
            {
                var item = items[0];
                if (item.type >= 1 && item.type < Main.maxItemTypes)
                {
                    var players = TShockAPI.TShock.Utils.FindPlayer(plStr);
                    if (players.Count == 0)
                    {
                        args.Player.SendErrorMessage("Invalid player!");
                    }
                    else if (players.Count > 1)
                    {
                        args.Player.SendErrorMessage("More than one player matched!");
                    }
                    else
                    {
                        var plr = players[0];
                        if (itemAmount == 0 || itemAmount > item.maxStack)
                            itemAmount = item.maxStack;
                        if (plr.GiveItemCheck(item.type, item.name, item.width, item.height, itemAmount, prefix))
                        {
                            args.Player.SendSuccessMessage("Gave {0} {1} {2}(s).", plr.Name, itemAmount, item.name);
                            plr.SendSuccessMessage("{0} gave you {1} {2}(s).", args.Player.Name, itemAmount, item.name);
                        }
                        else
                        {
                            args.Player.SendErrorMessage("The item is banned and the config prevents spawning banned items.");
                        }
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("Invalid item type!");
                }
            }
        }
        #endregion

        #region AutoHeal
        public static void AutoHeal(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                var player = Utils.GetPlayers(args.Player.Index);
                player.isHeal = !player.isHeal;

                args.Player.SendSuccessMessage("Autoheal is now " + (player.isHeal ? "on" : "off"));
            }
            else
            {
                string str = args.Parameters[0];

                var findPlayers = TShockAPI.TShock.Utils.FindPlayer(str);

                if (findPlayers.Count > 1)
                {
                    List<string> foundPlayers = new List<string>();
                    foreach (TSPlayer player in findPlayers)
                    {
                        foundPlayers.Add(player.Name);
                    }

                    TShock.Utils.SendMultipleMatchError(args.Player, foundPlayers);
                }

                else if (findPlayers.Count < 1)
                {
                    args.Player.SendMessage(findPlayers.Count + " players matched.", Color.Red);
                }

                else
                {
                    var player = Utils.GetPlayers(args.Parameters[0]);
                    TShockAPI.TSPlayer ply = findPlayers[0];

                    player.isHeal = !player.isHeal;

                    if (player.isHeal)
                    {
                        args.Player.SendInfoMessage("You have activated auto-heal for " + ply.Name + ".");
                        ply.SendInfoMessage(args.Player.Name + " has activated auto-heal on you");
                    }

                    else
                    {
                        args.Player.SendInfoMessage("You have deactivated auto-heal for " + ply.Name + ".");
                        ply.SendInfoMessage(args.Player.Name + " has deactivated auto-heal on you");
                    }
                }
            }
        }
        #endregion

        #region HealAll
        public static void HealAll(CommandArgs args)
        {
            int healCount = 0;
            foreach (TSPlayer player in TShock.Players)
            {
                if (player != null && player.ConnectionAlive && player.Active)
                {
                    player.Heal();
                    healCount++;
                }
            }
            args.Player.SendSuccessMessage("Healed {0} player{1}", healCount, healCount == 0 || healCount > 1 ? "s" : "");
        }
        #endregion

        #region Ghost
        public static void Ghost(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                int tempTeam = args.Player.TPlayer.team;
                args.Player.TPlayer.team = 0;
                NetMessage.SendData(45, -1, -1, "", args.Player.Index);
                args.Player.TPlayer.team = tempTeam;

                var player = Utils.GetPlayers(args.Player.Index);

                player.isGhost = !player.isGhost;

                args.Player.SendSuccessMessage("Ghost mode {0}tivated", player.isGhost ? "ac" : "deac");

                TSPlayer.All.SendInfoMessage("{0} {1}", args.Player.Name, player.isGhost ? "has left." : "has joined.");

                float tempx = args.Player.TPlayer.position.X;
                float tempy = args.Player.TPlayer.position.Y;
                args.Player.TPlayer.position.X = 0;
                args.Player.TPlayer.position.Y = 0;
                MAC.cansend = true;
                NetMessage.SendData(13, -1, -1, "", args.Player.Index);
                MAC.cansend = false;
                //This is for SSC-enabled servers.
                args.Player.TPlayer.position.X = tempx;
                args.Player.TPlayer.position.Y = tempy;
            }
            else
            {
                string str = "";
                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    if (i != args.Parameters.Count - 1)
                    {
                        str += args.Parameters[i] + " ";
                    }
                    else
                    {
                        str += args.Parameters[i];
                    }
                }

                var playerList = TShockAPI.TShock.Utils.FindPlayer(str);

                if (playerList.Count > 1)
                {
                    List<string> foundPlayers = new List<string>();
                    foreach (TSPlayer player in playerList)
                    {
                        foundPlayers.Add(player.Name);
                    }
                    TShock.Utils.SendMultipleMatchError(args.Player, foundPlayers);
                }

                else if (playerList.Count < 1)
                {
                    args.Player.SendErrorMessage(playerList.Count.ToString() + " players matched.");
                }

                else
                {
                    TSPlayer Player = playerList[0];
                    int tempTeam = Player.TPlayer.team;
                    Player.TPlayer.team = 0;
                    NetMessage.SendData(45, -1, -1, "", Player.Index);
                    Player.TPlayer.team = tempTeam;
                    var Mplayer = Utils.GetPlayers(Player.Index);

                    Mplayer.isGhost = !Mplayer.isGhost;

                    args.Player.SendSuccessMessage("Ghost mode {0}tivated for {1}.", Mplayer.isGhost ? "ac" : "deac", Mplayer.name);

                    Player.SendInfoMessage("{0} has {1}tivated ghost mode on you", args.Player.Name, Mplayer.isGhost ? "ac" : "deac");

                    TSPlayer.All.SendInfoMessage("{0} {1}", Mplayer.name, Mplayer.isGhost ? "has left." : "has joined.");
                    
                    float tempx = Player.TPlayer.position.X;
                    float tempy = Player.TPlayer.position.Y;
                    Player.TPlayer.position.X = 0;
                    Player.TPlayer.position.Y = 0;
                    MAC.cansend = true;
                    NetMessage.SendData(13, -1, -1, "", Player.Index);
                    MAC.cansend = false;
                    //This is for SSC-enabled servers.
                    Player.TPlayer.position.X = tempx;
                    Player.TPlayer.position.Y = tempy;
                }
            }
        }
        #endregion

        #region MoonPhase
        public static void MoonPhase(CommandArgs args)
        {
            int phase;
            bool result = Int32.TryParse(args.Parameters[0], out phase);
            if (result && phase > -1 && phase < 8 && args.Parameters.Count > 0)
            {
                string phaseName = "";
                Main.moonPhase = phase;

                #region PhaseName
                switch (phase)
                {
                    case 0:
                        phaseName = "full";
                        break;
                    case 1:
                        phaseName = "3/4";
                        break;
                    case 2:
                        phaseName = "1/2";
                        break;
                    case 3:
                        phaseName = "1/4";
                        break;
                    case 4:
                        phaseName = "new";
                        break;
                    case 5:
                        phaseName = "1/4";
                        break;
                    case 6:
                        phaseName = "1/2";
                        break;
                    case 7:
                        phaseName = "3/4";
                        break;
                }
                #endregion

                TSPlayer.All.SendInfoMessage("Moon phase set to {0}.", phaseName);
            }
            else
                args.Player.SendErrorMessage("Invalid usage! Proper usage: /moon [0-7]");
        }
        #endregion

        #region Reload
        public static void ReloadMore(CommandArgs args)
        {
            Utils.SetUpConfig();
            args.Player.SendInfoMessage("Reloaded MoreAdminCommands config file");
        }
        #endregion


        //Buffs
        #region Permabuff
        public static void Permabuff(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                var player = Utils.GetPlayers(args.Player.Index);
                player.isPermabuff = !player.isPermabuff;

                args.Player.SendSuccessMessage("Permabuffs are now " + (player.isPermabuff ? "on" : "off"));
            }
            else
            {
                string str = args.Parameters[0];

                var findPlayers = TShockAPI.TShock.Utils.FindPlayer(str);

                if (findPlayers.Count > 1)
                {
                    List<string> foundPlayers = new List<string>();
                    foreach (TSPlayer player in findPlayers)
                    {
                        foundPlayers.Add(player.Name);
                    }

                    TShock.Utils.SendMultipleMatchError(args.Player, foundPlayers);
                }

                else if (findPlayers.Count < 1)
                {
                    args.Player.SendErrorMessage(findPlayers.Count + " players matched.");
                }

                else
                {
                    var player = Utils.GetPlayers(args.Parameters[0]);
                    TShockAPI.TSPlayer ply = findPlayers[0];

                    player.isPermabuff = !player.isPermabuff;

                    args.Player.SendSuccessMessage(string.Format("You have {0}tivated permabuffs on {1}.",
                        (player.isPermabuff ? "ac" : "deac"), ply.Name));

                    ply.SendInfoMessage(string.Format("{0} has {1}tivated permabuffs on you",
                        args.Player.Name, (player.isPermabuff ? "ac" : "deac")));

                    if (player.isPermabuff)
                        if (!updateTimers.permaBuffTimer.Enabled)
                            updateTimers.permaBuffTimer.Enabled = true;
                }
            }
        }
        #endregion

        #region PermaDebuff
        public static void permDebuff(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                var player = Utils.GetPlayers(args.Player.Index);

                player.isPermaDebuff = !player.isPermaDebuff;

                args.Player.SendSuccessMessage("Permanent debuffs are now " + (player.isPermaDebuff ? "on" : "off"));
            }
            else
            {
                string str = args.Parameters[0];

                var findPlayers = TShockAPI.TShock.Utils.FindPlayer(str);

                if (findPlayers.Count > 1)
                {
                    List<string> foundPlayers = new List<string>();
                    foreach (TSPlayer player in findPlayers)
                    {
                        foundPlayers.Add(player.Name);
                    }

                    TShock.Utils.SendMultipleMatchError(args.Player, foundPlayers);
                }

                else if (findPlayers.Count < 1)
                {
                    args.Player.SendErrorMessage(findPlayers.Count + " players matched.");
                }

                else
                {
                    var player = Utils.GetPlayers(args.Parameters[0]);

                    player.isPermaDebuff = !player.isPermaDebuff;

                    args.Player.SendSuccessMessage(string.Format("You have {0}tivated permanent debuffs on {1}!",
                        player.isPermaDebuff ? "ac" : "deac", player.name));

                    player.TSPlayer.SendInfoMessage(string.Format("{0} has {1}tivated permament debuffs on you",
                       args.Player, player.isPermaDebuff ? "ac" : "deac"));

                    if (player.isPermaDebuff)
                        if (!updateTimers.permaDebuffTimer.Enabled)
                            updateTimers.permaDebuffTimer.Enabled = true;
                }
            }
        }
        #endregion


        //Spawning
        #region SpawnGroup
        public static void SpawnGroup(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
                args.Player.SendErrorMessage("Invalid syntax: /spawngroup <npcGroupName>");
            else
            {
                bool didspawn = false;
                string nGroup = string.Join(" ", args.Parameters[0]);
                didspawn = Utils.getSpawnGroup(nGroup, args.Player);
                if (didspawn)
                    args.Player.SendSuccessMessage("Mobs spawned successfully");
                else
                    args.Player.SendErrorMessage("Command failed! Try a different group name.");
            }
        }
        #endregion

        #region SpawnMobPlayer
        public static void SpawnMobPlayer(CommandArgs args)
        {
            if (args.Parameters.Count != 3)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /smp <mob name/id> <username> <amount>");
            }
            else
            {
                if (TShock.Utils.FindPlayer(args.Parameters[1]).Count != 0)
                {
                    var player = TShock.Utils.FindPlayer(args.Parameters[1])[0];
                    
                    int mobID;

                    if (int.TryParse(args.Parameters[0], out mobID))
                    {
                        if (mobID <= 377)
                        {
                            NPC npc = TShock.Utils.GetNPCById(mobID);

                            int amount;
                            if (int.TryParse(args.Parameters[2], out amount))
                            {
                                TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, player.TileX, player.TileY, 50, 20);
                                TSPlayer.All.SendSuccessMessage("{0} was spawned {1} time{2} near {3}",
                                    npc.name, amount, amount > 1 || amount == 0 ? "s" : "", player.Name);
                            }
                            else
                            {
                                TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, player.TileX, player.TileY, 50, 20);
                                TSPlayer.All.SendSuccessMessage("{0} was spawned 1 time near {3}",
                                     npc.name, player.Name);
                            }
                        }
                        else
                            args.Player.SendErrorMessage("Invalid NPC ID.");
                    }
                    /*if (int.TryParse(args.Parameters[0], out mobID))
                    {
                        if (TShock.Utils.GetNPCById(mobID.ToString()).Count != 0)
                        {
                            NPC npc = TShock.Utils.GetNPCByIdOrName(mobID.ToString())[0];

                            int amount;
                            if (int.TryParse(args.Parameters[2], out amount))
                            {
                                TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, player.TileX, player.TileY, 50, 20);
                                TSPlayer.All.SendSuccessMessage("{0} was spawned {1} time{2} near {3}",
                                    npc.name, amount, amount > 1 || amount == 0 ? "s" : "", player.Name);
                            }
                            else
                            {
                                TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, player.TileX, player.TileY, 50, 20);
                                TSPlayer.All.SendSuccessMessage("{0} was spawned 1 time near {3}",
                                     npc.name, player.Name);
                            }
                        }
                        else
                            args.Player.SendErrorMessage("Invalid NPC ID.");
                    }*/
                    else
                    {
                        if (TShock.Utils.GetNPCByName(args.Parameters[0]).Count != 0)
                        {
                            NPC npc = TShock.Utils.GetNPCByName(args.Parameters[0])[0];

                            int amount;
                            if (int.TryParse(args.Parameters[2], out amount))
                            {
                                TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, player.TileX, player.TileY, 50, 20);
                                TSPlayer.All.SendSuccessMessage("{0} was spawned {1} time{2} near {3}",
                                    npc.name, amount, amount > 1 || amount == 0 ? "s" : "", player.Name);
                            }
                            else
                            {
                                TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, player.TileX, player.TileY, 50, 20);
                                TSPlayer.All.SendSuccessMessage("{0} was spawned 1 time near {1}",
                                     npc.name, player.Name);
                            }
                        }
                        else
                            args.Player.SendErrorMessage("Invalid NPC name.");
                    }
                }
                else
                    args.Player.SendErrorMessage("Player not found.");
            }
            /*
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendMessage("Missing mob name/id", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 3 && !int.TryParse(args.Parameters[1], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <mob name/id> [amount] [username]", Color.Red);
                return;
            }

            amount = Math.Min(amount, Main.maxNPCs);

            var npcs = TShockAPI.TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
            var players = TShockAPI.TShock.Utils.FindPlayer(args.Parameters[2]);
            if (players.Count == 0)
            {
                args.Player.SendMessage("Invalid player!", Color.Red);
            }
            else if (players.Count > 1)
            {
                args.Player.SendMessage("More than one player matched!", Color.Red);
            }
            else if (npcs.Count == 0)
            {
                args.Player.SendMessage("Invalid mob type!", Color.Red);
            }
            else if (npcs.Count > 1)
            {
                args.Player.SendMessage(string.Format("More than one ({0}) mob matched!", npcs.Count), Color.Red);
            }
            else
            {
                var npc = npcs[0];
                if (npc.type >= 1 && npc.type < Main.maxNPCTypes)
                {
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, players[0].TileX, players[0].TileY, 50, 20);
                    TSPlayer.All.SendInfoMessage(string.Format("{0} was spawned {1} time(s) nearby {2}.", npc.name, amount, players[0].Name));
                }
                else
                    args.Player.SendMessage("Invalid mob type!", Color.Red);
            }*/
        }
        #endregion

        #region SpawnByMe
        public static void SpawnByMe(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
                args.Player.SendErrorMessage("Invalid syntax. Try /sbm <mob name/ID> [amount]");
            else
            {
                int mobID;
                if (int.TryParse(args.Parameters[0], out mobID))
                {
                    if (TShock.Utils.GetNPCByIdOrName(mobID.ToString()).Count != 0)
                    {
                        NPC npc = TShock.Utils.GetNPCByIdOrName(mobID.ToString())[0];

                        int amount;
                        if (int.TryParse(args.Parameters[1], out amount))
                        {
                            TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, args.Player.TileX,
                                args.Player.TileY, 2, 5);
                            TSPlayer.All.SendSuccessMessage("Spawned {0} {1}{2}",
                                     amount, npc.name, amount > 1 || amount == 0 ? "'s" : "");
                        }
                        else
                        {
                            TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, args.Player.TileX,
                                args.Player.TileY, 2, 5);
                            TSPlayer.All.SendSuccessMessage("Spawned 1 {0}", npc.name);
                        }
                    }
                    else
                        args.Player.SendErrorMessage("Invalid mob ID or name");
                }
                else
                {
                    if (TShock.Utils.GetNPCByIdOrName(args.Parameters[0]).Count != 0)
                    {
                        NPC npc = TShock.Utils.GetNPCByIdOrName(args.Parameters[0])[0];

                        int amount;
                        if (int.TryParse(args.Parameters[1], out amount))
                        {
                            TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, args.Player.TileX,
                                args.Player.TileY, 2, 5);
                            TSPlayer.All.SendSuccessMessage("Spawned {0} {1}{2}",
                                     amount, npc.name, amount > 1 || amount == 0 ? "'s" : "");
                        }
                        else
                        {
                            TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, args.Player.TileX,
                                args.Player.TileY, 2, 5);
                            TSPlayer.All.SendSuccessMessage("Spawned 1 {0}", npc.name);
                        }
                    }
                    else
                        args.Player.SendErrorMessage("Invalid mob ID or name");
                }
            }
        }
        #endregion


        //Player management
        #region MuteAll
        public static void MuteAll(CommandArgs args)
        {
            int muteCount = 0;

            MAC.muteAll = !MAC.muteAll;
            if (MAC.muteAll)
            {
                MAC.config.muteAllReason = "";
                MAC.config.muteAllReason = string.Join(" ", args.Parameters);

                if (MAC.config.muteAllReason == "")
                    MAC.config.muteAllReason = MAC.config.defaultMuteAllReason;

                foreach (TSPlayer player in TShock.Players)
                    if (player != null && player.Active && player.ConnectionAlive &&
                        !player.mute && !player.Group.HasPermission(Permissions.mute))
                    {
                        player.mute = true;
                        muteCount++;
                    }

                TSPlayer.All.SendInfoMessage(args.Player.Name + " has muted everyone.");
                args.Player.SendSuccessMessage("You have muted everyone ({0} people) without the mute permission. " +
                    "They will remain muted until you use /muteall again.", muteCount);

            }
            else
            {
                foreach (TSPlayer player in TShock.Players)
                    if (player.mute)
                        player.mute = false;

                TSPlayer.All.SendInfoMessage(args.Player.Name + 
                    " has unmuted everyone, except perhaps those muted before everyone was muted.");
            }
        }
        #endregion

        #region Permamute
        public static void PermaMute(CommandArgs args)
        {
            if (args.Parameters.Count() > 0)
            {
                var tply = TShockAPI.TShock.Utils.FindPlayer(args.Parameters[0]);
                var readTableName = MAC.SQLEditor.ReadColumn("muteList", "Name", new List<SqlValue>());
                var readTableIP = MAC.SQLEditor.ReadColumn("muteList", "IP", new List<SqlValue>());

                if (tply.Count() > 1)
                {
                    TShock.Utils.SendMultipleMatchError(args.Player, tply);
                }

                else if (tply.Count() == 1)
                {
                    if (readTableName.Contains(args.Parameters[0].ToLower()))
                    {
                        List<SqlValue> List = new List<SqlValue>();
                        List<SqlValue> where = new List<SqlValue>();
                        where.Add(new SqlValue("Name", "'" + args.Parameters[0].ToLower() + "'"));
                        MAC.SQLWriter.DeleteRow("muteList", where);
                        args.Player.SendInfoMessage(args.Parameters[0] +
                            " has been successfully been removed from the perma-mute list.");
                    }
                    else
                    {
                        args.Player.SendErrorMessage("No players found under that name on the server or in the perma-mute list.");
                    }
                }

                else
                {
                    var player = Utils.GetPlayers(tply[0].Index);
                    player.muteTime = -1;
                    string str = tply[0].Name.ToLower();
                    int index = Utils.SearchTable(MAC.SQLEditor.ReadColumn("muteList", "Name", new List<SqlValue>()), str);

                    if (index == -1)
                    {
                        List<SqlValue> theList = new List<SqlValue>();
                        theList.Add(new SqlValue("Name", "'" + str + "'"));
                        theList.Add(new SqlValue("IP", "'" + tply[0].IP + "'"));
                        MAC.SQLEditor.InsertValues("muteList", theList);
                        player.muted = true;
                        args.Player.SendInfoMessage(tply[0].Name + " has been permamuted by his/her IP Address.");
                        tply[0].SendErrorMessage("You have been muted by an admin.");
                    }

                    else
                    {
                        List<SqlValue> where = new List<SqlValue>();
                        where.Add(new SqlValue("IP", "'" + tply[0].IP + "'"));
                        MAC.SQLWriter.DeleteRow("muteList", where);
                        player.muted = false;
                        args.Player.SendInfoMessage(tply[0].Name + " has been taken off the perma-mute list, and is now un-muted.");
                        tply[0].SendInfoMessage("You have been unmuted.");
                    }
                }
            }

            else
            {
                args.Player.SendErrorMessage("Improper Syntax.  Proper Syntax: /permamute player");
            }
        }
        #endregion

        #region ViewAll
        public static void ViewAll(CommandArgs args)
        {
            var player = Utils.GetPlayers(args.Player.Index);

            player.viewAll = !player.viewAll;

            if (player.viewAll)
            {
                args.Player.SendInfoMessage("View All mode has been turned on.");
                if (!updateTimers.viewAllTimer.Enabled)
                    updateTimers.viewAllTimer.Enabled = true;
            }

            else
            {
                args.Player.SetTeam(Main.player[args.Player.Index].team);
                foreach (TSPlayer tply in TShock.Players)
                {
                    NetMessage.SendData((int)PacketTypes.PlayerTeam, args.Player.Index, -1, "", tply.Index);
                }
                args.Player.SendInfoMessage("View All mode has been turned off.");
            }
        }
        #endregion

        #region Disable
        /*public static void Disable(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /disable [player name]");
                return;
            }
            var foundplr = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (foundplr.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid player!");
                return;
            }
            else if (foundplr.Count > 1)
            {
                TShock.Utils.SendMultipleMatchError(args.Player, foundplr);
                return;
            }
            else
            {
                var player = Utils.GetPlayers(foundplr[0].Index);
                player.isDisabled = !player.isDisabled;

                if (player.isDisabled)
                    if (!updateTimers.disableTimer.Enabled)
                        updateTimers.disableTimer.Enabled = true;

                args.Player.SendSuccessMessage(string.Format("{0}abled {1}!", player.isDisabled ? "Dis" : "En", player.name));
                foundplr[0].SendMessage(string.Format("{0} {1}abled you!", args.Player.Name, player.isDisabled ? "dis" : "en"), Color.Red);
            }
        }*/
        #endregion


        //Butchering
        #region ButcherNear
        public static void ButcherNear(CommandArgs args)
        {
            int nearby = 50;
            if (args.Parameters.Count > 0)
            {
                if (!int.TryParse(args.Parameters[0], out nearby))
                {
                    args.Player.SendErrorMessage("Improper Syntax. Proper Syntax: /butchernear [distance]"); 
                    return;
                }
            }
            int killcount = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if ((Main.npc[i].active && !Main.npc[i].friendly &&
                    Utils.getDistance(args.Player.TPlayer.position, Main.npc[i].position, nearby)))
                {
                    TSPlayer.Server.StrikeNPC(i, 99999, 1f, 1);
                    killcount++;
                }
            }
            args.Player.SendInfoMessage(string.Format("Killed {0} NPC(s) within a radius of {1} blocks.", killcount, nearby));
            TSPlayer.All.SendInfoMessage(string.Format("{0} killed {1} NPC{2}", args.Player.Name, killcount,
                killcount == 0 || killcount > 1 ? "s" : ""));
        }
        #endregion

        #region ButcherAll
        public static void ButcherAll(CommandArgs args)
        {
            int killcount = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active)
                {
                    TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
                    killcount++;
                }
            }
            TSPlayer.All.SendInfoMessage(string.Format("{0} killed {1} NPC{2}.", args.Player.Name, killcount,
                killcount == 0 || killcount > 1 ? "s" : ""));
        }
        #endregion

        #region ButcherFriendly
        public static void ButcherFriendly(CommandArgs args)
        {
            int killcount = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].active && Main.npc[i].townNPC)
                {
                    TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
                    killcount++;
                }
            }
            args.Player.SendInfoMessage(string.Format("Killed {0} friendly NPCs.", killcount));
            TSPlayer.All.SendInfoMessage(string.Format("{0} killed {1} friendly NPCs", args.Player.Name, killcount));
        }
        #endregion

        #region ButcherNPC
        public static void ButcherNPC(CommandArgs args)
        {

            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /butchernpc <npc name/id>", Color.Red);
                return;
            }

            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendMessage("Missing npc name/id", Color.Red);
                return;
            }

            var npcs = TShockAPI.TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);

            if (npcs.Count == 0)
            {
                args.Player.SendMessage("Invalid npc type!", Color.Red);
            }

            else if (npcs.Count > 1)
            {
                List<string> npcNames = new List<string>();
                foreach (NPC npc in npcs)
                    npcNames.Add(npc.name);

                TShock.Utils.SendMultipleMatchError(args.Player, npcNames);
            }

            else
            {
                var npc = npcs[0];

                if (npc.type >= 1 && npc.type < Main.maxNPCTypes)
                {
                    int killcount = 0;

                    for (int i = 0; i < Main.npc.Length; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].type == npc.type)
                        {
                            TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
                            killcount++;
                        }
                    }

                    args.Player.SendInfoMessage(string.Format("Killed {0} " + npc.name + "(s).", killcount));
                    TSPlayer.All.SendInfoMessage(string.Format("{0} {1}(s) were killed", npc.name, killcount));
                }

                else
                    args.Player.SendMessage("Invalid npc type!", Color.Red);
            }
        }
        #endregion
    }
}
