START ../../PacketGenerator/PacketGenerator/bin/Debug/PacketGenerator.exe ../../PacketGenerator/PacketGenerator/PDL.xml
XCOPY /Y GenPackets.cs "../../DummyClient/Packet"
XCOPY /Y GenPackets.cs "../../Server Base/Packet"