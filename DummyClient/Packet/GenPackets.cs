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
    C_Chat = 1,
	S_Chat = 2,
	
}

interface IPacket
{
    ushort Protocol { get; }
	void ReadBuffer(ArraySegment<byte> segement);
	ArraySegment<byte> WriteBuffer();
}



class C_Chat : IPacket
{
    public string chat;

    public ushort Protocol => (ushort)PacketIDEnum.C_Chat;    

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
		// string chat 읽기
		ushort chatLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		this.chat = Encoding.Unicode.GetString(s.Slice(count, chatLength));
		count += chatLength;
    } 
    
    public ArraySegment<byte> WriteBuffer()
    {
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.C_Chat);
        count += sizeof(ushort);

        
		// string chat 보내기
		ushort chatLength = (ushort)Encoding.Unicode.GetBytes(this.chat, 0, this.chat.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), chatLength);
		count += sizeof(ushort);
		count += chatLength;           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}

class S_Chat : IPacket
{
    public int playerid;
	public string chat;

    public ushort Protocol => (ushort)PacketIDEnum.S_Chat;    

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
		// int playerid 읽기
		this.playerid = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		count += sizeof(int);
		
		// string chat 읽기
		ushort chatLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		this.chat = Encoding.Unicode.GetString(s.Slice(count, chatLength));
		count += chatLength;
    } 
    
    public ArraySegment<byte> WriteBuffer()
    {
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.S_Chat);
        count += sizeof(ushort);

        
		// int playerid 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerid);
		count += sizeof(int);
		
		// string chat 보내기
		ushort chatLength = (ushort)Encoding.Unicode.GetBytes(this.chat, 0, this.chat.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), chatLength);
		count += sizeof(ushort);
		count += chatLength;           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}