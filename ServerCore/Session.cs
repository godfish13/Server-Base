using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    internal class Session
    {
        Socket _socket;
        int _disconnected = 0;
        
        public void Start(Socket socket)
        {
            _socket = socket;
            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);            
            recvArgs.SetBuffer(new byte[1024], 0, 1024);    // (버퍼, 버퍼시작점, 버퍼의 사이즈)

            RegisterReceive(recvArgs);
        }

        public void Send(byte[] sendBuff)
        {
            _socket.Send(sendBuff);
        }

        public void DisConnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)    // 이미 disconnect된 상태면 return
                return;

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region 네트워크 통신 receive
        void RegisterReceive(SocketAsyncEventArgs args)
        {
            bool pending = _socket.ReceiveAsync(args);
            if (pending == false)
                OnReceiveCompleted(null, args);
        }

        void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if(args.BytesTransferred > 0 && args.SocketError == SocketError.Success)    // 상대방이 연결을 끊는 등 특정상황에서 0바이트가 올 수도 있음 체크해줘야함
            {
                try
                {
                    string receiveData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred); // init에서 SetBuffer로 설정한 값 추출하는 args 변수들
                    Console.WriteLine($"[From Client] : {receiveData}");
                    RegisterReceive(args);
                }
                catch (Exception e) 
                {
                    Console.WriteLine($"OnReceiveComplete Failed : {e}");
                }
                
            }
            else
            {

            }
        }
        #endregion
    }
}
