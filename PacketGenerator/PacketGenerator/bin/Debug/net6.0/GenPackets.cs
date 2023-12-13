class PlayerInfoRequirement
{
    public long playerID;
	public string name;
	public struct Skill
	{
	    public int id;
		public short level;
		public float duration;
	
	    public void Read(ReadOnlySpan<byte> s, ref ushort count)
	    {
	        // 단순 변수 읽기
			this.id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
			count += sizeof(int);
			// 단순 변수 읽기
			this.level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
			count += sizeof(short);
			// 단순 변수 읽기
			this.duration = BitConverter.ToSingle(s.Slice(count, s.Length - count));
			count += sizeof(float);
	    }
	
	    public bool Write(Span<byte> s, ref ushort count)
	    {
	        bool success = true;
	        // 단순 변수 보내기
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.id);
			count += sizeof(int);
			// 단순 변수 보내기
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.level);
			count += sizeof(short);
			// 단순 변수 보내기
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.duration);
			count += sizeof(float);
	
	        return success;
	    }
	}
	public List<Skill> skills = new List<Skill>();

    public void ReadBuffer(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        // 단순 변수 읽기
		this.playerID = BitConverter.ToInt64(s.Slice(count, s.Length - count));
		count += sizeof(long);
		
		// string 읽기
		ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
		count += nameLength;
		
		// list 처리 읽기
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

        // 단순 변수 보내기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerID);
		count += sizeof(long);
		
		// string 보내기
		ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, openSegment.Array, openSegment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
		count += sizeof(ushort);
		count += nameLength;
		
		// list 보내기
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