using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server_Base
{
    class Packet                // 대부분의 게임은 설계할 때, buffer전체 크기 size를 첫번째 인자, packetID를 두번째 인자로 넘겨주는 경우가 많다
    {
        public ushort size;     // 메모리 절약을 위해 2byte == ushort정도만 사용함
        public ushort packetID; // packet설계는 최대한 메모리 아껴주는게 좋음
    }

    class GameSession : Session // 실제로 각 상황에서 사용될 기능 구현
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            Packet packet = new Packet() { size = 4, packetID = 7 };

            // 보낸다
            for (int i = 0; i < 5; i++)
            {
                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096); // static threadLocal CurrentBuffer 생성 및 4096크기 할당 요청
                byte[] buffer1 = BitConverter.GetBytes(packet.size);   // BitConverter : 대충 값 알아서 buffer에 넣을 byte로 변환해줌
                byte[] buffer2 = BitConverter.GetBytes(packet.packetID);
                Array.Copy(buffer1, 0, openSegment.Array, openSegment.Offset, buffer1.Length);  // CurrentBuffer에 값들 직렬화해서 입력
                Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer1.Length, buffer2.Length);
                ArraySegment<byte> sendBuff = SendBufferHelper.Close(packet.size);  // 완성된 CurrentBuffer를 sendBuff으로 보냄

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

    class Program
    {
        static void Main(string[] args)
        {
            // DNS (Domain Name System) : 주소 이름으로 IP 찾는 방식
            string host = Dns.GetHostName();    // 내 컴퓨터 주소의 이름을 알아내고 host에 저장
            IPHostEntry ipHost = Dns.GetHostEntry(host);    // 알아낸 주소의 여러 정보가 담김 Dns가 알아서 해줌
            IPAddress ipAddr = ipHost.AddressList[0];   // ip는 여러개를 리스트로 묶어서 보내는 경우(구글이라던가)가 많음 => 일단 우리는 1개니깐 첫번째로만 사용
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // 뽑아낸 ip를 가공한 최종 주소, port == 입장문 번호

            Connector connector = new Connector();

            connector.Connect(endPoint, () => { return new GameSession(); });

            while(true)
            {
                // 자기 휴대폰 설정
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(1000);
            }                   
        }
    }
}