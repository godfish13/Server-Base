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
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            Thread.Sleep(5000);

            DisConnect();
        }
        //[4][.] [I][D] [][][][][][][][]
        
        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            ushort count = 0;
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;
            ushort ID = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            switch ((PacketIDEnum)ID)
            {
                case PacketIDEnum.PlayerInfoRequirement:
                    {
                        PlayerInfoRequirement p = new PlayerInfoRequirement();
                        p.ReadBuffer(buffer);
                        Console.WriteLine($"PlayerInfoRequirement : {p.playerID}, PlayerName : {p.name}");

                        foreach (PlayerInfoRequirement.Skill skill in p.skills)
                            Console.WriteLine($"Skill : {skill.id}, Skill lvl : {skill.level}, Skill Duration : {skill.duration}");
                    }
                    break;
                /*case PacketIDEnum.PlayerInfoOk:
                    {

                    }
                    break*/
            }

            Console.WriteLine($"ReceivePacketID : {ID}, size : {size}");
        }

        public override void OnDisConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnected : {endPoint}");
        }

        public override void OnSend(int numOfbytes)
        {
            Console.WriteLine($"Transferred args byte : {numOfbytes}");
        }
    }
}
