﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using homeControl.Domain.Events;
using homeControl.Interop.Rabbit;
using Moq;
using Xunit;

namespace homeControl.Tests.Interop
{
    public class BusTests
    {
        private class TestEvent : IEvent { }
        private class DerivedTestEvent : TestEvent { }
        private class AnotherTestEvent : IEvent { }

        [Fact]
        public void TestSender_WhenEventNotConfigured_ThenError()
        {
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            var exchangeConfiguration = new ExchangeConfiguration();

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSender;
            Assert.Throws<InvalidOperationException>(() => bus.SendEvent(new TestEvent()));
        }

        [Fact]
        public void TestSource_WhenEventNotConfigured_ThenError()
        {
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            var exchangeConfiguration = new ExchangeConfiguration();

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSource;
            Assert.Throws<InvalidOperationException>(() => bus.ReceiveEvents<TestEvent>());
        }

        [Fact]
        public void TestSender_WhenEventConfigured_ThenSend()
        {
            const string exchangeName = "exchange";

            var @event = new TestEvent();
            var senderMock = new Mock<IEventSender>(MockBehavior.Strict);
            senderMock.Setup(m => m.SendEvent(@event));
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSender(exchangeName)).Returns(senderMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSender(typeof(TestEvent), exchangeName);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSender;
            bus.SendEvent(@event);

            senderMock.Verify(m => m.SendEvent(@event), Times.Once);
        }

        [Fact]
        public void TestSource_WhenEventConfigured_ThenReceive()
        {
            const string exchangeName = "exchange";
            const string exchangeType = "type";
            const string routingKey = "route";

            var @event = new TestEvent();
            var sourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            sourceMock.Setup(m => m.ReceiveEvents<TestEvent>()).Returns(Observable.Return(@event));
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSource(exchangeName, exchangeType, routingKey)).Returns(sourceMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSource(typeof(TestEvent), exchangeName, exchangeType, routingKey);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSource;
            var receivedEvent = bus.ReceiveEvents<TestEvent>().Wait();

            sourceMock.Verify(m => m.ReceiveEvents<TestEvent>(), Times.Once);
            Assert.Equal(@event, receivedEvent);
        }

        [Fact]
        public void TestSender_WhenConfiguredMultipleEventsWithEqualParameters_ThenCreateOnlyOneSender()
        {
            const string exchangeName = "exchange";

            var senderMock = new Mock<IEventSender>(MockBehavior.Loose);
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSender(exchangeName)).Returns(senderMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSender(typeof(TestEvent), exchangeName);
            exchangeConfiguration.ConfigureEventSender(typeof(AnotherTestEvent), exchangeName);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSender;
            bus.SendEvent(new TestEvent());
            bus.SendEvent(new AnotherTestEvent());

            factoryMock.Verify(m => m.CreateSender(exchangeName), Times.Once);
        }

        [Fact]
        public void TestSource_WhenConfiguredMultipleEventsWithEqualParameters_ThenCreateOnlyOneSource()
        {
            const string exchangeName = "exchange";
            const string exchangeType = "type";
            const string routingKey = "route";

            var sourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            sourceMock.Setup(m => m.ReceiveEvents<TestEvent>()).Returns(Observable.Empty<TestEvent>());
            sourceMock.Setup(m => m.ReceiveEvents<AnotherTestEvent>()).Returns(Observable.Empty<AnotherTestEvent>());
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSource(exchangeName, exchangeType, routingKey)).Returns(sourceMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSource(typeof(TestEvent), exchangeName, exchangeType, routingKey);
            exchangeConfiguration.ConfigureEventSource(typeof(AnotherTestEvent), exchangeName, exchangeType, routingKey);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSource;
            bus.ReceiveEvents<TestEvent>();
            bus.ReceiveEvents<AnotherTestEvent>();

            factoryMock.Verify(m => m.CreateSource(exchangeName, exchangeType, routingKey), Times.Once);
        }

        [Fact]
        public void TestSender_WhenConfiguredMultipleEvents_ThenChooseWisely()
        {
            const string exchangeName = "exchange";
            const string anotherExchangeName = "another-exchange";

            var @event = new TestEvent();
            var anotherEvent = new AnotherTestEvent();
            var senderMock = new Mock<IEventSender>(MockBehavior.Strict);
            senderMock.Setup(m => m.SendEvent(@event));
            var anotherSenderMock = new Mock<IEventSender>(MockBehavior.Strict);
            anotherSenderMock.Setup(m => m.SendEvent(anotherEvent));
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSender(exchangeName)).Returns(senderMock.Object);
            factoryMock.Setup(m => m.CreateSender(anotherExchangeName)).Returns(anotherSenderMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSender(typeof(TestEvent), exchangeName);
            exchangeConfiguration.ConfigureEventSender(typeof(AnotherTestEvent), anotherExchangeName);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSender;
            bus.SendEvent(@event);
            bus.SendEvent(anotherEvent);

            senderMock.Verify(m => m.SendEvent(@event), Times.Once);
            anotherSenderMock.Verify(m => m.SendEvent(@event), Times.Never);
            senderMock.Verify(m => m.SendEvent(anotherEvent), Times.Never);
            anotherSenderMock.Verify(m => m.SendEvent(anotherEvent), Times.Once);
        }
        [Fact]
        public void TestSource_WhenConfiguredMultipleEvents_ThenChooseWisely()
        {
            const string exchangeName = "exchange";
            const string exchangeType = "type";
            const string routingKey = "route";
            const string anotherExchangeName = "another-exchange";
            const string anotherExchangeType = "another-type";
            const string anotherRoutingKey = "another-route";

            var @event = new TestEvent();
            var anotherEvent = new AnotherTestEvent();
            var sourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            sourceMock.Setup(m => m.ReceiveEvents<TestEvent>()).Returns(Observable.Return(@event));
            var anotherSourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            anotherSourceMock.Setup(m => m.ReceiveEvents<AnotherTestEvent>()).Returns(Observable.Return(anotherEvent));
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSource(exchangeName, exchangeType, routingKey)).Returns(sourceMock.Object);
            factoryMock.Setup(m => m.CreateSource(anotherExchangeName, anotherExchangeType, anotherRoutingKey)).Returns(anotherSourceMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSource(typeof(TestEvent), exchangeName, exchangeType, routingKey);
            exchangeConfiguration.ConfigureEventSource(typeof(AnotherTestEvent), anotherExchangeName, anotherExchangeType, anotherRoutingKey);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSource;
            var receivedEvent = bus.ReceiveEvents<TestEvent>().Wait();
            var receivedAnotherEvent = bus.ReceiveEvents<AnotherTestEvent>().Wait();

            Assert.Equal(receivedEvent, @event);
            Assert.Equal(receivedAnotherEvent, anotherEvent);
            sourceMock.Verify(m => m.ReceiveEvents<TestEvent>(), Times.Once);
            sourceMock.Verify(m => m.ReceiveEvents<AnotherTestEvent>(), Times.Never);
            anotherSourceMock.Verify(m => m.ReceiveEvents<TestEvent>(), Times.Never);
            anotherSourceMock.Verify(m => m.ReceiveEvents<AnotherTestEvent>(), Times.Once);
        }

        [Fact]
        public void TestSender_WhenConfiguredForBaseEvent_ThenSendDerived()
        {
            const string exchangeName = "exchange";

            var derivedTestEvent = new DerivedTestEvent();
            var senderMock = new Mock<IEventSender>(MockBehavior.Strict);
            senderMock.Setup(m => m.SendEvent(derivedTestEvent));
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSender(exchangeName)).Returns(senderMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSender(typeof(TestEvent), exchangeName);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSender;
            bus.SendEvent(derivedTestEvent);

            senderMock.Verify(m => m.SendEvent(derivedTestEvent), Times.Once);
        }

        [Fact]
        public void TestSender_WhenDifferentConfigsForBaseAndDerived_ThenSendToBoth()
        {
            const string exchangeName = "exchange";
            const string derivedExchangeName = "derived-exchange";

            var derivedTestEvent = new DerivedTestEvent();
            var senderMock = new Mock<IEventSender>(MockBehavior.Strict);
            senderMock.Setup(m => m.SendEvent(derivedTestEvent));
            var derivedSenderMock = new Mock<IEventSender>(MockBehavior.Strict);
            derivedSenderMock.Setup(m => m.SendEvent(derivedTestEvent));
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSender(exchangeName)).Returns(senderMock.Object);
            factoryMock.Setup(m => m.CreateSender(derivedExchangeName)).Returns(derivedSenderMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSender(typeof(TestEvent), exchangeName);
            exchangeConfiguration.ConfigureEventSender(typeof(DerivedTestEvent), derivedExchangeName);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSender;
            bus.SendEvent(derivedTestEvent);

            senderMock.Verify(m => m.SendEvent(derivedTestEvent), Times.Once);
            derivedSenderMock.Verify(m => m.SendEvent(derivedTestEvent), Times.Once);
        }

        [Fact]
        public void TestSender_WhenConfiguredDerivedEvent_ThenErrorWhileSendingBase()
        {
            const string derivedExchangeName = "derived-exchange";

            var @event = new TestEvent();
            var derivedSenderMock = new Mock<IEventSender>(MockBehavior.Strict);
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSender(derivedExchangeName)).Returns(derivedSenderMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSender(typeof(DerivedTestEvent), derivedExchangeName);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSender;
            Assert.Throws<InvalidOperationException>(() => bus.SendEvent(@event));
        }

        [Fact]
        public void TestSource_WhenConfiguredForBaseEvent_ThenReceiveDerived()
        {
            const string exchangeName = "exchange";
            const string exchangeType = "type";
            const string routingKey = "route";

            var derivedTestEvent = new DerivedTestEvent();
            var sourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            sourceMock.Setup(m => m.ReceiveEvents<DerivedTestEvent>()).Returns(Observable.Return(derivedTestEvent));
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSource(exchangeName, exchangeType, routingKey)).Returns(sourceMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSource(typeof(TestEvent), exchangeName, exchangeType, routingKey);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSource;
            var receivedEvent = bus.ReceiveEvents<DerivedTestEvent>().Wait();

            Assert.Equal(derivedTestEvent, receivedEvent);
            sourceMock.Verify(m => m.ReceiveEvents<DerivedTestEvent>(), Times.Once);
        }

        [Fact]
        public void TestSource_WhenDifferentConfigsForBaseAndDerived_ThenReceiveFromBoth()
        {
            const string exchangeName = "exchange";
            const string exchangeType = "type";
            const string routingKey = "route";
            const string derivedExchangeName = "derived-exchange";
            const string derivedExchangeType = "derived-type";
            const string derivedRoutingKey = "derived-route";

            var derivedTestEvent = new DerivedTestEvent();
            var derivedTestEvent2 = new DerivedTestEvent();
            var sourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            sourceMock.Setup(m => m.ReceiveEvents<DerivedTestEvent>()).Returns(Observable.Return(derivedTestEvent));
            var derivedSourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            derivedSourceMock.Setup(m => m.ReceiveEvents<DerivedTestEvent>()).Returns(Observable.Return(derivedTestEvent2));
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSource(exchangeName, exchangeType, routingKey)).Returns(sourceMock.Object);
            factoryMock.Setup(m => m.CreateSource(derivedExchangeName, derivedExchangeType, derivedRoutingKey)).Returns(derivedSourceMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSource(typeof(TestEvent), exchangeName, exchangeType, routingKey);
            exchangeConfiguration.ConfigureEventSource(typeof(DerivedTestEvent), derivedExchangeName, derivedExchangeType, derivedRoutingKey);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSource;
            var receivedEvents = bus.ReceiveEvents<DerivedTestEvent>().ToArray().Wait();

            Assert.Equal(2, receivedEvents.Length);
            Assert.Contains(derivedTestEvent, receivedEvents);
            Assert.Contains(derivedTestEvent2, receivedEvents);
            sourceMock.Verify(m => m.ReceiveEvents<DerivedTestEvent>(), Times.Once);
            derivedSourceMock.Verify(m => m.ReceiveEvents<DerivedTestEvent>(), Times.Once);
        }

        [Fact]
        public void TestSource_WhenConfiguredDerivedEvent_ThenErrorWhileReceivingBase()
        {
            const string exchangeName = "exchange";
            const string exchangeType = "type";
            const string routingKey = "route";

            var sourceMock = new Mock<IEventSource>(MockBehavior.Strict);
            var factoryMock = new Mock<IEventProcessorFactory>(MockBehavior.Strict);
            factoryMock.Setup(m => m.CreateSource(exchangeName, exchangeType, routingKey)).Returns(sourceMock.Object);
            var exchangeConfiguration = new ExchangeConfiguration();
            exchangeConfiguration.ConfigureEventSource(typeof(DerivedTestEvent), exchangeName, exchangeType, routingKey);

            var bus = new Bus(factoryMock.Object, exchangeConfiguration) as IEventSource;
            Assert.Throws<InvalidOperationException>(() => bus.ReceiveEvents<TestEvent>().Wait());
        }
    }
}
