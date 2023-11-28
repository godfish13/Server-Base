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
    public abstract class Packet    // 대부분의 게임은 설계할 때, buffer전체 크기 size를 첫번째 인자, packetID를 두번째 인자로 넘겨주는 경우가 많다
    {
        public ushort size;     // 메모리 절약을 위해 2byte == ushort정도만 사용함
        public ushort packetID; // packet설계는 최대한 메모리 아껴주는게 좋음

        public abstract ArraySegment<byte> WriteBuffer();
        public abstract void ReadBuffer(ArraySegment<byte> segment);
    }

    class PlayerInfoRequirement : Packet
    {
        public long playerID;

        public PlayerInfoRequirement()
        {
            this.packetID = (ushort)PacketIDEnum.PlayerInfoRequirement;
        }

        public override ArraySegment<byte> WriteBuffer()
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096); // static threadLocal CurrentBuffer 생성 및 4096크기 할당 요청
            bool success = true;
            ushort count = 0;

            // GetBytes 후 Copy 대신 openSegment에 바로 packet 입력 // 단, 유니티 옛버전에서는 TryWriteBytes가 사용불가능함! 자기가 쓰는 버전 확인해보기
            //success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count), packet.size); // 아래로 이동됨!
            count += 2; // packet.size는 ushort 타입이므로 2인것은 확정, 그러므로 첫번째로 count+= 2 함으로서 버퍼 가장 앞부분에 size의 공간을 미리 확보해둠
            success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count), (ushort)PacketIDEnum.PlayerInfoRequirement);
            count += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count), this.playerID);
            count += 8;                                                             // 시작점 재설정 + count     // 길이 재설정 - count
            success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count), count); // count == packet.size
            // Span : ArraySegment와 마찬가지로 Array의 범위를 지정하는 역할
            
            // 가장 밑줄에서 size == count를 buffer에 넣어줘야함!! 왜냐하면 최종적인 packet의 크기는 이것저것 넣고 난 뒤의 최종 count수치이기 때문!!
            if (success == false)   // 변환 실패시 null 반환
                return null;

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
            return SendBufferHelper.Close(count);  // 완성된 CurrentBuffer를 sendBuff으로 보냄         
        }

        public override void ReadBuffer(ArraySegment<byte> segment)
        {
            ushort count = 0;
            //ushort size = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
            count += 2;
            //ushort ID = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
            count += 2;

            //this.playerID = BitConverter.ToInt64(segment.Array, segment.Offset + count); 이 방식은 만일 client가 size값을 이상하게 보낼시 오류 발생
            this.playerID = BitConverter.ToInt64(new ReadOnlySpan<byte>(segment.Array, segment.Offset + count, segment.Count - count));
            // ex. writebuffer에서 size != 12로 거짓입력했을 시 segment의 완성본은 close에 의해 segment.Count != 12 가 됨
            // segment.Count - 4 != 8이므로 원래 long의 길이 8만큼 읽어야하는데 그게 성립되지 않으므로 오류 발생(Count < 12면 읽다가 OutofRange 에러, Count > 12면 이상함아무튼)
            // 이때 catch 발생, 오류 체크 가능해짐
            count += 8;
        }    
    }

    public enum PacketIDEnum
    {
        PlayerInfoRequirement = 1,
        PlayerInfoOk = 2,
    }

    class ServerSession : Session // 실제로 각 상황에서 사용될 기능 구현   // 서버에 앉혀둘 대리인
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            PlayerInfoRequirement packet = new PlayerInfoRequirement() { playerID = 1001 };

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
