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
        {                   // 모든 ClientSession들에게 _pendingList 발송때려줌
            foreach (ClientSession s in _sessions)  // JobQueue 내에서 lock을 잡고 움직이므로 lock 제거
                s.Send(_pendingList);

            //Console.WriteLine($"Flushed {_pendingList.Count} Items");
            _pendingList.Clear();
        }

        public void BroadCast(ArraySegment<byte> segment)   // 서버가 room 내 전체 인원에게 패킷을 발송함
        {
            _pendingList.Add(segment);
        }


        public void Enter(ClientSession session)
        {
            // 새로 들어온 플레이어 Room에 추가
            _sessions.Add(session);
            session.Room = this;
                   
            // 신입생한테 기존 모든 플레이어 정보 전송
            S_PlayerList players = new S_PlayerList();  // 플레이어 목록 패킷
            foreach (ClientSession s in _sessions)
            {
                players.players.Add(new S_PlayerList.Player()
                {
                    isSelf = (s == session),    // 탐색한 session이 자기일 경우 true
                    playerID = s.Sessionid,
                    posX = s.PosX,
                    posY = s.PosY,
                    posZ = s.PosZ,
                });
            }
            session.Send(players.WriteBuffer());    // players패킷 작성하고 전송

            // 신입생이 입장했다고 기존 모든 플레이어한테 방송
            S_BroadCastEnterGame enter = new S_BroadCastEnterGame();
            enter.playerID = session.Sessionid;
            enter.posX = 0;
            enter.posY = 0;
            enter.posZ = 0;
            BroadCast(enter.WriteBuffer());
        }

        public void Leave(ClientSession session)
        {           
            // 플레이어 제거
            //int index = _sessions.IndexOf(session);
            //if (index != -1)
                _sessions.Remove(session);
            //else
            //    Console.WriteLine($"{session.Sessionid} is not Founded");
            
            // 특정 유저 나갔다는 것을 방송
            S_BroadCastLeaveGame leave = new S_BroadCastLeaveGame();
            leave.playerID = session.Sessionid;
            BroadCast(leave.WriteBuffer());
        }

        public void Move(ClientSession session, C_Move packet)
        {
            // 좌표 바꿔주기
            session.PosX = packet.posX;
            session.PosY = packet.posY;
            session.PosZ = packet.posZ;

            // 나머지 인원에게 옮겨진 좌표 방송
            S_BroadCastMove move = new S_BroadCastMove();
            move.playerID = session.Sessionid;
            move.posX = session.PosX;
            move.posY = session.PosY;
            move.posZ = session.PosZ;
            BroadCast(move.WriteBuffer());
        }
    }
}
