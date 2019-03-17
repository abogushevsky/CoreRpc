using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Xml;

namespace CommonWcfComponents
{
    public class ServiceClient<TContract> : IDisposable
    {
        private ServiceClient(ChannelFactory<TContract> channelFactory, string addressPrefix)
        {
            _channelFactory = channelFactory;
            _addressPrefix = addressPrefix;
        }

        public TContract Service => _channelFactory.CreateChannel();

        public TResult Execute<TResult>(Func<TContract, TResult> func)
        {
            var channel = _channelFactory.CreateChannel();
            return func(channel);
        }

        public void Execute(Action<TContract> action)
        {
            var channel = _channelFactory.CreateChannel();
            action(channel);
        }

        public static ServiceClient<TContract> Create(string addressPrefix = null)
        {
            var timeout = TimeSpan.FromMinutes(5);
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
            {
                CloseTimeout = timeout,
                OpenTimeout = timeout,
                ReceiveTimeout = timeout,
                SendTimeout = timeout,
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
            var endpointAddress = !string.IsNullOrEmpty(addressPrefix)
                ? $"net.pipe://localhost/{addressPrefix}/{typeof(TContract).Name}"
                : $"net.pipe://localhost/{typeof(TContract).Name}";
            var channelFactory = new ChannelFactory<TContract>(
                binding,
                new EndpointAddress(endpointAddress));
            return new ServiceClient<TContract>(channelFactory, addressPrefix);
        }

        public void Dispose()
        {
            try
            {
                _channelFactory.Close();
            }
            catch (Exception ex)
            {
                var eventLog = EventLog.GetEventLogs().FirstOrDefault(log => log.LogDisplayName == "Application");
                if (eventLog != null)
                {
                    eventLog.Source = $"{Process.GetCurrentProcess().ProcessName}_{_addressPrefix}";
                    eventLog.WriteEntry(ex.ToString(), EventLogEntryType.Error);
                }
            }
        }

        private readonly ChannelFactory<TContract> _channelFactory;
        private readonly string _addressPrefix;
    }
}