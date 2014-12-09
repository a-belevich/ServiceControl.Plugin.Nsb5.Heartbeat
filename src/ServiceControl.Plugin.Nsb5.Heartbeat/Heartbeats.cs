namespace ServiceControl.Plugin.Nsb5.Heartbeat
{
    using System;
    using System.Configuration;
    using System.Threading;
    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.Hosting;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using ServiceControl.Plugin.Heartbeat.Messages;

    class Heartbeats : Feature, IWantToRunWhenConfigurationIsComplete
    {
        public ISendMessages SendMessages { get; set; }
        public Configure Configure { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public CriticalError CriticalError { get; set; }
        

        public Heartbeats()
        {
            EnableByDefault();
        }

        public void Run(Configure config)
        {
            if (!IsEnabledByDefault)
            {
                return;
            }
            backend = new ServiceControlBackend(SendMessages, Configure, CriticalError);
            heartbeatInterval = TimeSpan.FromSeconds(10);
            var interval = ConfigurationManager.AppSettings[@"Heartbeat/Interval"];
            if (!String.IsNullOrEmpty(interval))
            {
                heartbeatInterval = TimeSpan.Parse(interval);
            }
            
            var hostInfo = UnicastBus.HostInformation;

            SendStartupMessageToBackend(hostInfo);

            heartbeatTimer = new Timer(x => ExecuteHeartbeat(hostInfo), null, TimeSpan.Zero, heartbeatInterval);
        }

        void SendStartupMessageToBackend(HostInformation hostInfo)
        {
            backend.Send(new RegisterEndpointStartup
            {
                HostId = hostInfo.HostId, 
                Host = hostInfo.DisplayName,
                Endpoint = Configure.Settings.EndpointName(),
                HostDisplayName = hostInfo.DisplayName,
                HostProperties = hostInfo.Properties,
                StartedAt = DateTime.UtcNow
            });
        }

     
        void ExecuteHeartbeat(HostInformation hostInfo)
        {
            var heartBeat = new EndpointHeartbeat
            {
                ExecutedAt = DateTime.UtcNow,
                EndpointName = Configure.Settings.EndpointName(),
                Host = hostInfo.DisplayName,
                HostId = hostInfo.HostId
            };

            backend.Send(heartBeat, TimeSpan.FromTicks(heartbeatInterval.Ticks * 4));
        }

        ServiceControlBackend backend;
// ReSharper disable once NotAccessedField.Local
        Timer heartbeatTimer;
        TimeSpan heartbeatInterval;

        protected override void Setup(FeatureConfigurationContext context)
        {
        }
        
    }
}