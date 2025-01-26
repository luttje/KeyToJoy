using System.Collections.Generic;
using System.Threading.Tasks;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.Mapping;
using Key2Joy.Mapping.Actions;
using Key2Joy.Mapping.Triggers;
using Key2Joy.Tests.Core.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Key2Joy.Tests.Core;

public class MockAction : CoreAction
{
    public MockAction(string name) : base(name)
    { }

    public override Task Execute(AbstractInputBag inputBag = null)
        => Task.CompletedTask;
}

public class MockTrigger : CoreTrigger
{
    public MockTrigger(string name) : base(name)
    { }

    public override AbstractTriggerListener GetTriggerListener() => throw new System.NotImplementedException();
}

public class MockTriggerListener : CoreTriggerListener
{
    private bool isTriggered = false;

    public void DoExecuteTriggerForTest(IList<AbstractMappedOption> relevantMappedOptions, MockInputBag inputBag)
    {
        this.isTriggered = true;
        this.DoExecuteTrigger(relevantMappedOptions, inputBag);
        this.isTriggered = false;
    }

    public override void AddMappedOption(AbstractMappedOption mappedOption)
    { }

    public override bool GetIsTriggered(AbstractTrigger trigger)
        => this.isTriggered;
}

public class MockInputBag : AbstractInputBag
{
    public string Key { get; set; }
}

[TestClass]
public class Key2JoyManagerTests
{
    [TestCleanup]
    public void Cleanup() => Key2JoyManager.Instance.DisarmMappings();

    private delegate void SetupTestMocks_SingleTriggerListenerAndAction(
        Mock<MockTriggerListener> listenerMock,
        Mock<MockTrigger> triggerMock,
        Mock<MockAction> actionMock
    );

    private void SetupTestMocks(SetupTestMocks_SingleTriggerListenerAndAction callback)
    {
        var configContents = MockConfigManager.CopyStub("current-config.json", MockConfigManager.GetMockConfigPath());
        var configManager = MockConfigManager.LoadOrCreateMock();

        Key2JoyManager.InitSafely(null, (pluginSet) =>
        {
            var allListenersActivated = new List<AbstractTriggerListener>();
            var allActionsActivated = new List<AbstractAction>();
            var profile = new MappingProfile("testprofile");
            var listener = new Mock<MockTriggerListener>()
            {
                CallBase = true
            };

            Mock<MockAction> action = new("MockActionName");
            Mock<MockTrigger> trigger = new("MockTriggerName");

            trigger.Setup(t => t.GetTriggerListener())
                .Returns(listener.Object);

            //// Override the handler/invoker:
            // Mock<IHaveHandleAndInvoke> invoker = new();
            // Key2JoyManager.Instance.SetHandlerWithInvoke(invoker.Object);

            callback(listener, trigger, action);
        }, configManager);
    }

    [TestMethod]
    public void ArmMappings_Should_AddListenerActivatesMappedOption()
        => this.SetupTestMocks((listener, trigger, action) =>
        {
            var allListenersActivated = new List<AbstractTriggerListener>();
            var allActionsActivated = new List<AbstractAction>();
            var profile = new MappingProfile("testprofile");

            action.Setup(
                    a => a.OnStartListening(
                        It.IsAny<AbstractTriggerListener>(), // listener to start listening
                        ref It.Ref<IList<AbstractAction>>.IsAny // other actions
                    )
                )
                .Callback((AbstractTriggerListener listener, ref IList<AbstractAction> allActions) =>
                {
                    allListenersActivated.Add(listener);
                    allActionsActivated.AddRange(allActions);
                });

            var mappedOption = new MappedOption()
            {
                Action = action.Object,
                Trigger = trigger.Object
            };
            profile.MappedOptions.Add(mappedOption);

            Key2JoyManager.Instance.ArmMappings(profile, false);

            // Assert that the listener hadd the mapped option added
            listener.Verify(l => l.AddMappedOption(mappedOption), Times.Once);

            // Assert that the mapped option action was informed of the listener
            action.Verify(a => a.OnStartListening(listener.Object, ref It.Ref<IList<AbstractAction>>.IsAny), Times.Once);

            // Assert that the listener and action have been sent through OnStartListening
            Assert.IsTrue(allListenersActivated.Contains(listener.Object));
            Assert.IsTrue(allActionsActivated.Contains(action.Object));

            // Assert that there are no more listeners and actions than those added (since we set Explicit Trigger Listeners to an empty list)
            Assert.AreEqual(1, allListenersActivated.Count);
            Assert.AreEqual(1, allActionsActivated.Count);
        });

    [TestMethod]
    public void ArmMappings_Should_CallActionOnTriggerActivated()
        => this.SetupTestMocks((listener, trigger, action) =>
        {
            var triggerWasTriggered = false;
            var profile = new MappingProfile("testprofile");
            var inputBag = new MockInputBag()
            {
                Key = "TestInput"
            };

            trigger.Setup(
                t => t.GetShouldExecute()
            ).Returns(true);

            trigger.Setup(
                t => t.DoActivate(
                    inputBag,
                    true
                )
            );

            var listenerObject = listener.Object;

            action.Setup(
                a => a.Execute(
                    inputBag
                )
            )
            .Callback(() => triggerWasTriggered = listenerObject.GetIsTriggered(trigger.Object));

            var mappedOption = new MappedOption()
            {
                Action = action.Object,
                Trigger = trigger.Object
            };
            profile.MappedOptions.Add(mappedOption);

            Key2JoyManager.Instance.ArmMappings(profile, false);

            listenerObject.DoExecuteTriggerForTest(
                new List<AbstractMappedOption>() { mappedOption },
                inputBag
            );

            // Assert that the trigger was executed
            trigger.Verify(t => t.DoActivate(inputBag, true), Times.Once);

            // Assert that the trigger can be asked through GetIsTriggered if it was triggered
            Assert.IsTrue(triggerWasTriggered);

            // Assert that the action was executed
            action.Verify(a => a.Execute(inputBag), Times.Once);
        });
}
