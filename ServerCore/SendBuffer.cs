using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ServerCore
{
    public class SendBufferHelper   // SendBuffer 클래스 사용하기 쉽게 매핑해두는 클래스
    {
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });
        // 전역변수로 올려두지만 멀티스레드 환경이므로 ThreadLocal로 선언

        public static int ChunkSize { get; set; } = 65535 * 100;

        public static ArraySegment<byte> Open(int reserveSize)  // 원하는 사이즈만큼 할당 요청
        {
            if (CurrentBuffer.Value == null)    // 따로 사용중인 CurrentBuffer 없으면 하나 만들어줌
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            if (CurrentBuffer.Value.FreeSize < reserveSize)     // 원하는 사이즈만큼 여유공간 없으면 CurrentBuffer를 새걸로 초기화
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            return CurrentBuffer.Value.Open(reserveSize);   // 원하는 사이즈만큼 여유공간을 가진 buffer를 리턴해줌
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return CurrentBuffer.Value.Close(usedSize);     // 완성한 Buffer 리턴해줌
        }
    }

    public class SendBuffer
    {
        // [] [] [] [] [] [] [] [] [] [] []
        byte[] _buffer;
        int _usedSizePosition = 0;  // buffer를 어디까지 사용헀는지 표시하는 index

        public int FreeSize { get { return _buffer.Length - _usedSizePosition; } }

        public SendBuffer(int chunkSize)
        {
            _buffer = new byte[chunkSize];
        }

        public ArraySegment<byte> Open(int reserveSize)     // reserveSize : 얼마만큼의 버퍼공간을 할당받을지 선언 (최대크기이므로 실제사용량과 다름)
        {
            if (reserveSize > FreeSize)     // 사용하고싶은 크기가 여유공간보다 크면 오류처리
                return null;

            return new ArraySegment<byte>(_buffer, _usedSizePosition, reserveSize); // 사용할 버퍼의 공간 리턴해줌
        }

        public ArraySegment<byte> Close(int usedSize)   // usedSize : 실제로 사용한 만큼의 사이즈
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSizePosition, usedSize);
            _usedSizePosition += usedSize;
            return segment; // 실제로 사용한 범위 리턴
        }
    }
}
