using System.Threading.Tasks;

namespace CoreRpc.Networking.Rpc
{
    public class AsyncOperationCallResult
    {
        public AsyncOperationCallResult(Task task, bool isVoid)
        {
            Task = task;
            IsVoid = isVoid;
        }

        public Task Task { get; }
		
        public bool IsVoid { get; }
    }
}