using DummyClient;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class PacketHandler
{
    public static void S_BroadCastEnterGameHandler(PacketSession session, IPacket packet)
    {
        S_BroadCastEnterGame pkt = packet as S_BroadCastEnterGame;
    }

    public static void S_BroadCastLeaveGameHandler(PacketSession session, IPacket packet)
    {
        S_BroadCastLeaveGame pkt = packet as S_BroadCastLeaveGame;
    }

    public static void S_PlayerListHandler(PacketSession session, IPacket packet)
    {
        S_PlayerList pkt = packet as S_PlayerList;
    }

    public static void S_BroadCastMoveHandler(PacketSession session, IPacket packet)
    {
        S_BroadCastMove pkt = packet as S_BroadCastMove;       
    }
}
