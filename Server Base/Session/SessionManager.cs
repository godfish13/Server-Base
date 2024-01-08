using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Base
{
    class SessionManager    // session 관련 문의가 오면 입장시키기/찾기/제거 등 관리
    {
        static SessionManager _session = new SessionManager();
        public static SessionManager instance { get { return _session; } }

        int _sessionid = 0;
        Dictionary<int, ClientSession> _sessionDic = new Dictionary<int, ClientSession>();
        object _lock = new object();

        public ClientSession Generate() // 입장요청한 세션에 id부여 및 입장기록 저장
        {
            lock (_lock)
            {
                int sessionid = ++_sessionid;
                ClientSession session = new ClientSession();
                session.Sessionid = sessionid;
                _sessionDic.Add(sessionid, session);

                //Console.WriteLine($"Connected : {session.Sessionid}");

                return session;
            }
        }

        public ClientSession Find(int id)   // id를 통해 Session 찾아줌
        {
            lock ( _lock)
            {
                ClientSession session = null;
                _sessionDic.TryGetValue(id, out session);
                return session;
            }
        }

        public void Remove(ClientSession session)
        {
            lock (_lock)
            {
                _sessionDic.Remove(session.Sessionid);
            }
        }
    }
}
