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
    class GameSession : Session // 실제로 각 상황에서 사용될 기능 구현
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            // 보낸다
            for (int i = 0; i < 5; i++)
            {
                byte[] SendBuff = Encoding.UTF8.GetBytes($"Hello World! : {i}");
                Send(SendBuff);
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