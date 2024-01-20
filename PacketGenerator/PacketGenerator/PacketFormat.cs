using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketGenerator       // common/batch.exe 실행을 통해 GenPackets를 생성하고 DummyClient, Server_Base에 각각 복사붙여넣기 해줌
{
    internal class PacketFormat     // @""의 형태로 @를 붙여서 쓰면 아래처럼 쓸 수 있음
    {
        // {0} : 패킷 등록(register)
        public static string managerFormat =
@"using ServerCore;
using System;
using System.Collections.Generic;

public class PacketManager
{{
    #region Singletone
    static PacketManager _instance = new PacketManager();
    public static PacketManager Instance {{ get {{ return _instance; }} }}
    #endregion

    PacketManager() 
    {{
        Register();
    }}
    Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
    Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();

    public void Register()
    {{
{0}
    }}

    public void OnReceivePacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> OnRecvCallback = null)
    {{
        ushort count = 0;
        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;
        ushort ID = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
        if (_makeFunc.TryGetValue(ID, out func))
        {{
            IPacket packet = func.Invoke(session, buffer);

            if (OnRecvCallback != null)
                OnRecvCallback.Invoke(session, packet);
            else
                HandlePacket(session, packet);
        }}
    }}

    T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
    {{
        T packet = new T();
        packet.ReadBuffer(buffer);
        return packet;
    }}

    public void HandlePacket(PacketSession session, IPacket packet)
    {{
        Action<PacketSession, IPacket> action = null;
        if (_handler.TryGetValue(packet.Protocol, out action))
            action.Invoke(session, packet);
    }}
}}";

        // {0} : 패킷 이름
        public static string managerRegisterFormat =
@"        _makeFunc.Add((ushort)PacketIDEnum.{0}, MakePacket<{0}>);
        _handler.Add((ushort)PacketIDEnum.{0}, PacketHandler.{0}Handler);";


        // {0} : 패킷 이름/번호 목록
        // {1} : 패킷 목록
        public static string fileFormat =
@"using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public enum PacketIDEnum
{{
    {0}
}}

public interface IPacket
{{
    ushort Protocol {{ get; }}
	void ReadBuffer(ArraySegment<byte> segement);
	ArraySegment<byte> WriteBuffer();
}}

{1}";
        // {0} : 패킷 이름
        // {1} : 패킷 번호
        public static string packetEnumFormat =
@"{0} = {1},";



        // {0} 패킷 이름
        // {1} 멤버 변수들
        // {2} 멤버 변수 Read
        // {3} 멤버 변수 Write       // {}을 매개변수 말고 중괄호로 쓰고싶으면 {{ 이런식으로 2개 연달아 쓰면됨
        public static string packetFormat =
@"

public class {0} : IPacket
{{
    {1}

    public ushort Protocol => (ushort)PacketIDEnum.{0};    

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
@"
public List<{0}> {1}s = new List<{0}>();
public class {0}
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
";


        // {0} 변수 이름
        // {1} To~~ 변수 형식
        // {2} 변수 형식
        public static string readFormat =
@"
// {2} {0} 읽기
this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
count += sizeof({2});";

        // {0} 변수 이름
        // {1} 변수 형식
        public static string readByteFormat =
@"
// {1} {0} 읽기
this.{0} = ({1})segment.Array[segment.Offset + count];
count += sizeof({1});";

        // {0} 변수 이름
        public static string readStringFormat =
@"
// string {0} 읽기
ushort {0}Length = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);
this.{0} = Encoding.Unicode.GetString(s.Slice(count, {0}Length));
count += {0}Length;";

        // {0} 리스트 이름 [대문자시작] : struct 이름
        // {1} 리스트 이름 [소문자시작] : 멤버 변수로 사용할 리스트 이름
        public static string readListFormat =
@"
// {0} list {1}s 읽기 처리
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
@"
// {1} {0} 보내기
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.{0});
count += sizeof({1});";

        // {0} 변수 이름
        // {1} 변수 형식
        public static string writeByteFormat =
@"
// {1} {0} 읽기
openSegment.Array[openSegment.Offset + count] = ({1})this.{0};
count += sizeof({1});";

        // {0} 변수 이름
        public static string writeStringFormat =
@"
// string {0} 보내기
ushort {0}Length = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Length);
count += sizeof(ushort);
count += {0}Length;";

        // {0} 리스트 이름 [대문자시작] : struct 이름
        // {1} 리스트 이름 [소문자시작] : 멤버 변수로 사용할 리스트 이름
        public static string writeListFormat =
@"
// {0} list {1}s 보내기 처리
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.{1}s.Count);  // 스킬 갯수 직렬화, ushort로 메모리 아끼는거 기억!
count += sizeof(ushort);
foreach ({0} {1} in this.{1}s) // foreach를 통해 각 skill 마다 Write로 s에 직렬화해줌
    success &= {1}.Write(s, ref count);";
    }
}
