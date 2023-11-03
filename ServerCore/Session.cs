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

        object _lock = new object();
        Queue<byte[]> _sendQueue = new Queue<byte[]>();
        //bool _pending = false;  // RegisterSending 한번 하면 pending = true로 Queue에 모으기 실행, completed되면 false로 바꿔주도록
                                  // _pending 대신 _pendingList count값으로 판별하도록 변경함
        List<ArraySegment<byte>> _pendinglist = new List<ArraySegment<byte>>(); // RegisterSend() 내에서 _sendArgs.BufferList 제작용 list
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();    // 재활용 용이하게하기위해 멤버변수로 미리 선언


        public void Start(Socket socket)
        {
            _socket = socket;
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);            
            _recvArgs.SetBuffer(new byte[1024], 0, 1024);    // (버퍼, 버퍼시작점, 버퍼의 사이즈)

            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterReceive();
        }

        /*public void Send(byte[] sendBuff)   // 매번 sendBuff를 등록하는 방식
        {
            //_socket.Send(sendBuff);   비동기없이 그냥 실행하던 구버전       
            _sendArgs.SetBuffer(sendBuff, 0, sendBuff.Length);
            RegisterSend();
        }*/

        public void Send(byte[] sendBuff)   // sendBuff를 queue에 모아서 보내는 방식
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);   
                //if (_pending == false)  // RegisterSend내에서 SendAsync가 지연중인데 또다른 Send신호가 오면 _pending == true인 상태라 후자는 RegisterSend를 못함
                //    RegisterSend();     // 이를 방지하기 위해 OnSendCompleted의 try문 내에서 _sendQueue.Count > 0인 경우에 RegisterSend 한번 더 해줌
                if (_pendinglist.Count == 0)    // 바로 위 boolean으로 pending 판별 대신 pendinglist의 내용물 값으로 판별
                    RegisterSend();
            }          
        }   

        public void DisConnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)    // 이미 disconnect된 상태면 return
                return;

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region 네트워크 통신 send, receive   

        void RegisterSend() // Send에 의해서만 호출되므로 이미 lock이 걸린 내부에서만 동작 => 따로 lock 안걸어줌
        {
            //_pending = true;
            /*byte[] buff = _sendQueue.Dequeue();   // _sendQueue 내의 값을 하나씩 버퍼로 세팅해주는 방식, 아래의 list방식으로 변경함
            _sendArgs.SetBuffer(buff, 0, buff.Length);*/

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

                        if (_sendQueue.Count > 0)   // Send()에 주석으로 달아둔 오류 해결 위해 RegisterSend해줌
                            RegisterSend();

                        Console.WriteLine($"Transferred args byte : {_sendArgs.BytesTransferred}");

                        //else
                        //    _pending = false;     // sendQueue가 비어있는 경우 _pending = false                  
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
                    string receiveData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred); // init에서 SetBuffer로 설정한 값 추출하는 args 변수들
                    Console.WriteLine($"[From Client] : {receiveData}");
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
