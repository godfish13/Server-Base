using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Base
{
    class GameRoom     // 채팅 인원들 들어와있을 곳
    {
        List<ClientSession> _sessions = new List<ClientSession>();
        object _lock = new object();

        public void BroadCast(ClientSession session, string chat)   // Client가 보낸 chat 서버 내 전체 인원에게 발송
        {
            S_Chat packet = new S_Chat();
            packet.playerid = session.Sessionid;
            packet.chat = $"{packet.playerid} : {chat}";
            ArraySegment<byte> segment = packet.WriteBuffer();

            lock (_lock)        // 해당 파트에서 패킷이 몰림, 틱이 밀리기 쉬움 => 각 스레드가 패킷을 Queue에 넣고 돌아가도록 설계하는 게 JobQueue설계!
            {
                foreach (ClientSession s in _sessions)
                    s.Send(segment);               
            }
        }


        public void Enter(ClientSession session)
        {
            lock (_lock) 
            {
                _sessions.Add(session);
                session.Room = this;
            }         
        }

        public void Leave(ClientSession session)
        {
            lock (_lock) 
            {
                _sessions.Remove(session);
            }
        }
    }
}
