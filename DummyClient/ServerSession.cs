using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DummyClient       
{
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

    class ServerSession : Session // 실제로 각 상황에서 사용될 기능 구현   // 서버에 앉혀둘 대리인
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            PlayerInfoRequirement packet = new PlayerInfoRequirement() { playerID = 1001, name = "ABCD", };

            var Skill = new PlayerInfoRequirement.Skill() { id = 501, level = 5, duration = 5 };
            Skill.attributes.Add(new PlayerInfoRequirement.Skill.Attribute() { att = 11 });
            packet.skills.Add(Skill);

            packet.skills.Add(new PlayerInfoRequirement.Skill() { id = 101, level = 1, duration = 3.0f });
            packet.skills.Add(new PlayerInfoRequirement.Skill() { id = 201, level = 2, duration = 2.5f });
            packet.skills.Add(new PlayerInfoRequirement.Skill() { id = 301, level = 3, duration = 2.0f });
            packet.skills.Add(new PlayerInfoRequirement.Skill() { id = 401, level = 4, duration = 1.5f });

            // 보낸다
            //for (int i = 0; i < 5; i++)
            {
                ArraySegment<byte> segment = packet.WriteBuffer();
                if (segment != null)
                    Send(segment);

                //Thread.Sleep(10);
            }
        }

        public override void OnDisConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnected : {endPoint}");
        }

        public override int OnReceive(ArraySegment<byte> buffer)
        {
            string receiveData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count); // init에서 SetBuffer로 설정한 값 추출하는 args 변수들
            Console.WriteLine($"[From Server] : {receiveData}");
            return buffer.Count;
        }

        public override void OnSend(int numOfbytes)
        {
            Console.WriteLine($"Transferred args byte : {numOfbytes}");
        }
    }
}