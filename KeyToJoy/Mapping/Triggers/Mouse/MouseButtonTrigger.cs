﻿using System;
using System.Windows.Forms;
using KeyToJoy.Input.LowLevel;
using Linearstar.Windows.RawInput.Native;
using Newtonsoft.Json;

namespace KeyToJoy.Mapping
{
    [Trigger(
        Name = "Mouse Button Event",
        OptionsUserControl = typeof(MouseButtonTriggerOptionsControl)
    )]
    public class MouseButtonTrigger : BaseTrigger, IEquatable<MouseButtonTrigger>
    {
        public const string PREFIX_UNIQUE = nameof(MouseButtonTrigger);

        [JsonProperty]
        public Mouse.Buttons MouseButtons { get; set; }


        [JsonConstructor]
        public MouseButtonTrigger(string name)
            :base(name)
        { }

        internal override TriggerListener GetTriggerListener()
        {
            return MouseButtonTriggerListener.Instance;
        }

        internal override string GetUniqueKey()
        {
            return $"{PREFIX_UNIQUE}_{MouseButtons}";
        }

        public override bool Equals(object obj)
        {
            if(!(obj is MouseButtonTrigger other)) 
                return false;

            return Equals(other);
        }

        public bool Equals(MouseButtonTrigger other)
        {
            return MouseButtons == other.MouseButtons;
        }

        public override string ToString()
        {
            return $"(mouse) {MouseButtons}";
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}
