using System;
using System.Threading.Tasks;

namespace CoreRpc.Networking.Rpc
{
    internal class AsyncOperationCallResult
    {
        public AsyncOperationCallResult(Task task)
        {
            Task = task;
            IsVoid = true;
        }

        public AsyncOperationCallResult(Task task, Func<Task, ServiceCallResult> getResult)
        {
            Task = task;
            GetResult = getResult;
            IsVoid = false;
        }

        public Task Task { get; }
        
        public Func<Task, ServiceCallResult> GetResult { get; }

        public bool IsVoid { get; }
    }
}