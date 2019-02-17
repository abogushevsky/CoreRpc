using System;
using System.Reflection;
using System.Text;

namespace CoreRpc.Utilities
{
	internal static class ReflectionExtentions
	{
		public static string GetFullMethodName(this MethodInfo methodInfo)
		{
			var methodNameBuilder = new StringBuilder();
			methodNameBuilder.Append(methodInfo.Name);
			methodInfo.GetParameters().ForEach(parameterInfo => methodNameBuilder.Append($"_{parameterInfo.Name}"));
			return methodNameBuilder.ToString();
		}

		public static void ThrowIfNotInterfaceType(this Type type)
		{
			if (!type.IsInterface)
			{
				throw new Exception("Type should be an interface");
			}
		}
	}
}