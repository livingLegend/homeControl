﻿using System;
using System.Collections.Generic;
using System.Linq;
using homeControl.Configuration;
using homeControl.Domain.Events;
using homeControl.Domain.Events.Sensors;
using homeControl.NooliteF.Adapters;
using homeControl.NooliteF.Configuration;
using Serilog;
using ThinkingHome.NooLite;
using ThinkingHome.NooLite.Internal;

namespace homeControl.NooliteF
{
    internal sealed class NooliteFSensor : IDisposable
    {
        private readonly IEventSender _eventSender;
        private readonly IMtrfAdapter _adapter;
        private readonly ILogger _log;
        private readonly Lazy<IDictionary<int, AbstractNooliteFSensorInfo>> _channelToSensorConfig;

        public NooliteFSensor(
            IEventSender eventSender,
            IMtrfAdapter adapter,
            INooliteFSensorInfoRepository sensorRepository,
            ILogger log)
        {
            Guard.DebugAssertArgumentNotNull(eventSender, nameof(eventSender));
            Guard.DebugAssertArgumentNotNull(adapter, nameof(adapter));
            Guard.DebugAssertArgumentNotNull(sensorRepository, nameof(sensorRepository));

            _eventSender = eventSender;
            _adapter = adapter;
            _log = log;
            _channelToSensorConfig = new Lazy<IDictionary<int, AbstractNooliteFSensorInfo>>(() => LoadSensorConfig(sensorRepository));
        }

        public void Activate()
        {
            _adapter.ReceiveData += AdapterOnReceiveData;
            _adapter.Activate();
            _log.Debug("Noolite sensor started.");
        }

        private static Dictionary<int, AbstractNooliteFSensorInfo> LoadSensorConfig(INooliteFSensorInfoRepository repository)
        {
            Guard.DebugAssertArgumentNotNull(repository, nameof(repository));

            try
            {
                return repository.GetAll().Result.ToDictionary(cfg => (int)cfg.Channel);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidConfigurationException(ex, "Found duplicated Noolite.F sensor channels in the configuration file.");
            }
        }
        
        private void AdapterOnReceiveData(object sender, ReceivedData receivedData)
        {
            Guard.DebugAssertArgumentNotNull(receivedData, nameof(receivedData));

            if (receivedData.Mode == MTRFXXMode.Service)
            {
                return;
            }

            if (receivedData.Result == ResultCode.NoResponse)
            {
                //todo noResponce event
                return;
            }
            
            switch (receivedData.Command)
            {
                case MTRFXXCommand.On:
                case MTRFXXCommand.TemporarySwitchOn:
                    var onSensorInfo = GetSensorInfo<OnOffNooliteFSensorInfo>(receivedData);
                    _eventSender.SendEvent(new SensorActivatedEvent(onSensorInfo.SensorId));
                    _log.Information("Noolite.F sensor activated: {SensorId}", onSensorInfo.SensorId);
                    break;
                
                case MTRFXXCommand.Off:
                    var offSensorInfo = GetSensorInfo<OnOffNooliteFSensorInfo>(receivedData);
                    _eventSender.SendEvent(new SensorDeactivatedEvent(offSensorInfo.SensorId));
                    _log.Information("Noolite.F sensor deactivated: {SensorId}", offSensorInfo.SensorId);
                    break;
                    
                case MTRFXXCommand.SendState:
                    break;
                
                case MTRFXXCommand.MicroclimateData when receivedData is MicroclimateData microclimateData:
                    var temperatureSensor = GetSensorInfo<TemperatureNooliteFSensorInfo>(receivedData);
                    _eventSender.SendEvent(new SensorValueEvent(temperatureSensor.TemperatureSensorId, microclimateData.Temperature));
                    if (microclimateData.Humidity.HasValue &&
                        temperatureSensor is TemperatureAndHumidityNooliteFSensorInfo humiditySensor)
                    {
                        _eventSender.SendEvent(new SensorValueEvent(humiditySensor.HumiditySensorId, microclimateData.Humidity.Value));
                    }
                    
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(receivedData.Command));
            }
        }

        private TInfo GetSensorInfo<TInfo>(ReceivedData receivedData) where TInfo : AbstractNooliteFSensorInfo
        {
            if (!_channelToSensorConfig.Value.TryGetValue(receivedData.Channel, out var info))
                throw new InvalidConfigurationException($"Could not locate Noolite channel #{receivedData.Channel} in the sensors configuration.");

            if (!(info is TInfo typedInfo))
                throw new InvalidConfigurationException($"Noolite sensor with channel #{receivedData.Channel} has invalid type {info.GetType()}. Expected type: {typeof(TInfo)}");
            
            return typedInfo;
        }

        public void Dispose()
        {
            _adapter.ReceiveData -= AdapterOnReceiveData;
        }
    }
}
