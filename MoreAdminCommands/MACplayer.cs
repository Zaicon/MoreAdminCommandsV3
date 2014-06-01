﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using System.Timers;

namespace MoreAdminCommands
{
    public class Mplayer
    {
        public int Index;
        public bool isHeal;
        public bool isGhost;
        public bool muted;
        public int muteTime;
        public bool viewAll;
        public bool accessRed;
        public bool accessBlue;
        public bool accessGreen;
        public bool accessYellow;
        public bool autoKill;
        public bool isPermabuff;
        public bool isPermaDebuff;
        public bool isDisabled;

        public string name { get { return Main.player[Index].name; } }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }

        public Mplayer(int index)
        {
            Index = index;
        }
    }
}
