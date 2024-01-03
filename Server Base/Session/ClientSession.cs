using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server_Base       
{
    class ClientSession : PacketSession // 실제로 각 상황에서 사용될 기능 구현     // Client에 앉혀둘 대리인
    {
        public int Sessionid { get; set; }
        public GameRoom Room { get; set; }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            Program.Room.Enter(this);
            //ToDo
        }
        //[4][.] [I][D] [][][][][][][][]
        
        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnReceivePacket(this, buffer);
            // 싱글톤으로 구현해둔 PacketManager에 연결
        }

        public override void OnDisConnected(EndPoint endPoint)
        {
            SessionManager.instance.Remove(this);
            if (Room != null)
            {
                Room.Leave(this);
                Room = null;
            }
            Console.WriteLine($"OnDisConnected : {endPoint}");
        }

        public override void OnSend(int numOfbytes)
        {
            //Console.WriteLine($"Transferred args byte : {numOfbytes}");
        }
    }
}
