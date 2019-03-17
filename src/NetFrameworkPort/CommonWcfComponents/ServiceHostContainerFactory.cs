using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Xml;

namespace CommonWcfComponents
{
    public static class ServiceHostContainerFactory
    {
        public static ServiceHostContainer<TContract> Create<TService, TContract>(string addressPrefix = null) where TService : TContract
        {
            try
            {
                var host = new ServiceHost(typeof(TService),
                    new Uri($"net.tcp://localhost/{addressPrefix ?? string.Empty}"));
                var timeout = TimeSpan.FromMinutes(5);
                var binding = new NetTcpBinding(SecurityMode.None)
                {
                    CloseTimeout = timeout,
                    OpenTimeout = timeout,
                    ReceiveTimeout = timeout,
                    SendTimeout = timeout,
                    MaxConnections = 1000,
                    MaxReceivedMessageSize = 2147483647,
                    MaxBufferPoolSize = 2147483647,
                    MaxBufferSize = 2147483647,
                    ReaderQuotas = new XmlDictionaryReaderQuotas()
                    {
                        MaxDepth = 2147483647,
                        MaxStringContentLength = 2147483647,
                        MaxArrayLength = 2147483647,
                        MaxBytesPerRead = 2147483647,
                        MaxNameTableCharCount = 2147483647
                    }
                };
                host.AddServiceEndpoint(typeof(TContract), binding, $"{typeof(TContract).Name}");
                var throttlingBehavior = new ServiceThrottlingBehavior
                {
                    MaxConcurrentCalls = 1000,
                    MaxConcurrentInstances = 1000,
                    MaxConcurrentSessions = 1000
                };
                host.Description.Behaviors.Add(throttlingBehavior);
                host.Open();
                return new ServiceHostContainer<TContract>(host, (TContract) host.SingletonInstance);
            }
            catch (Exception ex)
            {
                var eventLog = EventLog.GetEventLogs().FirstOrDefault(log => log.LogDisplayName == "Application");
                if (eventLog != null)
                {
                    eventLog.Source = $"{Process.GetCurrentProcess().ProcessName}_{addressPrefix}";
                    eventLog.WriteEntry(ex.ToString(), EventLogEntryType.Error);
                }
                throw;
            }
        }
    }
}