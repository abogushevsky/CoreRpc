using System;
using CoreRpc.Logging;
using CoreRpc.Serialization;
using CoreRpc.Serialization.MessagePack;

namespace CoreRpc.Networking.Rpc
{
	public sealed class RpcTcpServicePublisher
	{
		public RpcTcpServicePublisher(ILogger logger) : this(new MessagePackSerializerFactory(), logger)
		{
		}
		
		public RpcTcpServicePublisher(ISerializerFactory serializerFactory, ILogger logger)
		{
			_serializerFactory = serializerFactory;
			_logger = logger;
		}

		public RpcTcpServer<TService> PublishUnsecured<TService>(
			TService serviceInstance, 
			TimeSpan serviceShutdownTimeout)
		{
			var service = new RpcTcpServer<TService>(
				serviceInstance, 
				serviceShutdownTimeout, 
				_serializerFactory, 
				_logger);
			service.Start();
			return service;
		}

		public RpcTcpServer<TService> PublishSecured<TService>(
			TService serviceInstance,
			TimeSpan serviceShutdownTimeout,
			ServerSslSettings sslSettings)
		{
			var service = new RpcTcpServer<TService>(
				serviceInstance, 
				serviceShutdownTimeout, 
				sslSettings, 
				_serializerFactory, 
				_logger);
			service.Start();
			return service;
		}

		private readonly ISerializerFactory _serializerFactory;
		private readonly ILogger _logger;
	}
}