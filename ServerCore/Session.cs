using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public abstract class Session  // Engine파트, 실 기능은 Program에서 상속하여 구현
    {
        Socket _socket;
        int _disconnected = 0;

        object _lock = new object();
        Queue<byte[]> _sendQueue = new Queue<byte[]>();
        List<ArraySegment<byte>> _pendinglist = new List<ArraySegment<byte>>(); // RegisterSend() 내에서 _sendArgs.BufferList 제작용 list
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();    // 재활용 용이하게하기위해 멤버변수로 미리 선언

        // Program에서 Session을 상속하여 사용할 때 만들 인터페이스
        public abstract void OnConnected(EndPoint endPoint);      
        public abstract void OnReceive(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfbytes);
        public abstract void OnDisConnected(EndPoint endPoint);

        public void Start(Socket socket)
        {
            _socket = socket;
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);            
            _recvArgs.SetBuffer(new byte[1024], 0, 1024);    // (버퍼, 버퍼시작점, 버퍼의 사이즈)

            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterReceive();
        }

        public void Send(byte[] sendBuff)   // sendBuff를 queue에 모아서 보내는 방식
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);   
                if (_pendinglist.Count == 0)    // pendinglist의 내용물 값으로 pending여부 확이
                    RegisterSend();
            }          
        }   

        public void DisConnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)    // 이미 disconnect된 상태면 return
                return;

            OnDisConnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region 네트워크 통신 send, receive   

        void RegisterSend() // Send에 의해서만 호출되므로 이미 lock이 걸린 내부에서만 동작 => 따로 lock 안걸어줌
        {
            while (_sendQueue.Count > 0)
            {
                byte[] buff = _sendQueue.Dequeue();
                _pendinglist.Add(new ArraySegment<byte>(buff, 0, buff.Length));
            }
            _sendArgs.BufferList = _pendinglist;     // Args.BufferList에 바로 Add하면 안되고 이처럼 list를 따로 선언하고 list를 완성시킨 후 복사붙여넣기해줘야함
                                                     // 그냥 C#에서 이따구로 만들어둠 외워서 쓰면됨
            bool pending = _socket.SendAsync(_sendArgs);
            if (pending == false)
                OnSendCompleted(null, _sendArgs);
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)  // RegisterSend 외에 이벤트 콜백에 의해 멀티스레드 상태로 호출될 수 있으므로 lock 걸어줌
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendinglist.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)   // Send()에 주석으로 달아둔 오류 해결 위해 RegisterSend해줌
                            RegisterSend();                                    
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendComplete Failed : {e}");
                    }
                }
                else
                {
                    DisConnect();
                }
            }
        }

        void RegisterReceive()
        {          
            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (pending == false)
                OnReceiveCompleted(null, _recvArgs);           
        }

        void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if(args.BytesTransferred > 0 && args.SocketError == SocketError.Success)    // 상대방이 연결을 끊는 등 특정상황에서 0바이트가 올 수도 있음 체크해줘야함
            {
                try
                {
                    OnReceive(new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred));
                    RegisterReceive();
                }
                catch (Exception e) 
                {
                    Console.WriteLine($"OnReceiveComplete Failed : {e}");
                }               
            }
            else
            {
                DisConnect();
            }
        }

        #endregion
    }
}
