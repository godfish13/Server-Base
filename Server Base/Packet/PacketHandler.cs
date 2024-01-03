using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class PacketHandler
{
    public static void C_PlayerInfoRequirementHandler(PacketSession session, IPacket packet)
    {
        C_PlayerInfoRequirement p = packet as C_PlayerInfoRequirement;

        Console.WriteLine($"PlayerInfoRequirement : {p.playerID}, PlayerName : {p.name}");

        foreach (C_PlayerInfoRequirement.Skill skill in p.skills)
            Console.WriteLine($"Skill : {skill.id}, Skill lvl : {skill.level}, Skill Duration : {skill.duration}");
    }
}

