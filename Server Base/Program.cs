using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using ServerCore;

namespace Server_Base
{
    class Packet                // 대부분의 게임은 설계할 때, size를 첫번째 인자, packetID를 두번째 인자로 넘겨주는 경우가 많다
    {
        public ushort size;     // 메모리 절약을 위해 2byte == ushort정도만 사용함
        public ushort packetID; // packet설계는 최대한 메모리 아껴주는게 좋음
    }

    class GameSession : PacketSession // 실제로 각 상황에서 사용될 기능 구현
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            /*Packet packet = new Packet() { size = 100, packetID = 10};

            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            byte[] buffer1 = BitConverter.GetBytes(packet.size);   // BitConverter : 대충 값 알아서 buffer에 넣을 byte로 변환해줌
            byte[] buffer2 = BitConverter.GetBytes(packet.packetID);
            Array.Copy(buffer1, 0, openSegment.Array, openSegment.Offset, buffer1.Length);
            Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer1.Length, buffer2.Length);
            ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer1.Length + buffer2.Length);

            Send(sendBuff);*/
            Thread.Sleep(5000);

            DisConnect();
        }

        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            ushort ID = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
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

    internal class Program
    {
        static Listener _listener = new Listener();

        static void Main(string[] args)
        {
            // DNS (Domain Name System) : 주소 이름으로 IP 찾는 방식
            string host = Dns.GetHostName();    // 내 컴퓨터 주소의 이름을 알아내고 host에 저장
            IPHostEntry ipHost = Dns.GetHostEntry(host);    // 알아낸 주소의 여러 정보가 담김 Dns가 알아서 해줌
            IPAddress ipAddr = ipHost.AddressList[0];   // ip는 여러개를 리스트로 묶어서 보내는 경우(구글이라던가)가 많음 => 일단 우리는 1개니깐 첫번째로만 사용
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // 뽑아낸 ip를 가공한 최종 주소, port == 입장문 번호

            // 손님의 문의가 오면 입장시키기
            _listener.init(endPoint, () => { return new GameSession(); });  // GameSession을 Func 형태로 넣어줌
            Console.WriteLine("Listening...");

            while (true)
            {
                // 프로그램 중단되지 말라고 넣어둠
            }
        }
    }
}