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
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"Client Session ({this.Sessionid}) OnConnected : {endPoint}");

            Program.Room.Push(() => Program.Room.Enter(this));  // JobQueue에 등록
            //ToDo
        }
        
        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnReceivePacket(this, buffer);
            //Console.WriteLine("ClentSession received Packet");
            // 싱글톤으로 구현해둔 PacketManager에 연결
        }

        public override void OnDisConnected(EndPoint endPoint)
        {
            SessionManager.instance.Remove(this);
            if (Room != null)
            {
                GameRoom room = Room;   // DummyClient 종료 시, Room = null 인 상태에서 JobQueue의 leave에서 null ref 오류 발생
                                        // 이를 방지하기 위해 null되지않은 room을 하나 만들어서 사용
                room.Push(() => room.Leave(this));  // JobQueue에 등록
                Room = null;
            }
            Console.WriteLine($"OnDisConnected ({this.Sessionid}) : {endPoint}");
        }

        public override void OnSend(int numOfbytes)
        {
            //  Console.WriteLine($"Transferred args byte : {numOfbytes}");
        }
    }
}
