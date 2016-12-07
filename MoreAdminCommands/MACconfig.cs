using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MoreAdminCommands
{
    public class NPCdetails
    {
        public string name;
        public int amount;

        public NPCdetails(string name, int amount)
        {
            this.name = name;
            this.amount = amount;
        }
    }

    public class NPCobj
    {
        public string groupName;
        public List<NPCdetails> npcDetails;

        public NPCobj(string gNa, List<NPCdetails> nD)
        {
            groupName = gNa;
            npcDetails = nD;
        }
    }

    public class NPCset
    {
        public List<NPCobj> NPCList;
        public NPCset(List<NPCobj> NPCList)
        {
            this.NPCList = NPCList;
        }
    }

    public class MACconfig
    {
        public string defaultMuteAllReason = "Listen to find out";
        public string muteAllReason = "";
        public string redPass = "";
        public string bluePass = "";
        public string greenPass = "";
        public string yellowPass = "";

        public int maxDamage = 500;

        public bool maxDamageIgnore = false;
        public bool maxDamageKick = false;
        public bool maxDamageBan = false;

        public List<NPCset> SpawnGroupNPCs;

        public static MACconfig Read(string path)
        {
            if (!File.Exists(path))
                return new MACconfig();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static MACconfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<MACconfig>(sr.ReadToEnd());
                if (ConfigRead != null)
                    ConfigRead(cf);
                return cf;
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            SpawnGroupNPCs = new List<NPCset>();
            List<NPCobj> NPCs_Town = new List<NPCobj>();
            NPCs_Town.Add(new NPCobj("Town NPCs", new List<NPCdetails>()
            {
                new NPCdetails("guide", 1), new NPCdetails("merchant", 1), new NPCdetails("nurse", 1),
                new NPCdetails("demolitionist", 1), new NPCdetails("dryad", 1), new NPCdetails("arms dealer", 1),
                new NPCdetails("clothier", 1), new NPCdetails("mechanic", 1), new NPCdetails("goblin tinkerer", 1),
                new NPCdetails("wizard", 1), new NPCdetails("steampunker", 1), new NPCdetails("dye trader", 1),
                new NPCdetails("santa claus", 1), new NPCdetails("party girl", 1), new NPCdetails("painter", 1),
                new NPCdetails("witch doctor", 1), new NPCdetails("pirate", 1), new NPCdetails("cyborg", 1),
                new NPCdetails("tavernkeep", 1)
            }));
            SpawnGroupNPCs.Add(new NPCset(NPCs_Town));

            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }

        public static Action<MACconfig> ConfigRead;
    }
}
