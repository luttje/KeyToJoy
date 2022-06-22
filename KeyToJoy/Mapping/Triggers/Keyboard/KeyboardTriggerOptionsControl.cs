﻿using KeyToJoy.Input;
using Linearstar.Windows.RawInput;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Linearstar.Windows.RawInput.Native;
using KeyToJoy.Mapping;

namespace KeyToJoy
{
    public partial class KeyboardTriggerOptionsControl : UserControl, ISetupTrigger
    {
        private Keys keys;

        public KeyboardTriggerOptionsControl()
        {
            InitializeComponent();

            RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, Handle);

            // Relieve input capturing by this mapping form and return it to the main form
            this.Disposed += (s,e) => RawInputDevice.UnregisterDevice(HidUsageAndPage.Keyboard);
        }

        public void Setup(BaseTrigger trigger)
        {
            var thisTrigger = (KeyboardTrigger) trigger;

            thisTrigger.Keys = this.keys;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_INPUT = 0x00FF;

            if (m.Msg == WM_INPUT)
            {
                var data = RawInputData.FromHandle(m.LParam);

                if (data is RawInputKeyboardData keyboard)
                {
                    keys = VirtualKeyConverter.KeysFromVirtual(keyboard.Keyboard.VirutalKey);
                    var flags = keyboard.Keyboard.Flags;

                    if ((flags & RawKeyboardFlags.KeyE0) == RawKeyboardFlags.KeyE0)
                    {
                        if (keys == Keys.ControlKey)
                            keys = Keys.RControlKey;
                        if (keys == Keys.ShiftKey)
                            keys = Keys.RShiftKey;
                    }
                    else
                    {
                        if (keys == Keys.ControlKey)
                            keys = Keys.LControlKey;
                        if (keys == Keys.ShiftKey)
                            keys = Keys.LShiftKey;
                    }
                    
                    txtKeyBind.Text = keys.ToString();
                }
            }

            base.WndProc(ref m);
        }
    }
}
