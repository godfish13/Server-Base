using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Server_Base
{
    class GameSession : Session // 실제로 각 상황에서 사용될 기능 구현
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMO RPG Server!!");
            Send(sendBuff);
            Thread.Sleep(100);

            DisConnect();
        }

        public override void OnDisConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnected : {endPoint}");
        }

        public override void OnReceive(ArraySegment<byte> buffer)
        {
            string receiveData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count); // init에서 SetBuffer로 설정한 값 추출하는 args 변수들
            Console.WriteLine($"[From Client] : {receiveData}");
        }

        public override void OnSend(int numOfbytes)
        {
            Console.WriteLine($"Transferred args byte : {numOfbytes}");
        }
    }

    internal class Program
    {
        static Listener _listener = new Listener();

        // OnAcceptHandler는 Session으로 각 기능을 인터페이스 상속하여 구현해줬으므로 필요x
        /*static void OnAcceptHandler(Socket clientSocket)
        {
            try
            {
                // 문지기고용
                /*socket.Connect(endPoint);
                Console.WriteLine($"Connected to {socket.RemoteEndPoint.ToString()}");  // 연결된 상대방쪽 주소
                //Listener class로 분리

                // 받기 보내기 대화종료
                // 받는다
                byte[] receiveBuffer = new byte[1024];
                int receiveByte = clientSocket.Receive(receiveBuffer);  // clientSocket에서 받은 데이터 receiveBuffer에 저장
                string receiveData = Encoding.UTF8.GetString(receiveBuffer, 0, receiveByte); // (문자열, 문자열이 시작되는 index, 문자의 갯수)
                Console.WriteLine($"[From Client] : {receiveData}");            // 서버상 보내려면 인코딩 맞춰야함 여기선 UTF8 사용

                // 보낸다
                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMO RPG Server!!");
                clientSocket.Send(sendBuff);

                // 손님 내보낸다
                clientSocket.Shutdown(SocketShutdown.Both); // 양쪽 모두 서로 대화 나눌것 없다고 공지
                clientSocket.Close();     // 대화끝
                // Session class로 분리

                GameSession session = new GameSession();
                session.Start(clientSocket);

                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMO RPG Server!!");
                session.Send(sendBuff);

                Thread.Sleep(1000);

                session.DisConnect();

                session.DisConnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }*/

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