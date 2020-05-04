using System;
using System.Threading.Tasks;
using CoreRpc.Serialization;

namespace CoreRpc.Networking.Rpc
{
    public class AsyncOperationCallResult
    {
        public AsyncOperationCallResult(Task task)
        {
            Task = task;
            IsVoid = true;
        }

        public AsyncOperationCallResult(
            Task task, 
            bool isVoid, 
            Type resultType,
            Func<ISerializer<object>> createSerializer)
        {
            Task = task;
            IsVoid = isVoid;
            ResultType = resultType;
            CreateSerializer = createSerializer;
        }

        public Task Task { get; }
        
        public Type ResultType { get; }
        
        public Func<ISerializer<object>> CreateSerializer { get; }
		
        public bool IsVoid { get; }
    }
}