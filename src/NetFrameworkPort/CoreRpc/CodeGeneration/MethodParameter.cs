using System;
using System.Reflection;

namespace CoreRpc.CodeGeneration
{
	internal class MethodParameter
	{
		public MethodParameter(Type type, string name)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public MethodParameter(ParameterInfo parameterInfo)
			: this(parameterInfo.ParameterType, parameterInfo.Name)
		{
			
		}

		public Type Type { get; }

		public string Name { get; }
	}
}