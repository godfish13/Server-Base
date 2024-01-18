using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class ReceiveBuffer
    {
        // ex. [r] [ ] [ ] [w] [ ] [ ] [ ] [ ] 
        ArraySegment<byte> _buffer;
        int _readPosition;      // 버퍼 내에서 어느 위치까지 처리했는지 기록해두는 index
        int _writePosition;     // 입력받은 패킷 사이즈만큼 버퍼에 넣고 남은 다음 위치에서부터 버퍼를 다시 넣기 위해 다음으로 입력할 위치 index

        public ReceiveBuffer(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public int DataSize { get { return _writePosition - _readPosition; } }  // 처리해야할 남은 Data의 크기 //ex. 3(w_index) - 0(r_index) = 3
        public int WritableSize { get { return _buffer.Count - _writePosition; } }  // 버퍼에 사용가능하게 남은 공간 //ex. 8(크기) - 3(index) = 5

        public ArraySegment<byte> ReadSegment   // 지금 받은 데이터 중 유효한 Data 부분
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPosition, DataSize); } // ArraySegment<T>(버퍼, 버퍼시작점, 버퍼의 사이즈)
        }

        public ArraySegment<byte> WritableSegment   // 아직 남아있는 여유 부분
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePosition, WritableSize); }
        }

        public void Clean()     // r/w position 가끔 앞으로 땡겨주는 메소드
        {
            int dataSize = DataSize;
            if(dataSize == 0)   // r/w가 동일한, 처리안된 데이터가 없는 경우
            {
                _readPosition = _writePosition = 0;
            }
            else    // 처리안된 데이터가 남아있는 경우, 남은 데이터를 0으로 복사시켜주고 r/w 각 위치로 이동 
            {
                Array.Copy(_buffer.Array, _buffer.Offset + _readPosition, _buffer.Array, _buffer.Offset, dataSize);
                _readPosition = 0;
                _writePosition = dataSize;
            }
        }

        public bool OnRead(int numOfBytes)  // 성공적으로 처리했을 시 r_position 이동
        {
            if (numOfBytes > DataSize)  // numOfbytes만큼 처리했다고 하는데 남은 처리해야할 데이터보다 처리했다고 주장하는 크기가 크면 오류 때림
                return false;

            _readPosition += numOfBytes;
            return true;
        }
        
        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > WritableSize)  // numOfbytes만큼 쓰겠다는데 남은 데이터 범위보다 큰 크기를 넣으려하면 오류때림
                return false;

            _writePosition += numOfBytes;
            return true;
        }
    }
}
