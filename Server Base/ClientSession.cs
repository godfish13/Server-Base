using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server_Base       
{
    /*public abstract class Packet    // 대부분의 게임은 설계할 때, buffer전체 크기 size를 첫번째 인자, packetID를 두번째 인자로 넘겨주는 경우가 많다
    {
        public ushort size;     // 메모리 절약을 위해 2byte == ushort정도만 사용함
        public ushort packetID; // packet설계는 최대한 메모리 아껴주는게 좋음

        public abstract ArraySegment<byte> WriteBuffer();
        public abstract void ReadBuffer(ArraySegment<byte> segment);
    }

    class PlayerInfoRequirement : Packet
    {
        public long playerID;
        public string name;

        public struct Skillinfo
        {
            public int id;
            public short level;
            public float duration;

            public bool Write(Span<byte> s, ref ushort count)
            {
                bool success = true;
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), id);
                count += sizeof(int);
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), level);
                count += sizeof(short);
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), duration);
                count += sizeof(float);

                return success;
            }

            public void Read(ReadOnlySpan<byte> s, ref ushort count)
            {
                id = BitConverter.ToInt32(s.Slice(count, s.Length - count));    // ToInt16, ToInt32, ToSingle 등 각 변수 자료형에 따라 주의!!
                count += sizeof(int);
                level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
                count += sizeof(short);
                duration = BitConverter.ToSingle(s.Slice(count, s.Length - count)); // float은 ToSingle 사용
                count += sizeof(float);
            }
        }

        public List<Skillinfo> skills = new List<Skillinfo>();

        public PlayerInfoRequirement()
        {
            this.packetID = (ushort)PacketIDEnum.PlayerInfoRequirement;
        }

        public override ArraySegment<byte> WriteBuffer()
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096); // static threadLocal CurrentBuffer 생성 및 4096크기 할당 요청
            bool success = true;
            ushort count = 0;

            Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);
            // Span : ArraySegment와 마찬가지로 Array의 범위를 지정하는 역할

            // GetBytes 후 Copy 대신 openSegment에 바로 packet 입력 // 단, 유니티 옛버전에서는 TryWriteBytes가 사용불가능함! 자기가 쓰는 버전 확인해보기
            //success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count), packet.size); // 아래로 이동됨!
            count += sizeof(ushort); // packet.size는 ushort 타입, 그러므로 첫번째로 버퍼 가장 앞부분에 size의 공간을 미리 확보해둠
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.packetID);
            count += sizeof(ushort);
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerID);
            count += sizeof(long);                                                  // 시작점 재설정 + count     // 길이 재설정 - count

            //string 보내기 전략 : 앞부분 ushort 2byte에 string의 길이를 담고 이후byte에 string 넣어서 보내기                    
            ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
            // Getbytes : 목표 bytes에 string 넣고 해당 길이 return  // string을 직렬화하기 전에 nameLength 정보를 담아둘 ushort 공간 미리 확보하고 그 뒷부분에 string 직렬화
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength); // nameLength 직렬화
            count += sizeof(ushort);
            count += nameLength;

            // skill list 보내기
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)skills.Count);  // 스킬 갯수 직렬화, ushort로 메모리 아끼는거 기억!
            count += sizeof(ushort);
            foreach (Skillinfo skill in skills) // foreach를 통해 각 skill 마다 Write로 s에 직렬화해줌
                success &= skill.Write(s, ref count);


            success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
            // 가장 밑줄에서 size == count를 buffer에 넣어줘야함!! 왜냐하면 최종적인 packet의 크기는 이것저것 넣고 난 뒤의 최종 count수치이기 때문!!

            if (success == false)   // 변환 실패시 null 반환
                return null;

            return SendBufferHelper.Close(count);  // 완성된 CurrentBuffer를 sendBuff으로 보냄         
        }

        public override void ReadBuffer(ArraySegment<byte> segment)
        {
            ushort count = 0;

            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

            count += sizeof(ushort);
            count += sizeof(ushort);

            //this.playerID = BitConverter.ToInt64(segment.Array, segment.Offset + count); 이 방식은 만일 client가 size값을 이상하게 보낼시 오류 발생
            this.playerID = BitConverter.ToInt64(s.Slice(count, s.Length - count));
            // ex. writebuffer에서 size != 12로 거짓입력했을 시 segment의 완성본은 close에 의해 segment.Count != 12 가 됨
            // segment.Count - 4 != 8이므로 원래 long의 길이 8만큼 읽어야하는데 그게 성립되지 않으므로 오류 발생(Count < 12면 읽다가 OutofRange 에러, Count > 12면 이상함아무튼)
            // 이때 slice이용 시 catch 발생, 오류 체크 가능해짐
            count += sizeof(long);

            // string 읽기
            ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);
            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength)); // byte를 string으로 바꿔서 읽어줌
            count += nameLength;

            // skill list 읽기
            skills.Clear(); // 시작전 한번 청소
            ushort skillLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);
            for (int i = 0; i < skillLength; i++)
            {
                Skillinfo skill = new Skillinfo();
                skill.Read(s, ref count);
                skills.Add(skill);
            }
        }
    }

    public enum PacketIDEnum
    {
        PlayerInfoRequirement = 1,
        PlayerInfoOk = 2,
    }*/

    public enum PacketIDEnum
    {
        PlayerInfoRequirement = 1,
        Test = 2,

    }


    class PlayerInfoRequirement
    {
        public byte testByte;
        public long playerID;
        public string name;
        public List<Skill> skills = new List<Skill>();

        public class Skill
        {
            public int id;
            public short level;
            public float duration;

            public class Attribute
            {
                public int att;

                public void Read(ReadOnlySpan<byte> s, ref ushort count)
                {

                    // int att 읽기
                    this.att = BitConverter.ToInt32(s.Slice(count, s.Length - count));
                    count += sizeof(int);
                }

                public bool Write(Span<byte> s, ref ushort count)
                {
                    bool success = true;

                    // int att 보내기
                    success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.att);
                    count += sizeof(int);

                    return success;
                }
            }

            public List<Attribute> attributes = new List<Attribute>();

            public void Read(ReadOnlySpan<byte> s, ref ushort count)
            {

                // int id 읽기
                this.id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
                count += sizeof(int);

                // short level 읽기
                this.level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
                count += sizeof(short);

                // float duration 읽기
                this.duration = BitConverter.ToSingle(s.Slice(count, s.Length - count));
                count += sizeof(float);

                // Attribute list attributes 읽기 처리
                attributes.Clear(); // 시작전 한번 청소
                ushort attributeLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
                count += sizeof(ushort);
                for (int i = 0; i < attributeLength; i++)
                {
                    Attribute attribute = new Attribute();
                    attribute.Read(s, ref count);
                    attributes.Add(attribute);
                }
            }

            public bool Write(Span<byte> s, ref ushort count)
            {
                bool success = true;

                // int id 보내기
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.id);
                count += sizeof(int);

                // short level 보내기
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.level);
                count += sizeof(short);

                // float duration 보내기
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.duration);
                count += sizeof(float);

                // Attribute list attributes 보내기 처리
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.attributes.Count);  // 스킬 갯수 직렬화, ushort로 메모리 아끼는거 기억!
                count += sizeof(ushort);
                foreach (Attribute attribute in this.attributes) // foreach를 통해 각 skill 마다 Write로 s에 직렬화해줌
                    success &= attribute.Write(s, ref count);

                return success;
            }
        }


        public void ReadBuffer(ArraySegment<byte> segment)
        {
            ushort count = 0;

            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
            count += sizeof(ushort);
            count += sizeof(ushort);


            // byte testByte 읽기
            this.testByte = (byte)segment.Array[segment.Offset + count];
            count += sizeof(byte);

            // long playerID 읽기
            this.playerID = BitConverter.ToInt64(s.Slice(count, s.Length - count));
            count += sizeof(long);

            // string name 읽기
            ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);
            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
            count += nameLength;

            // Skill list skills 읽기 처리
            skills.Clear(); // 시작전 한번 청소
            ushort skillLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);
            for (int i = 0; i < skillLength; i++)
            {
                Skill skill = new Skill();
                skill.Read(s, ref count);
                skills.Add(skill);
            }
        }

        public ArraySegment<byte> WriteBuffer()
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            ushort count = 0;
            bool success = true;

            Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

            count += sizeof(ushort);
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.PlayerInfoRequirement);
            count += sizeof(ushort);


            // byte testByte 읽기
            openSegment.Array[openSegment.Offset + count] = (byte)this.testByte;
            count += sizeof(byte);

            // long playerID 보내기
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerID);
            count += sizeof(long);

            // string name 보내기
            ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
            count += sizeof(ushort);
            count += nameLength;

            // Skill list skills 보내기 처리
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.skills.Count);  // 스킬 갯수 직렬화, ushort로 메모리 아끼는거 기억!
            count += sizeof(ushort);
            foreach (Skill skill in this.skills) // foreach를 통해 각 skill 마다 Write로 s에 직렬화해줌
                success &= skill.Write(s, ref count);

            success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
            if (success == false)
                return null;
            return SendBufferHelper.Close(count);
        }
    }

    class ClientSession : PacketSession // 실제로 각 상황에서 사용될 기능 구현     // Client에 앉혀둘 대리인
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            Thread.Sleep(5000);

            DisConnect();
        }
        //[4][.] [I][D] [][][][][][][][]
        
        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            ushort count = 0;
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;
            ushort ID = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            switch ((PacketIDEnum)ID)
            {
                case PacketIDEnum.PlayerInfoRequirement:
                    {
                        PlayerInfoRequirement p = new PlayerInfoRequirement();
                        p.ReadBuffer(buffer);
                        Console.WriteLine($"PlayerInfoRequirement : {p.playerID}, PlayerName : {p.name}");

                        foreach (PlayerInfoRequirement.Skill skill in p.skills)
                            Console.WriteLine($"Skill : {skill.id}, Skill lvl : {skill.level}, Skill Duration : {skill.duration}");
                    }
                    break;
                /*case PacketIDEnum.PlayerInfoOk:
                    {

                    }
                    break*/
            }

            Console.WriteLine($"ReceivePacketID : {ID}, size : {size}");
        }

        public override void OnDisConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnected : {endPoint}");
        }

        public override void OnSend(int numOfbytes)
        {
            Console.WriteLine($"Transferred args byte : {numOfbytes}");
        }
    }
}
