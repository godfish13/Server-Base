using DummyClient;
using ServerCore;
using UnityEngine;

class PacketHandler
{
    public static void S_ChatHandler(PacketSession session, IPacket packet)
    {
        S_Chat chatPacket = packet as S_Chat;
        ServerSession serverSession = session as ServerSession;

        //if (chatPacket.playerid == 1)
        {
            Debug.Log(chatPacket.chat);
            GameObject go = GameObject.Find("Player");
            if (go == null)
                Debug.Log("Can't find Player");
            else
                Debug.Log("Player Founded");
        }
    }
}
