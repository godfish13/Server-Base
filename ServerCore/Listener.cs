using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Listener  // 문지기   서버 입장에서 client의 연결신청을 대기하는 역할
    {
        Socket _listenSocket;
        Func<Session> _sessionFactory;

        public void init(IPEndPoint endPoint, Func<Session> sessionFactory, int register = 10, int backlog = 100)
        {
            // 문지기가 든 휴대폰 (listen Socket)
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory += sessionFactory;

            // 문지기 교육 (Bind)
            _listenSocket.Bind(endPoint);

            // 영업 시작
            _listenSocket.Listen(backlog);    // backlog == 최대 대기 수

            for (int i = 0; i < register; i++)    // 한번에 여러개 돌리고 싶을 경우(문지기 여러명이 필요할 경우) 여러개를 만들어 돌리면 됨
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs(); // 한번 new해두면 계속 재활용 가능
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted); // args.Completed이벤트에 OnAcceptCompleted 콜백 연동
                // == args.Completed가 발생할때마다 OnAcceptCompleted 콜백시킴  // SocketAsyncEventArgs 덕분에 멀티 스레드 환경에서 구동됨 조심해야함!
                // RegisterAccept 내의 pending == true면 여기서 OnAcceptCompleted, false면 RegisterAccept내부에서 OnAcceptCompleted실행          
                RegisterAccept(args);   // 최초 초기화에선 수동으로 등록 
            }                       
        }

        // 이하 Register와 OnAccept를 계속 반복하게 됨
        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;   // 루프 처음에 args 한번 청소

            bool pending = _listenSocket.AcceptAsync(args);    // 비동기 Accept, Accept 시도하다가 성공하면 args.Completed 이벤트 실행
            if (pending == false)   // 비동기 Accept요청을 했는데 하자마자 바로 성공했을 경우 바로 OnAcceptCompleted로 넘겨버리기
                OnAcceptCompleted(null, args);
        }

        void OnAcceptCompleted(Object sender, SocketAsyncEventArgs args)    // 멀티스레드 환경에서 실행되므로 조심해서 다뤄야 함!
        {
            if (args.SocketError == SocketError.Success)    // 에러없이 잘 됐을 경우
            {
                Session session = _sessionFactory.Invoke();
                session.Start(args.AcceptSocket);
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);
            }    // 지금 접속한 소켓은 AcceptSocket이다 라고 _onAcceptHandler에 전달, Program의 OnAcceptHandler에 clientSocket으로 전달되고 작업 진행
            else
                Console.WriteLine(args.SocketError.ToString());

            RegisterAccept(args);   // 최초 Register한 내용이 끝났으므로 다음 Register 실행
        }
    }
}
