using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketGenerator
{
    internal class PacketFormat     // @""의 형태로 @를 붙여서 쓰면 아래처럼 쓸 수 있음
    {
        // {0} 패킷 이름
        // {1} 멤버 변수들
        // {2} 멤버 변수 Read
        // {3} 멤버 변수 Write       // {}을 매개변수 말고 중괄호로 쓰고싶으면 {{ 이런식으로 2개 연달아 쓰면됨
        public static string packetFormat =
@"
class {0}
{{
    {1}

    public void ReadBuffer(ArraySegment<byte> segment)
    {{
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        {2}
    }} 
    
    public ArraySegment<byte> WriteBuffer()
    {{
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.{0});
        count += sizeof(ushort);

        {3}           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }}
}}
";

        // {0} 변수 형식
        // {1} 변수 이름
        public static string memberFormat =
@"public {0} {1};";

        // {0} 변수 이름
        // {1} To~~ 변수 형식
        // {2} 변수 형식
        public static string readFormat =
@"this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
count += sizeof({2});";

        // {0} 변수 이름
        public static string readStringFormat =
@"ushort {0}Length = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);
this.{0} = Encoding.Unicode.GetString(s.Slice(count, nameLength));
count += {0}Length;";

        // {0} 변수 이름
        // {1} 변수 형식
        public static string writeFormat =
@"success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.{0});
count += sizeof({1});";

        // {0} 변수 이름
        public static string writeStringFormat =
@"ushort {0}Length = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Length);
count += sizeof(ushort);
count += {0}Length;";
    }
}
