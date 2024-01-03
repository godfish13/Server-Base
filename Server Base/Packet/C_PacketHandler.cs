using Server_Base;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class C_PacketHandler
{
    public static void C_ChatHandler(PacketSession session, IPacket packet)
    {
        C_Chat chatPacket = packet as C_Chat;
        ClientSession clientSession = session as ClientSession;     // Client가 보낸 Session이 왔으므로 ClientSession으로 캐스팅

        if (clientSession.Room == null)
            return;

        clientSession.Room.BroadCast(clientSession, chatPacket.chat);   // Client가 보낸 chat을 전체에 뿌려줌
    }
}