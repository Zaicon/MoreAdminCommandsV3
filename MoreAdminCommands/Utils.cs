using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace MoreAdminCommands
{
    public class Utils
    {
        #region GetPlayers
        public static Mplayer GetPlayers(string name)
        {
			return MAC.Players.Find(p => p.name == name);
        }

        public static Mplayer GetPlayers(int index)
        {
			return MAC.Players.Find(p => p.Index == index);
        }
        #endregion

        #region SetUpConfig
        public static void SetUpConfig()
        {
            try
            {
                if (!File.Exists(MAC.savePath))
                {
                    MAC.config.Write(MAC.savePath);
                }
                else
                {
                    MAC.config = MACconfig.Read(MAC.savePath);
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid value in MoreAdminCommands.json");
                Console.ResetColor();
            }
        }
        #endregion

        #region FindIfPlaying
        public static bool findIfPlayingCommand(string text)
        {

            if (text.StartsWith("{0}playing".SFormat(TShock.Config.CommandSpecifier)) || text.StartsWith("{0}playing".SFormat(TShock.Config.CommandSilentSpecifier)))
            {

                if (text.Length == 8)
                    return true;
                else if (text[8] == ' ')
                    return true;
                else
                    return false;

            }
			else if (text.StartsWith("{0}who".SFormat(TShock.Config.CommandSpecifier)) || text.StartsWith("{0}who".SFormat(TShock.Config.CommandSilentSpecifier)))
            {

                if (text.Length == 4)
                    return true;
                else if (text[4] == ' ')
                    return true;
                else
                    return false;

            }
            else
                return false;

        }
        #endregion

        #region FindIfMe
        public static bool findIfMeCommand(string text)
        {

			if (text.StartsWith("{0}me".SFormat(TShock.Config.CommandSpecifier)) || text.StartsWith("{0}me".SFormat(TShock.Config.CommandSilentSpecifier)))
            {

                if (text.Length == 3)
                    return true;
                else if (text[3] == ' ')
                    return true;
                else
                    return false;

            }
            else
                return false;

        }
        #endregion

        #region ParseParameters
        public static List<String> parseParameters(string str)
        {
            var ret = new List<string>();
            string sb = "";
            bool instr = false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (instr)
                {
                    if (c == '\\')
                    {
                        if (i + 1 >= str.Length)
                            break;
                        c = GetEscape(str[++i]);
                    }
                    else if (c == '"')
                    {
                        ret.Add(sb);
                        sb = "";
                        instr = false;
                        continue;
                    }
                    sb += c;
                }
                else
                {
                    if (IsWhiteSpace(c))
                    {
                        if (sb.Length > 0)
                        {
                            ret.Add(sb.ToString());
                            sb = "";
                        }
                    }
                    else if (c == '"')
                    {
                        if (sb.Length > 0)
                        {
                            ret.Add(sb.ToString());
                            sb = "";
                        }
                        instr = true;
                    }
                    else
                    {
                        sb += c;
                    }
                }
            }
            if (sb.Length > 0)
                ret.Add(sb.ToString());

            return ret;
        }
        #endregion

        #region Escape character
        private static char GetEscape(char c)
        {
            switch (c)
            {
                case '\\':
                    return '\\';
                case '"':
                    return '"';
                case 't':
                    return '\t';
                default:
                    return c;
            }
        }
        #endregion

        #region WhiteSpace check
        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n';
        }
        #endregion

        #region SearchTable
        public static int SearchTable(List<object> Table, string Query)
        {
            for (int i = 0; i < Table.Count; i++)
            {
                try
                {
                    if (Query == Table[i].ToString())
                    {
                        return (i);
                    }
                }
                catch { }
            }
            return (-1);
        }
        #endregion

        #region getDistance
        public static bool getDistance(Vector2 player, Vector2 mob, int radius)
        {
            if ((int)Math.Sqrt(((int)(mob.X - player.X) ^ 2) + ((int)(mob.Y - player.Y) ^ 2)) <= radius)
                return true;
            return false;
        }
        #endregion

        #region getSpawnGroup
        public static bool getSpawnGroup(string nGroup, TSPlayer player)
        {
            bool didspawn = false;
            foreach (NPCset set in MAC.config.SpawnGroupNPCs)
                foreach (NPCobj obj in set.NPCList)
                    if (obj.groupName == nGroup)
                    {
                        foreach (NPCdetails details in obj.npcDetails)
                        {
                            NPC npc = TShock.Utils.GetNPCByIdOrName(details.name)[0];

                            TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, details.amount, player.TileX, player.TileY, 50, 20);
                        }
                        didspawn = true;
                    }
            return didspawn;
        }
        #endregion
    }
}
