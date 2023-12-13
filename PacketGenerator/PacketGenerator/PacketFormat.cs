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
@"class {0}
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
}}";

        // {0} 변수 형식
        // {1} 변수 이름
        public static string memberFormat =
@"public {0} {1};";


        // {0} 리스트 이름 [대문자시작] : struct 이름
        // {1} 리스트 이름 [소문자시작] : 멤버 변수로 사용할 리스트 이름
        // {2} 멤버 변수들
        // {3} 멤버 변수 Read
        // {4} 멤버 변수 Write 
        public static string memberListFormat =
@"public struct {0}
{{
    {2}

    public void Read(ReadOnlySpan<byte> s, ref ushort count)
    {{
        {3}
    }}

    public bool Write(Span<byte> s, ref ushort count)
    {{
        bool success = true;
        {4}

        return success;
    }}
}}
public List<{0}> {1}s = new List<{0}>();";


        // {0} 변수 이름
        // {1} To~~ 변수 형식
        // {2} 변수 형식
        public static string readFormat =
@"// 단순 변수 읽기
this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
count += sizeof({2});";

        // {0} 변수 이름
        public static string readStringFormat =
@"
// string 읽기
ushort {0}Length = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);
this.{0} = Encoding.Unicode.GetString(s.Slice(count, nameLength));
count += {0}Length;";

        // {0} 리스트 이름 [대문자시작] : struct 이름
        // {1} 리스트 이름 [소문자시작] : 멤버 변수로 사용할 리스트 이름
        public static string readListFormat =
@"
// list 처리 읽기
{1}s.Clear(); // 시작전 한번 청소
ushort {1}Length = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);
for(int i = 0; i < {1}Length; i++)
{{
    {0} {1} = new {0}();
    {1}.Read(s, ref count);
    {1}s.Add({1});
}}";


        // {0} 변수 이름
        // {1} 변수 형식
        public static string writeFormat =
@"// 단순 변수 보내기
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.{0});
count += sizeof({1});";

        // {0} 변수 이름
        public static string writeStringFormat =
@"
// string 보내기
ushort {0}Length = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Length);
count += sizeof(ushort);
count += {0}Length;";

        // {0} 리스트 이름 [대문자시작] : struct 이름
        // {1} 리스트 이름 [소문자시작] : 멤버 변수로 사용할 리스트 이름
        public static string writeListFormat =
@"
// list 보내기
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.{1}s.Count);  // 스킬 갯수 직렬화, ushort로 메모리 아끼는거 기억!
count += sizeof(ushort);
foreach ({0} {1} in this.{1}s) // foreach를 통해 각 skill 마다 Write로 s에 직렬화해줌
    success &= {1}.Write(s, ref count);";
    }
}
