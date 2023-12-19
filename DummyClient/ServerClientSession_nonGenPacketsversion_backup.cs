// 빌드에서 제외, Packet자동생성 이전버전 백업용 파일

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
    class PlayerInfoRequirement
    {
        public long playerID;
        public string name;

        public struct Skill
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

        public List<Skill> skills = new List<Skill>();


        public ArraySegment<byte> WriteBuffer()
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096); // static threadLocal CurrentBuffer 생성 및 4096크기 할당 요청
            bool success = true;
            ushort count = 0;

            Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);
            // Span : ArraySegment와 마찬가지로 Array의 범위를 지정하는 역할

            // GetBytes 후 Copy 대신 openSegment에 바로 packet 입력 // 단, 유니티 옛버전에서는 TryWriteBytes가 사용불가능함! 자기가 쓰는 버전 확인해보기
            //success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count), packet.size); // 아래로 이동됨!
            count += sizeof(ushort); // packet.size는 ushort 타입, 그러므로 첫번째로 버퍼 가장 앞부분에 size의 공간을 미리 확보해둠
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.PlayerInfoRequirement);
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
            foreach (Skill skill in skills) // foreach를 통해 각 skill 마다 Write로 s에 직렬화해줌
                success &= skill.Write(s, ref count);


            success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
            // 가장 밑줄에서 size == count를 buffer에 넣어줘야함!! 왜냐하면 최종적인 packet의 크기는 이것저것 넣고 난 뒤의 최종 count수치이기 때문!!

            if (success == false)   // 변환 실패시 null 반환
                return null;

            return SendBufferHelper.Close(count);  // 완성된 CurrentBuffer를 sendBuff으로 보냄         
        }

        public void ReadBuffer(ArraySegment<byte> segment)
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
                Skill skill = new Skill();
                skill.Read(s, ref count);
                skills.Add(skill);
            }
        }
    }

    public enum PacketIDEnum
    {
        PlayerInfoRequirement = 1,
        PlayerInfoOk = 2,
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