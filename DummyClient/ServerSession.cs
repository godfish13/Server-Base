using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DummyClient       
{
    class ServerSession : Session // 실제로 각 상황에서 사용될 기능 구현   // 서버에 앉혀둘 대리인
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            C_PlayerInfoRequirement packet = new C_PlayerInfoRequirement() { playerID = 1001, name = "ABCD", };

            var Skill = new C_PlayerInfoRequirement.Skill() { id = 501, level = 5, duration = 5 };
            Skill.attributes.Add(new C_PlayerInfoRequirement.Skill.Attribute() { att = 11 });
            packet.skills.Add(Skill);

            packet.skills.Add(new C_PlayerInfoRequirement.Skill() { id = 101, level = 1, duration = 3.0f });
            packet.skills.Add(new C_PlayerInfoRequirement.Skill() { id = 201, level = 2, duration = 2.5f });
            packet.skills.Add(new C_PlayerInfoRequirement.Skill() { id = 301, level = 3, duration = 2.0f });
            packet.skills.Add(new C_PlayerInfoRequirement.Skill() { id = 401, level = 4, duration = 1.5f });

            // 보낸다
            //for (int i = 0; i < 5; i++)
            {
                ArraySegment<byte> segment = packet.WriteBuffer();
                if (segment != null)
                    Send(segment);

                //Thread.Sleep(10);
            }
        }

        public override void OnDisConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnected : {endPoint}");
        }

        public override int OnReceive(ArraySegment<byte> buffer)
        {
            string receiveData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count); // init에서 SetBuffer로 설정한 값 추출하는 args 변수들
            Console.WriteLine($"[From Server] : {receiveData}");
            return buffer.Count;
        }

        public override void OnSend(int numOfbytes)
        {
            Console.WriteLine($"Transferred args byte : {numOfbytes}");
        }
    }
}