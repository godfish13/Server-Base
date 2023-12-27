using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Base
{
    class PacketHandler
    {
        public static void PlayerInfoReqHandler(PacketSession session, IPacket packet)
        {
            PlayerInfoRequirement p = packet as PlayerInfoRequirement;

            Console.WriteLine($"PlayerInfoRequirement : {p.playerID}, PlayerName : {p.name}");

            foreach (PlayerInfoRequirement.Skill skill in p.skills)
                Console.WriteLine($"Skill : {skill.id}, Skill lvl : {skill.level}, Skill Duration : {skill.duration}");
        }
    }
}
