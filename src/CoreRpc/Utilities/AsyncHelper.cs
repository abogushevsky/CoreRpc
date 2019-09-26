using System.Reflection;
using System.Threading.Tasks;

namespace CoreRpc.Utilities
{
    internal static class AsyncHelper
    {
        public static bool IsAsyncMethod(MethodInfo methodInfo) => methodInfo.ReturnType.IsAssignableFrom(typeof(Task));
        
        public static bool IsVoidAsyncMethod(MethodInfo methodInfo) => methodInfo.ReturnType == typeof(Task);
    }
}