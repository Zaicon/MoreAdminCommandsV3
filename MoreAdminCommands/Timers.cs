using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using Terraria;
using Terraria.Localization;
using TShockAPI;

namespace MoreAdminCommands
{
    public class updateTimers
    {
        public static Timer timeTimer = new Timer(1000);
        public static Timer autoKillTimer = new Timer(1000);
        public static Timer viewAllTimer = new Timer(100);
        public static Timer permaBuffTimer = new Timer(100);
        public static Timer permaDebuffTimer = new Timer(100);
        public static Timer disableTimer = new Timer(100);

        public static void initializeTimers()
        {
            permaBuffTimer.Elapsed += new ElapsedEventHandler(pBTimer);
            disableTimer.Elapsed += new ElapsedEventHandler(dTimer);
            permaDebuffTimer.Elapsed += new ElapsedEventHandler(pDTimer);
            viewAllTimer.Elapsed += new ElapsedEventHandler(viewTimer);
            timeTimer.Elapsed += new ElapsedEventHandler(tTimer);
            autoKillTimer.Elapsed += new ElapsedEventHandler(aKTimer);
        }

        #region PermaBuffTimer
        public static void pBTimer(object sender, ElapsedEventArgs args)
        {
            int count = 0;
            foreach (Mplayer player in MAC.Players)
            {
                if (player.isPermabuff)
                {
                    foreach (int activeBuff in player.TSPlayer.TPlayer.buffType)
                    {
                        if (!Main.debuff[activeBuff])
                        {
                            player.TSPlayer.SetBuff(activeBuff, Int16.MaxValue);
                        }
                    }
                    count++;
                }
            }
            if (count == 0)
                permaBuffTimer.Enabled = false;
        }
        #endregion

        #region DisableTimer
        public static void dTimer(object sender, ElapsedEventArgs args)
        {
            int count = 0;
            foreach (Mplayer player in MAC.Players)
            {
                if (player.isDisabled)
                {
                    player.TSPlayer.SetBuff(47, 180);
                    count++;
                }
            }

            if (count == 0)
                disableTimer.Enabled = false;
        }
        #endregion

        #region PermaDebuffTimer
        public static void pDTimer(object sender, ElapsedEventArgs args)
        {
            int count = 0;
            foreach (Mplayer player in MAC.Players)
            {
                if (player.isPermaDebuff)
                {
                    foreach (int activeBuff in player.TSPlayer.TPlayer.buffType)
                    {
                        if (Main.debuff[activeBuff])
                        {
                            player.TSPlayer.SetBuff(activeBuff, Int16.MaxValue);
                        }
                    }
                    count++;
                }
            }
            if (count == 0)
                disableTimer.Enabled = false;
        }
        #endregion

        #region ViewTimer
        public static void viewTimer(object sender, ElapsedEventArgs args)
        {
            int count = 0;
            foreach (Mplayer player in MAC.Players)
            {
                if (player.viewAll)
                {
                    foreach (TSPlayer tply in TShock.Players)
                        try
                        {
                            int prevTeam = Main.player[tply.Index].team;
                            Main.player[tply.Index].team = MAC.viewAllTeam;
                            NetMessage.SendData((int)PacketTypes.PlayerTeam, player.Index, -1, NetworkText.Empty, tply.Index);
                            Main.player[tply.Index].team = prevTeam;
                        }
                        catch { }
                    count++;
                }

                if (count == 0)
                    viewAllTimer.Enabled = false;
            }
        }
        #endregion

        #region TimeTimer
        public static void tTimer(object sender, ElapsedEventArgs args)
        {
            if (MAC.timeFrozen)
            {
                if (Main.dayTime != MAC.freezeDayTime)
                {
                    if (MAC.timeToFreezeAt > 10000)
                    {
                        MAC.timeToFreezeAt -= 100;
                    }
                    else
                    {
                        MAC.timeToFreezeAt += 100;
                    }
                }
                TSPlayer.Server.SetTime(MAC.freezeDayTime, MAC.timeToFreezeAt);
            }
        }
        #endregion

        #region AutoKillTimer
        public static void aKTimer(object sender, ElapsedEventArgs args)
        {
            int count = 0;
            foreach (Mplayer player in MAC.Players)
            {
                if (!player.TSPlayer.Dead)
                {
                    if (player.autoKill)
                    {
                        player.TSPlayer.DamagePlayer(9999);
                    }
                }
                count++;
            }
            if (count == 0)
                autoKillTimer.Enabled = false;
        }
        #endregion
    }
}
