using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Triggers;

namespace Key2Joy.Mapping.Triggers;

public abstract class CoreTriggerListener : AbstractTriggerListener
{
    protected bool IsActive { get; private set; }

    protected IList<AbstractTriggerListener> allListeners;

    public override void StartListening(ref IList<AbstractTriggerListener> allListeners)
    {
        if (this.IsActive)
        {
            throw new Exception("Shouldn't StartListening to already active listener!");
        }

        this.allListeners = allListeners;

        this.Start();
    }

    public override void StopListening()
    {
        if (!this.IsActive)
        {
            return;
        }

        this.Stop();

        this.allListeners = null;
    }

    protected virtual void Start() => this.IsActive = true;

    protected virtual void Stop() => this.IsActive = false;

    /// <inheritdoc/>
    protected override bool DoExecuteTrigger(
        IList<AbstractMappedOption> mappedOptions,
        AbstractInputBag inputBag,
        Func<AbstractTrigger, bool> optionCandidateFilter = null)
    {
        var executedAny = base.DoExecuteTrigger(mappedOptions, inputBag, optionCandidateFilter);

        if (this is IOverrideDefaultBehavior overridesDefault)
        {
            return overridesDefault.ShouldListenerOverrideDefault(executedAny);
        }

        return false;
    }
}
