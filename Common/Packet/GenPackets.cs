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
    S_BroadCastEnterGame = 1,
	C_LeaveGame = 2,
	S_BroadCastLeaveGame = 3,
	S_PlayerList = 4,
	C_Move = 5,
	S_BroadCastMove = 6,
	
}

public interface IPacket
{
    ushort Protocol { get; }
	void ReadBuffer(ArraySegment<byte> segement);
	ArraySegment<byte> WriteBuffer();
}



public class S_BroadCastEnterGame : IPacket
{
    public int playerID;
	public float posX;
	public float posY;
	public float posZ;

    public ushort Protocol => (ushort)PacketIDEnum.S_BroadCastEnterGame;    

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
		// int playerID 읽기
		this.playerID = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		count += sizeof(int);
		
		// float posX 읽기
		this.posX = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		count += sizeof(float);
		
		// float posY 읽기
		this.posY = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		count += sizeof(float);
		
		// float posZ 읽기
		this.posZ = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		count += sizeof(float);
    } 
    
    public ArraySegment<byte> WriteBuffer()
    {
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.S_BroadCastEnterGame);
        count += sizeof(ushort);

        
		// int playerID 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerID);
		count += sizeof(int);
		
		// float posX 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posX);
		count += sizeof(float);
		
		// float posY 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posY);
		count += sizeof(float);
		
		// float posZ 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posZ);
		count += sizeof(float);           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}

public class C_LeaveGame : IPacket
{
    

    public ushort Protocol => (ushort)PacketIDEnum.C_LeaveGame;    

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
    } 
    
    public ArraySegment<byte> WriteBuffer()
    {
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.C_LeaveGame);
        count += sizeof(ushort);

                   

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}

public class S_BroadCastLeaveGame : IPacket
{
    public int playerID;

    public ushort Protocol => (ushort)PacketIDEnum.S_BroadCastLeaveGame;    

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
		// int playerID 읽기
		this.playerID = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		count += sizeof(int);
    } 
    
    public ArraySegment<byte> WriteBuffer()
    {
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.S_BroadCastLeaveGame);
        count += sizeof(ushort);

        
		// int playerID 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerID);
		count += sizeof(int);           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}

public class S_PlayerList : IPacket
{
    
	public List<Player> players = new List<Player>();
	public class Player
	{
	    public bool isSelf;
		public int playerID;
		public float posX;
		public float posY;
		public float posZ;
	
	    public void Read(ReadOnlySpan<byte> s, ref ushort count)
	    {
	        
			// bool isSelf 읽기
			this.isSelf = BitConverter.ToBoolean(s.Slice(count, s.Length - count));
			count += sizeof(bool);
			
			// int playerID 읽기
			this.playerID = BitConverter.ToInt32(s.Slice(count, s.Length - count));
			count += sizeof(int);
			
			// float posX 읽기
			this.posX = BitConverter.ToSingle(s.Slice(count, s.Length - count));
			count += sizeof(float);
			
			// float posY 읽기
			this.posY = BitConverter.ToSingle(s.Slice(count, s.Length - count));
			count += sizeof(float);
			
			// float posZ 읽기
			this.posZ = BitConverter.ToSingle(s.Slice(count, s.Length - count));
			count += sizeof(float);
	    }
	
	    public bool Write(Span<byte> s, ref ushort count)
	    {
	        bool success = true;
	        
			// bool isSelf 보내기
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.isSelf);
			count += sizeof(bool);
			
			// int playerID 보내기
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerID);
			count += sizeof(int);
			
			// float posX 보내기
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posX);
			count += sizeof(float);
			
			// float posY 보내기
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posY);
			count += sizeof(float);
			
			// float posZ 보내기
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posZ);
			count += sizeof(float);
	
	        return success;
	    }
	}
	

    public ushort Protocol => (ushort)PacketIDEnum.S_PlayerList;    

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
		// Player list players 읽기 처리
		players.Clear(); // 시작전 한번 청소
		ushort playerLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		for(int i = 0; i < playerLength; i++)
		{
		    Player player = new Player();
		    player.Read(s, ref count);
		    players.Add(player);
		}
    } 
    
    public ArraySegment<byte> WriteBuffer()
    {
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.S_PlayerList);
        count += sizeof(ushort);

        
		// Player list players 보내기 처리
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.players.Count);  // 스킬 갯수 직렬화, ushort로 메모리 아끼는거 기억!
		count += sizeof(ushort);
		foreach (Player player in this.players) // foreach를 통해 각 skill 마다 Write로 s에 직렬화해줌
		    success &= player.Write(s, ref count);           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}

public class C_Move : IPacket
{
    public float posX;
	public float posY;
	public float posZ;

    public ushort Protocol => (ushort)PacketIDEnum.C_Move;    

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
		// float posX 읽기
		this.posX = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		count += sizeof(float);
		
		// float posY 읽기
		this.posY = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		count += sizeof(float);
		
		// float posZ 읽기
		this.posZ = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		count += sizeof(float);
    } 
    
    public ArraySegment<byte> WriteBuffer()
    {
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.C_Move);
        count += sizeof(ushort);

        
		// float posX 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posX);
		count += sizeof(float);
		
		// float posY 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posY);
		count += sizeof(float);
		
		// float posZ 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posZ);
		count += sizeof(float);           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}

public class S_BroadCastMove : IPacket
{
    public int playerID;
	public float posX;
	public float posY;
	public float posZ;

    public ushort Protocol => (ushort)PacketIDEnum.S_BroadCastMove;    

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
		// int playerID 읽기
		this.playerID = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		count += sizeof(int);
		
		// float posX 읽기
		this.posX = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		count += sizeof(float);
		
		// float posY 읽기
		this.posY = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		count += sizeof(float);
		
		// float posZ 읽기
		this.posZ = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		count += sizeof(float);
    } 
    
    public ArraySegment<byte> WriteBuffer()
    {
        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;
        
        Span<byte> s = new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketIDEnum.S_BroadCastMove);
        count += sizeof(ushort);

        
		// int playerID 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerID);
		count += sizeof(int);
		
		// float posX 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posX);
		count += sizeof(float);
		
		// float posY 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posY);
		count += sizeof(float);
		
		// float posZ 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posZ);
		count += sizeof(float);           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}