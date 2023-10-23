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
    class Listener  // 문지기
    {
        Socket _listenSocket;
        Action<Socket> _onAcceptHandler;

        public void init(IPEndPoint endPoint, Action<Socket> OnAcceptHandler)
        {
            // 문지기가 든 휴대폰 (listen Socket)
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _onAcceptHandler += OnAcceptHandler;

            // 문지기 교육 (Bind)
            _listenSocket.Bind(endPoint);

            // 영업 시작
            _listenSocket.Listen(10);    // backlog == 최대 대기 수

            SocketAsyncEventArgs args = new SocketAsyncEventArgs(); // 한번 new해두면 계속 재활용 가능
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted); // args.Completed이벤트에 OnAcceptCompleted 콜백 연동
                                                                                        // == args.Completed가 발생할때마다 OnAcceptCompleted 콜백시킴
                                     // RegisterAccept 내의 pending == true면 여기서 OnAcceptCompleted, false면 RegisterAccept내부에서 OnAcceptCompleted실행
            
            RegisterAccept(args);   // 최초 초기화에선 수동으로 등록               
        }

        // 이하 Register와 OnAccept를 계속 반복하게 됨
        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;   // 루프 처음에 args 한번 청소

            bool pending = _listenSocket.AcceptAsync(args);    // 비동기 Accept, Accept 시도하다가 성공하면 args.Completed 이벤트 실행
            if (pending == false)   // 비동기 Accept요청을 했는데 하자마자 바로 성공했을 경우 바로 OnAcceptCompleted로 넘겨버리기
                OnAcceptCompleted(null, args);
        }

        void OnAcceptCompleted(Object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)    // 에러없이 잘 됐을 경우
            {
                _onAcceptHandler.Invoke(args.AcceptSocket); // Socket이 Accept했다는 이벤트 받았다
            }
            else
                Console.WriteLine(args.SocketError.ToString());

            RegisterAccept(args);   // 최초 Register한 내용이 끝났으므로 다음 Register 실행
        }
    }
}
