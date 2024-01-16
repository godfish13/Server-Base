using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Base
{
    class GameRoom : IJobQueue     // 채팅 인원들 들어와있을 곳
    {
        List<ClientSession> _sessions = new List<ClientSession>();
        JobQueue _jobQueue = new JobQueue();

        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); // 패킷 모아 보내기용 리스트

        public void Push(Action job)
        {
            _jobQueue.Push(job);
        }

        public void Flush() // jobQueue 내에서 하나의 스레드만 사용되도록 보장되므로 lock 안검!
        {
            foreach (ClientSession s in _sessions)  // JobQueue 내에서 lock을 잡고 움직이므로 lock 제거
                s.Send(_pendingList);

            Console.WriteLine($"Flushed {_pendingList.Count} Items");
            _pendingList.Clear();
        }

        public void BroadCast(ClientSession session, string chat)   // Client가 보낸 chat 서버 내 전체 인원에게 발송
        {
            S_Chat packet = new S_Chat();
            packet.playerid = session.Sessionid;
            packet.chat = $"{packet.playerid} : {chat}";
            ArraySegment<byte> segment = packet.WriteBuffer();

            /*lock (_lock)        // 해당 파트에서 패킷이 몰림, 틱이 밀리기 쉬움 => 각 스레드가 패킷을 Queue에 넣고 돌아가도록 설계하는 게 JobQueue설계!
            {
                foreach (ClientSession s in _sessions)
                    s.Send(segment);               
            }*/

            // 지금 유저수(n)^2 만큼의 Send를 발생시킴 => 최적화를 시키기 위해 이또한 Queue에 담아 패킷 뭉쳐서 보내기 만들기!
            // Queue에 담으면 n만큼만 Send 발생 가능
            //foreach (ClientSession s in _sessions)  // JobQueue 내에서 lock을 잡고 움직이므로 lock 제거
            //    s.Send(segment);
            _pendingList.Add(segment);
        }


        public void Enter(ClientSession session)
        {
            
            _sessions.Add(session);
            session.Room = this;
                   
        }

        public void Leave(ClientSession session)
        {           
            _sessions.Remove(session);           
        }

    }
}
