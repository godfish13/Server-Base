using Server_Base;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class PacketHandler
{
    public static void C_ChatHandler(PacketSession session, IPacket packet)
    {
        C_Chat chatPacket = packet as C_Chat;
        ClientSession clientSession = session as ClientSession;     // Client가 보낸 Session이 왔으므로 ClientSession으로 캐스팅

        if (clientSession.Room == null)
            return;      

        GameRoom room = clientSession.Room; // DummyClient 종료 시, Room = null 인 상태에서 JobQueue에서 null ref 오류 발생
                                            // 이를 방지하기 위해 null되지않은 room을 하나 만들어서 사용      
        room.Push(
            () => room.BroadCast(clientSession, chatPacket.chat)
        );  // BroadCast를 Action으로 JobQueue에 등록
            //clientSession.Room.BroadCast(clientSession, chatPacket.chat);   // Client가 보낸 chat을 전체에 뿌려줌        
    }
}