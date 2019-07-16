﻿using System.Threading.Tasks;
using homeControl.Domain;
using homeControl.NooliteService.Adapters;
using homeControl.NooliteService.Configuration;
using homeControl.NooliteService.SwitchController;
using Moq;
using ThinkingHome.NooLite;
using ThinkingHome.NooLite.LibUsb;
using Xunit;

namespace homeControl.Tests.Noolite
{
    public class NooliteSwitchControllerTests
    {
        [Fact]
        public void Test_TurnOn_SendsAdapterOnCommand()
        {
            var switchId = SwitchId.NewId();
            var configRepositoryMock = new Mock<INooliteSwitchInfoRepository>(MockBehavior.Strict);
            var adapterMock = new Mock<IPC11XXAdapter>();
            var config = new NooliteSwitchInfo { Channel = 123 };
            configRepositoryMock
                .Setup(repository => repository.ContainsConfig(switchId))
                .Returns(Task.FromResult(true));
            configRepositoryMock
                .Setup(repository => repository.GetConfig(switchId))
                .Returns(Task.FromResult(config));

            var controller = new NooliteSwitchController(configRepositoryMock.Object, adapterMock.Object);
            controller.TurnOn(switchId);

            adapterMock.Verify(adapter => adapter.SendCommand(PC11XXCommand.On, config.Channel, 0), Times.Once);
        }

        [Fact]
        public void Test_TurnOff_SendsAdapterOffCommand()
        {
            var switchId = SwitchId.NewId();
            var configRepositoryMock = new Mock<INooliteSwitchInfoRepository>(MockBehavior.Strict);
            var adapterMock = new Mock<IPC11XXAdapter>();
            var config = new NooliteSwitchInfo { Channel = 98 };
            configRepositoryMock
                .Setup(repository => repository.ContainsConfig(switchId))
                .Returns(Task.FromResult(true));
            configRepositoryMock
                .Setup(repository => repository.GetConfig(switchId))
                .Returns(Task.FromResult(config));

            var controller = new NooliteSwitchController(configRepositoryMock.Object, adapterMock.Object);
            controller.TurnOff(switchId);

            adapterMock.Verify(adapter => adapter.SendCommand(PC11XXCommand.Off, config.Channel, 0), Times.Once);
        }

        [Fact]
        public void Test_IfRepoDoesNotContainConfig_ThenCantHandle()
        {
            var configRepositoryMock = new Mock<INooliteSwitchInfoRepository>(MockBehavior.Strict);
            configRepositoryMock
                .Setup(repository => repository.ContainsConfig(It.IsAny<SwitchId>()))
                .Returns(Task.FromResult(false));

            var controller = new NooliteSwitchController(configRepositoryMock.Object, Mock.Of<IPC11XXAdapter>());
            Assert.False(controller.CanHandleSwitch(SwitchId.NewId()));

            configRepositoryMock.Verify(repo => repo.ContainsConfig(It.IsAny<SwitchId>()), Times.Once);
        }

        [Fact]
        public void Test_IfRepoContainsConfig_ThenCanHandle()
        {
            var configRepositoryMock = new Mock<INooliteSwitchInfoRepository>(MockBehavior.Strict);
            var switchId = SwitchId.NewId();
            configRepositoryMock
                .Setup(repository => repository.ContainsConfig(switchId))
                .Returns(Task.FromResult(true));
            
            var controller = new NooliteSwitchController(configRepositoryMock.Object, Mock.Of<IPC11XXAdapter>());
            Assert.True(controller.CanHandleSwitch(switchId));

            configRepositoryMock.Verify(repo => repo.ContainsConfig(switchId), Times.Once);
        }

        [Theory]
        [InlineData(127, 0, 1.0, 127)]
        [InlineData(127, 0, 0.0, 0)]
        [InlineData(127, 0, 0.5, 64)]
        [InlineData(100, 50, 1.0, 100)]
        [InlineData(100, 50, 0.0, 50)]
        [InlineData(100, 50, 0.2, 60)]
        public void Test_SetPower_ChangesAdapterLevel(byte fullPower, byte zeroPower, double requestedPower, byte expectedLevel)
        {
            var configRepositoryMock = new Mock<INooliteSwitchInfoRepository>(MockBehavior.Strict);
            var config = new NooliteSwitchInfo
            {
                SwitchId = SwitchId.NewId(),
                Channel = 98,
                FullPowerLevel = fullPower,
                ZeroPowerLevel = zeroPower
            };
            configRepositoryMock
                .Setup(repository => repository.ContainsConfig(config.SwitchId))
                .Returns(Task.FromResult(true));
            configRepositoryMock
                .Setup(repository => repository.GetConfig(config.SwitchId))
                .Returns(Task.FromResult(config));
            var adapterMock = new Mock<IPC11XXAdapter>(MockBehavior.Strict);
            adapterMock.Setup(adapter => adapter.SendCommand(PC11XXCommand.SetLevel, config.Channel, expectedLevel));
            var controller = new NooliteSwitchController(configRepositoryMock.Object, adapterMock.Object);

            controller.SetPower(config.SwitchId, requestedPower);

            adapterMock.Verify(adapter => adapter.SendCommand(PC11XXCommand.SetLevel, config.Channel, expectedLevel), Times.Once);
        }
    }
}
