﻿using System;
using System.Collections.Generic;
using System.Linq;
using homeControl.Configuration;
using homeControl.Configuration.Sensors;
using homeControl.Noolite.Adapters;
using homeControl.Noolite.Configuration;
using homeControl.Peripherals;
using ThinkingHome.NooLite.ReceivedData;

namespace homeControl.Noolite
{
    internal sealed class NooliteSensor : IDisposable
    {
        private const byte CommandOn = 2;
        private const byte CommandOff = 0;

        private readonly ISensorGate _sensorGate;
        private readonly IRX2164Adapter _adapter;
        private readonly Lazy<IDictionary<byte, NooliteSensorConfig>> _channelToConfig;

        public NooliteSensor(
            ISensorGate sensorGate,
            IRX2164Adapter adapter,
            ISensorConfigurationRepository configuration)
        {
            Guard.DebugAssertArgumentNotNull(sensorGate, nameof(sensorGate));
            Guard.DebugAssertArgumentNotNull(adapter, nameof(adapter));
            Guard.DebugAssertArgumentNotNull(configuration, nameof(configuration));

            _sensorGate = sensorGate;
            _adapter = adapter;
            _channelToConfig = new Lazy<IDictionary<byte, NooliteSensorConfig>>(() => LoadConfig(configuration));
        }

        public void Activate()
        {
            _adapter.CommandReceived += AdapterOnCommandReceived;
            _adapter.Activate();
        }

        private static Dictionary<byte, NooliteSensorConfig> LoadConfig(ISensorConfigurationRepository config)
        {
            Guard.DebugAssertArgumentNotNull(config, nameof(config));

            try
            {
                return config.GetAllConfigs<NooliteSensorConfig>().ToDictionary(cfg => cfg.Channel);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidConfigurationException(ex, "Found duplicated Noolite sensor channels in the configuration file.");
            }
        }

        private void AdapterOnCommandReceived(ReceivedCommandData receivedCommandData)
        {
            Guard.DebugAssertArgumentNotNull(receivedCommandData, nameof(receivedCommandData));

            NooliteSensorConfig config;
            if (!_channelToConfig.Value.TryGetValue(receivedCommandData.Channel, out config))
                throw new InvalidConfigurationException($"Could not locate Noolite channel #{receivedCommandData.Channel} in the configuration.");

            switch (receivedCommandData.Cmd)
            {
                case CommandOn:
                    _sensorGate.OnSensorActivated(config.SensorId);
                    break;
                case CommandOff:
                    _sensorGate.OnSensorDeactivated(config.SensorId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(receivedCommandData.Cmd));
            }
        }

        public void Dispose()
        {
            _adapter.CommandReceived -= AdapterOnCommandReceived;
        }
    }
}
