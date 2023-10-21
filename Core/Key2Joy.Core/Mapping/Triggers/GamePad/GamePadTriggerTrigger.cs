using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Key2Joy.Contracts.Mapping.Triggers;

namespace Key2Joy.Mapping.Triggers.GamePad;

[Trigger(
    Description = "GamePad Trigger Pull Event"
)]
public class GamePadTriggerTrigger : CoreTrigger, IReturnInputHash, IEquatable<GamePadTriggerTrigger>
{
    public const string PREFIX_UNIQUE = nameof(GamePadTriggerTrigger);

    /// <summary>
    /// Which gamepad index activates this trigger?
    /// </summary>
    public int GamePadIndex { get; set; }

    /// <summary>
    /// Which stick activates this trigger?
    /// </summary>
    public GamePadSide TriggerSide { get; set; }

    /// <summary>
    /// With what margin should the trigger be pulled back to activate?
    /// If null then this trigger will be fired on any move (taking into account the default deadzone).
    /// </summary>
    public float? DeltaMargin { get; set; } = null;

    [JsonConstructor]
    public GamePadTriggerTrigger(string name)
        : base(name)
    { }

    /// <inheritdoc/>
    public override AbstractTriggerListener GetTriggerListener()
        => GamePadTriggerTriggerListener.Instance;

    /// <inheritdoc/>
    public int GetInputHash()
        => this.GetHashCode();

    /// <inheritdoc/>
    public bool Equals(GamePadTriggerTrigger other)
        => other is not null
        && this.TriggerSide == other.TriggerSide
        && EqualityComparer<float?>.Default.Equals(this.DeltaMargin, other.DeltaMargin);

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is not GamePadTriggerTrigger other)
        {
            return false;
        }

        return this.Equals(other);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var axis = this.DeltaMargin != null ? this.DeltaMargin.Value.ToString() : "Any";
        return $"(gamepad) Move #{this.GamePadIndex} {this.TriggerSide} Trigger {axis}";
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = -890627829;
        hashCode = (hashCode * -1521134295) + this.GamePadIndex.GetHashCode();
        hashCode = (hashCode * -1521134295) + this.TriggerSide.GetHashCode();
        hashCode = (hashCode * -1521134295) + this.DeltaMargin.GetHashCode();
        return hashCode;
    }
}
