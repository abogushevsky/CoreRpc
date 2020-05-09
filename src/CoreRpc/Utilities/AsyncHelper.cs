using System.Reflection;
using System.Threading.Tasks;

namespace CoreRpc.Utilities
{
    internal static class AsyncHelper
    {
        public static bool IsAsyncMethod(MethodInfo methodInfo) => typeof(Task).IsAssignableFrom(methodInfo.ReturnType);
        
        public static bool IsVoidAsyncMethod(MethodInfo methodInfo) => methodInfo.ReturnType == typeof(Task);
    }
}