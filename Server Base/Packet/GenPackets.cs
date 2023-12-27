using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
		
		public List<Attribute> attributes = new List<Attribute>();
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
			for(int i = 0; i < attributeLength; i++)
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
		for(int i = 0; i < skillLength; i++)
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

class Test
{
    public int testint;
	public string none;

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
		// int testint 읽기
		this.testint = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		count += sizeof(int);
		
		// string none 읽기
		ushort noneLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		this.none = Encoding.Unicode.GetString(s.Slice(count, nameLength));
		count += noneLength;
    } 
    
    public ArraySegment<byte> WriteBuffer()
    {
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.Test);
        count += sizeof(ushort);

        
		// int testint 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.testint);
		count += sizeof(int);
		
		// string none 보내기
		ushort noneLength = (ushort)Encoding.Unicode.GetBytes(this.none, 0, this.none.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), noneLength);
		count += sizeof(ushort);
		count += noneLength;           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}