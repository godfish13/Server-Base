START ../../PacketGenerator/PacketGenerator/bin/Debug/PacketGenerator.exe ../../PacketGenerator/PacketGenerator/PDL.xml
XCOPY /Y GenPackets.cs "../../DummyClient/Packet"
XCOPY /Y GenPackets.cs "../../Client/Assets/Scripts/Packet"
XCOPY /Y GenPackets.cs "../../Server Base/Packet"
XCOPY /Y ClientPacketManager.cs "../../DummyClient/Packet"
XCOPY /Y ClientPacketManager.cs "../../Client/Assets/Scripts/Packet"
XCOPY /Y ServerPacketManager.cs "../../Server Base/Packet"