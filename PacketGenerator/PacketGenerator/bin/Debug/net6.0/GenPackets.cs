
class PlayerInfoRequirement
{
    public long playerID;
	public string name;
	

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.playerID = BitConverter.ToInt64(s.Slice(count, s.Length - count));
		count += sizeof(long);
		ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
		count += nameLength;
		
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

        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerID);
		count += sizeof(long);
		ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
		count += sizeof(ushort);
		count += nameLength;
		           

        success &= BitConverter.TryWriteBytes(s, count); // count == packet.size
        if (success == false)
            return null;
        return SendBufferHelper.Close(count);         
    }
}
