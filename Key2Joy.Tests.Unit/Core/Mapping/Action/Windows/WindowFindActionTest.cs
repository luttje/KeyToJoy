﻿using Key2Joy.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;

namespace Key2Joy.Tests.Unit.Core.Mapping.Action.Windows
{
    [TestClass]
    public class WindowFindActionTest
    {
        [TestMethod]
        [DataRow("notepad.exe")]
        [DataRow("cmd.exe")]
        public void Script_Finds_CreatedProcess(string processName)
        {
            var action = new WindowFindAction(string.Empty, string.Empty);
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = processName,
            });

            Thread.Sleep(50);

            var actualHandle = process.MainWindowHandle;
            var handle = action.ExecuteForScript(null, process.MainWindowTitle);
            process.Kill();

            Assert.AreEqual(actualHandle, handle);
        }
    }
}
