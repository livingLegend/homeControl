﻿using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using homeControl.ControllerService.Bindings;
using homeControl.ControllerService.Sensors;
using homeControl.Domain;
using homeControl.Domain.Events;
using homeControl.Domain.Events.Sensors;
using Moq;
using Serilog;
using Xunit;

namespace homeControl.Tests.Controller
{
    public class SensorEventsProcessorTests
    {
        [Fact]
        public void TestActivation()
        {
            var sensorId = SensorId.NewId();
            var sensorActivatedEvent = new SensorActivatedEvent(sensorId);
            var controllerMock = new Mock<IBindingController>(MockBehavior.Strict);
            controllerMock.Setup(controller => controller.ProcessSensorActivation(sensorId)).Returns(Task.CompletedTask);
            var eventsSourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            eventsSourceMock.Setup(e => e.ReceiveEvents<AbstractSensorEvent>()).Returns(Observable.Repeat(sensorActivatedEvent, 1));

            var handler = new SensorEventsProcessor(controllerMock.Object, eventsSourceMock.Object, Mock.Of<ILogger>());
            handler.Run(CancellationToken.None);

            controllerMock.Verify(controller => controller.ProcessSensorActivation(sensorId), Times.Once);
        }

        [Fact]
        public void TestDeactivation()
        {
            var sensorId = SensorId.NewId();
            var sensorDeactivatedEvent = new SensorDeactivatedEvent(sensorId);
            var controllerMock = new Mock<IBindingController>(MockBehavior.Strict);
            controllerMock.Setup(controller => controller.ProcessSensorDeactivation(sensorId)).Returns(Task.CompletedTask);
            var eventsSourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            eventsSourceMock.Setup(e => e.ReceiveEvents<AbstractSensorEvent>()).Returns(Observable.Repeat(sensorDeactivatedEvent, 1));

            var handler = new SensorEventsProcessor(controllerMock.Object, eventsSourceMock.Object, Mock.Of<ILogger>());
            handler.Run(CancellationToken.None);

            controllerMock.Verify(controller => controller.ProcessSensorDeactivation(sensorId), Times.Once);
        }
        
        [Fact]
        public void TestValue()
        {
            var sensorId = SensorId.NewId();
            const decimal value = 10m;
            var valueEvent = new SensorValueEvent(sensorId, value);
            var controllerMock = new Mock<IBindingController>(MockBehavior.Strict);
            controllerMock.Setup(controller => controller.ProcessSensorValue(sensorId, value)).Returns(Task.CompletedTask);
            var eventsSourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            eventsSourceMock.Setup(e => e.ReceiveEvents<AbstractSensorEvent>()).Returns(Observable.Repeat(valueEvent, 1));

            var handler = new SensorEventsProcessor(controllerMock.Object, eventsSourceMock.Object, Mock.Of<ILogger>());
            handler.Run(CancellationToken.None);

            controllerMock.Verify(controller => controller.ProcessSensorValue(sensorId, value), Times.Once);
        }
    }
}