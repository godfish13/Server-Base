using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Base
{
    // PacketHandler의 Push에 넣어줄 Task를 Lamda대신 Task 클래스를 파서 넣기 위한 클래스
    // JobQueue를 만들고 Lamda를 Push하는 대신 각 기능을 따로 구현해두고 사용할 수 있음
    // 현 프로젝트에서 따로 사용하진 않았지만 Task 전용 Queue를 만드는 것임

    interface ITask
    {
        void Execute();
    }

    public class BroadCastTask : ITask
    {
        GameRoom _room;
        ClientSession _session;
        string _chat;

        BroadCastTask(GameRoom room, ClientSession session, string chat)
        {
            _room = room;
            _session = session;
            _chat = chat;
        }
        public void Execute()
        {
            //_room.BroadCast(_session, _chat);
        }
    }

    class TaskQueue
    {
        Queue<ITask> _queue = new Queue<ITask>();
    }
}
