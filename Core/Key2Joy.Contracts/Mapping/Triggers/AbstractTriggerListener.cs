using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Key2Joy.Contracts.Mapping.Triggers;

public abstract class AbstractTriggerListener : MarshalByRefObject
{
    /// <summary>
    /// Set this to true if you want the listener to get WndProc messages.
    /// Sent from the main form.
    /// </summary>
    public virtual bool HasWndProcHandle { get; } = false;

    /// <summary>
    /// WndProc handler for the listener. Only called if <see cref="HasWndProcHandle"/>
    /// </summary>
    /// <param name="m"></param>
    public virtual void WndProc(ref Message m) { }

    /// <summary>
    /// Called when a trigger is about to activate. Listeners can modify which
    /// mapped option candidates will be executed.
    /// </summary>
    public event EventHandler<TriggerActivatingEventArgs> TriggerActivating;

    /// <summary>
    /// Called after the trigger has activated. Listeners can use this to see
    /// which mapped option candidates have been considered.
    /// Note: If the trigger itself chose not to execute by returning false in
    /// <see cref="AbstractTrigger.GetShouldExecute"/> you will not be able to
    /// tell. TODO: Consider registering which triggers actually executed (and
    /// with what results?)
    /// </summary>
    public event EventHandler<TriggerActivatedEventArgs> TriggerActivated;

    /// <summary>
    /// Called when the mappings are armed. The listener should use this to store
    /// the action (preferably in an efficient lookup).
    /// Later when the trigger activates, it can get mapped actions related to this
    /// trigger from this storage/lookup.
    /// </summary>
    /// <param name="mappedOption"></param>
    public abstract void AddMappedOption(AbstractMappedOption mappedOption);

    /// <summary>
    /// Called by other listeners (e.g: a CombinedTriggerListener) to check if
    /// this trigger is triggered at the time of calling.
    /// </summary>
    /// <param name="trigger"></param>
    /// <returns></returns>
    public abstract bool GetIsTriggered(AbstractTrigger trigger);

    /// <summary>
    /// Called when the mappings are armed. The listener should use this to start
    /// it's listening logic.
    /// </summary>
    /// <param name="allListeners"></param>
    public abstract void StartListening(ref IList<AbstractTriggerListener> allListeners);

    /// <summary>
    /// Called when the mappings are disarmed. The listener should use this to stop
    /// it's listening logic.
    /// </summary>
    public abstract void StopListening();

    /// <summary>
    /// Subclasses MUST call this to have their actions executed.
    ///
    /// Even when they know no actions are listening, they should call this. This
    /// lets events provide other mapped options to be injected.
    /// </summary>
    /// <param name="mappedOptions"></param>
    /// <param name="inputBag"></param>
    /// <param name="optionCandidateFilter"></param>
    protected virtual bool DoExecuteTrigger(
        IList<AbstractMappedOption> mappedOptions,
        AbstractInputBag inputBag,
        Func<AbstractTrigger, bool> optionCandidateFilter = null)
    {
        TriggerActivatingEventArgs eventArgs = new(
            this,
            inputBag,
            mappedOptions ?? new List<AbstractMappedOption>(),
            optionCandidateFilter);
        TriggerActivating?.Invoke(this, eventArgs);
        var executedAny = false;

        foreach (var mappedOption in eventArgs.MappedOptionCandidates)
        {
            var shouldExecute = mappedOption.Trigger.GetShouldExecute();

            mappedOption.Trigger.DoActivate(inputBag, shouldExecute);

            if (shouldExecute)
            {
                executedAny = true;
                _ = mappedOption.Action.Execute(inputBag);
            }
        }

        TriggerActivated?.Invoke(this, new TriggerActivatedEventArgs(
            this,
            inputBag,
            eventArgs.MappedOptionCandidates));

        return executedAny;
    }
}
