using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public interface IJobQueue
    {
        void Push(Action job);
    }

    public class JobQueue : IJobQueue
    {
        Queue<Action> _jobQueue = new Queue<Action>();
        object _lock = new object();
        bool _flush = false;    // Queue에 쌓아놓은 Action을 실행할지 말지 판단

        public void Push(Action job) 
        {
            bool flush = false; // lock 내에서만 작동하면 다른 Push들이 접근하기 어려워지므로 단계적으로 bool 사용

            lock (_lock) // _jobQueue에 멀티스레드로 접근되는것을 방지하기 위해 lock걸어줌
            {
                _jobQueue.Enqueue(job);
                if (_flush == false)
                    flush = _flush = true;
            }

            if (flush)   // 위의 lock통과한 스레드만 실행시키므로 멀티스레드 걱정 x
            {
                Flush();
            }
        }

        void Flush()
        {
            while (true)    // _jobQueue에 쌓인 모든 Action 실행
            {
                Action action = Pop();
                if (action == null)
                    return;

                action.Invoke();
            }
        }

        Action Pop()
        {
            lock (_lock)
            {
                if (_jobQueue.Count == 0)
                { 
                    _flush = false;
                    return null;
                }
                return _jobQueue.Dequeue();
            }
        }
    }
}
