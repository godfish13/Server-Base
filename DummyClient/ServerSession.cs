using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DummyClient       
{
    class Packet                // 대부분의 게임은 설계할 때, buffer전체 크기 size를 첫번째 인자, packetID를 두번째 인자로 넘겨주는 경우가 많다
    {
        public ushort size;     // 메모리 절약을 위해 2byte == ushort정도만 사용함
        public ushort packetID; // packet설계는 최대한 메모리 아껴주는게 좋음
    }

    class PlayerInfoRequirement : Packet
    {
        public long playerID;
    }

    class PlayerInfoOk : Packet
    {
        public int hp;
        public int attack;
    }

    public enum PacketID
    {
        PlayerInfoRequirement = 1,
        PlayerInfoOk = 2,
    }

    class ServerSession : Session // 실제로 각 상황에서 사용될 기능 구현   // 서버에 앉혀둘 대리인
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            PlayerInfoRequirement packet = new PlayerInfoRequirement() { packetID = (ushort)PacketID.PlayerInfoRequirement, playerID = 1001 };

            // 보낸다
            //for (int i = 0; i < 5; i++)
            {
                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096); // static threadLocal CurrentBuffer 생성 및 4096크기 할당 요청
                bool success = true;
                ushort count = 0;

                // GetBytes 후 Copy 대신 openSegment에 바로 packet 입력 // 단, 유니티 옛버전에서는 TryWriteBytes가 사용불가능함! 자기가 쓰는 버전 확인해보기
                //success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count), packet.size); // 아래로 이동됨!
                count += 2; // packet.size는 ushort 타입이므로 2인것은 확정, 그러므로 첫번째로 count+= 2 함으로서 버퍼 가장 앞부분에 size의 공간을 미리 확보해둠
                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count), packet.packetID);
                count += 2;                 
                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count), packet.playerID);
                count += 8;                                                             // 시작점 재설정 + count     // 길이 재설정 - count
                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count), count); // count == packet.size
                // 가장 밑줄에서 size == count를 buffer에 넣어줘야함!! 왜냐하면 최종적인 packet의 크기는 이것저것 넣고 난 뒤의 최종 count수치이기 때문!!

                /*  // GetBytes는 안정성이 높음, 단 그만큼 사양 많이잡아먹음 // 그러므로 GetBytes 후 Copy하는 대신에 위의 TryWriteBytes로 바로 변환
                byte[] size = BitConverter.GetBytes(packet.size);   // BitConverter : 대충 값 알아서 buffer에 넣을 byte로 변환해줌
                byte[] packetID = BitConverter.GetBytes(packet.packetID);   
                byte[] playerID = BitConverter.GetBytes(packet.playerID);   
               
                Array.Copy(size, 0, openSegment.Array, openSegment.Offset + count, 2);  // CurrentBuffer에 값들 직렬화해서 입력
                count += 2;
                Array.Copy(packetID, 0, openSegment.Array, openSegment.Offset + count, 2);
                count += 2;
                Array.Copy(playerID, 0, openSegment.Array, openSegment.Offset + count, 8);
                count += 8;             
                */
                ArraySegment<byte> sendBuff = SendBufferHelper.Close(count);  // 완성된 CurrentBuffer를 sendBuff으로 보냄

                Send(sendBuff);
                Thread.Sleep(10);
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
