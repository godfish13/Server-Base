using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        // {size[2]}{packetID[2]}{ ...... } {size[2]}{packetID[2]}{ ...... } {size[2]} ... 의 형식으로 packet들이 오게 됨
        public sealed override int OnReceive(ArraySegment<byte> buffer)     // sealed 키워드 : PacketSession을 상속받은 클래스들은 OnReceive를 override 불가능
        {
            int processLength = 0;
            int PacketCount = 0;

            while (true)
            {
                if (buffer.Count < HeaderSize)  // 최소한 헤더(size파트)는 파싱할 수 있는지 확인 // size 메모리 크기가 2 이므로 최소한 2보단 커야함
                    break;

                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);   // UInt16 == Ushort의 크기 즉 buffer에서 size의 크기만 추출
                if (buffer.Count < dataSize)    // 패킷이 dataSize보다 작으면 패킷이 다 안왔다는 뜻이므로 break
                    break;

                // 여기까지 통과했으면 패킷 조립 가능
                OnReceivePacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize)); // 팁으로 ArraySegment는 struct이므로 new키워드를 써도 힙메모리 할당이 아닌 스택에서 처리됨
                PacketCount++;

                // 제일 앞에 {size[2]} {packetID[2]} { ...... }파트를 처리했으므로 다음 {size[2]} {packetID[2]} { ...... } 파트로 이동
                processLength += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);              
            }

            //if (PacketCount > 1)
                //Console.WriteLine($"모아서 보낸 패킷 갯수 : {PacketCount}");

            return processLength;
        }

        public abstract void OnReceivePacket(ArraySegment<byte> buffer);

    }

    public abstract class Session  // Engine파트, 실 기능은 Program에서 상속하여 구현
    {
        Socket _socket;
        int _disconnected = 0;

        ReceiveBuffer _recvBuffer = new ReceiveBuffer(65535);

        object _lock = new object();
        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> _pendinglist = new List<ArraySegment<byte>>(); // RegisterSend() 내에서 _sendArgs.BufferList 제작용 list
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();    // 재활용 용이하게하기위해 멤버변수로 미리 선언

        // Program에서 Session을 상속하여 사용할 때 만들 인터페이스
        public abstract void OnConnected(EndPoint endPoint);      
        public abstract int OnReceive(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfbytes);
        public abstract void OnDisConnected(EndPoint endPoint);

        void Clear()
        {
            lock (_lock) 
            {
                _sendQueue.Clear();
                _pendinglist.Clear();
            }
        }

        public void Start(Socket socket)
        {
            _socket = socket;

            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            
            RegisterReceive();
        }

        public void Send(List<ArraySegment<byte>> sendBuffList)   // sendBuff를 queue에 모아서 보내는 방식
        {
            if (sendBuffList.Count == 0)    // pendingList가 빈 경우 OnSendCompleted에서 오류방지를 위해 return
                return;

            lock (_lock)
            {
                foreach(ArraySegment<byte> sendbuffer in sendBuffList)
                    _sendQueue.Enqueue(sendbuffer);

                if (_pendinglist.Count == 0)    // pendinglist의 내용물 값으로 pending여부 확인
                    RegisterSend();
            }
        }

        public void Send(ArraySegment<byte> sendBuff)   // sendBuff를 queue에 모아서 보내는 방식
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);   
                if (_pendinglist.Count == 0)    // pendinglist의 내용물 값으로 pending여부 확인
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
            Clear();
        }

        #region 네트워크 통신 send, receive   

        void RegisterSend() // Send에 의해서만 호출되므로 이미 lock이 걸린 내부에서만 동작 => 따로 lock 안걸어줌
        {
            if(_disconnected == 1)
                return;

            while (_sendQueue.Count > 0)
            {
                ArraySegment<byte> buff = _sendQueue.Dequeue();
                _pendinglist.Add(buff); // ArraySegment<T>(버퍼, 버퍼시작점, 버퍼의 사이즈)
            }
            _sendArgs.BufferList = _pendinglist;     // Args.BufferList에 바로 Add하면 안되고 이처럼 list를 따로 선언하고 list를 완성시킨 후 복사붙여넣기해줘야함
                                                     // 그냥 C#에서 이따구로 만들어둠 외워서 쓰면됨

            try
            {
                bool pending = _socket.SendAsync(_sendArgs);
                if (pending == false)
                    OnSendCompleted(null, _sendArgs);
            }
            catch (Exception e) 
            {
                Console.WriteLine($"RegisterSend failed : {e}");
            }
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
                        Console.WriteLine($"OnSendComplete failed : {e}");
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
            if (_disconnected == 1)
                return;

            _recvBuffer.Clean();
            ArraySegment<byte> segment = _recvBuffer.WritableSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            try
            {
                bool pending = _socket.ReceiveAsync(_recvArgs);
                if (pending == false)
                    OnReceiveCompleted(null, _recvArgs);
            }
            catch (Exception e) 
            {
                Console.WriteLine($"RegisterReceive Failed : {e}");
            }         
        }

        void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if(args.BytesTransferred > 0 && args.SocketError == SocketError.Success)    // 상대방이 연결을 끊는 등 특정상황에서 0바이트가 올 수도 있음 체크해줘야함
            {
                try
                {
                    // _recvBuffer의 w_position 이동   // OnWrite내에서 실행되고 true/false return해줌
                    if(_recvBuffer.OnWrite(args.BytesTransferred) == false)  // 요상한게 들어가면 바로 끊어버리고 return
                    {
                        DisConnect();
                        return;
                    }

                    // 컨텐츠 쪽으로 데이터 넘겨주고 얼마나 처리했는지 받아옴
                    int processLength =  OnReceive(_recvBuffer.ReadSegment);     
                    if (processLength < 0 || _recvBuffer.DataSize < processLength)  // 처리하는 크기가 이상하면 끊고 return
                    {
                        DisConnect();
                        return;
                    }

                    // _recvBuffer의 r_position 이동
                    if(_recvBuffer.OnRead(processLength) == false) 
                    {
                        DisConnect();
                        return;
                    }

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
