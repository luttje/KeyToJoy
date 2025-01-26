using System;
using Key2Joy.Mapping;

namespace Key2Joy;

public interface IKey2JoyManager
{
    void CallOnUiThread(Action action);

    /// <summary>
    /// Arms the mapping options so the triggers cause the actions to be executed.
    /// Optionally don't arm explicit trigger listeners (e.g. for testing). Explicit trigger listeners
    /// are always loaded, regardless if they're mapped. That allows scripts to ask them if they know
    /// if input is triggered.
    /// </summary>
    /// <param name="profile"></param>
    /// <param name="withExplicitTriggerListeners"></param>
    /// <exception cref="MappingArmingFailedException">Occurs when an illegal configuration can't be started</exception>
    void ArmMappings(MappingProfile profile, bool withExplicitTriggerListeners = true);

    bool GetIsArmed(MappingProfile profile = null);

    void DisarmMappings();
}
