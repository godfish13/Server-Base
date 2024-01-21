using Server_Base;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class PacketHandler
{
    public static void C_LeaveGameHandler(PacketSession session, IPacket packet)
    {
        ClientSession clientSession = session as ClientSession;

        if (clientSession.Room == null)
            return;      

        GameRoom room = clientSession.Room; // session이 Room에서 leave되기 전에 client가 강제종료등으로 먼저 사라질 시,
                                            // Room = null 이 되어 Push할 JobQueue가 없어 null ref 오류 발생
                                            // 이를 방지하기 위해 Room과 같은 값을 참조하는 room을 하나 만들어서 사용      
        room.Push(() => room.Leave(clientSession));  // Action으로 JobQueue에 등록       
    }

    public static void C_MoveHandler(PacketSession session, IPacket packet)
    {
        C_Move movePacket = packet as C_Move;
        ClientSession clientSession = session as ClientSession;

        if (clientSession.Room == null)
            return;

        //if(clientSession.Sessionid == 1)
        //    Console.WriteLine($"{clientSession.Sessionid} : move to ({movePacket.posX}, {movePacket.posY}, {movePacket.posZ})");

        GameRoom room = clientSession.Room;    
        room.Push(() => room.Move(clientSession, movePacket));
    }
}