using Key2Joy.Interop.Commands;
using Key2Joy.Interop;
using Key2Joy.Mapping;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Key2Joy.Tests.Core.Config;
using Key2Joy.Mapping.Actions;
using Key2Joy.Mapping.Triggers;

namespace Key2Joy.Tests.Core.Interop;

[TestClass]
public class InteropServerTests
{
    private Mock<IKey2JoyManager> managerMock;
    private Mock<ICommandRepository> commandRepositoryMock;
    private InteropServer server;

    [TestInitialize]
    public void SetUp()
    {
        this.managerMock = new Mock<IKey2JoyManager>();
        this.commandRepositoryMock = new Mock<ICommandRepository>();
        this.server = new InteropServer(this.managerMock.Object, this.commandRepositoryMock.Object);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Ctor_NullKey2JoyManager_ThrowsException()
        => new InteropServer(null, this.commandRepositoryMock.Object);

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Ctor_NullCommandRepository_ThrowsException()
        => new InteropServer(this.managerMock.Object, null);

    [TestMethod]
    public void RestartListening_CreatesNewPipeServer()
    {
        this.server.RestartListening();
        // Unfortunately, without further refactoring, we can't directly assert that the pipeServer was created.
        // But we can check if the pipe is disposed when calling StopListening.
        this.server.StopListening();
        // The above should not throw if the pipeServer was correctly initialized.
    }

    [TestMethod]
    public void HandleDisableCommand_DisarmsManagerWhenArmed()
    {
        this.managerMock.Setup(m => m.CallOnUiThread(It.IsAny<Action>()))
            .Callback<Action>(a => a());
        this.managerMock.Setup(m => m.GetIsArmed(null)).Returns(true);
        var command = new DisableCommand();

        this.server.HandleCommand(command);

        this.managerMock.Verify(m => m.DisarmMappings(), Times.Once());
    }

    [TestMethod]
    public void HandleDisableCommand_DoesNotDisarmManagerWhenNotArmed()
    {
        this.managerMock.Setup(m => m.CallOnUiThread(It.IsAny<Action>()))
            .Callback<Action>(a => a());
        this.managerMock.Setup(m => m.GetIsArmed(null)).Returns(false);
        var command = new DisableCommand();

        this.server.HandleCommand(command);

        this.managerMock.Verify(m => m.DisarmMappings(), Times.Never());
    }

    [TestMethod]
    public void HandleEnableCommand_Enables()
    {
        ActionsRepository.Buffer(); // required to load mapping profiles
        TriggersRepository.Buffer(); // required to load mapping profiles

        this.managerMock.Setup(m => m.CallOnUiThread(It.IsAny<Action>()))
            .Callback<Action>(a => a());
        this.managerMock.Setup(m => m.GetIsArmed(null)).Returns(false);

        var mappingProfilePath = MockConfigManager.GetMockMappingProfilePath("default-profile.k2j.json");
        MockConfigManager.CopyStubCurrentDefaultProfile(mappingProfilePath);
        var command = new EnableCommand()
        {
            ProfilePath = mappingProfilePath
        };

        this.server.HandleCommand(command);

        this.managerMock.Verify(m => m.ArmMappings(It.IsAny<MappingProfile>(), true), Times.Once());

        MockConfigManager.RemoveMappingProfiles();
    }
}
