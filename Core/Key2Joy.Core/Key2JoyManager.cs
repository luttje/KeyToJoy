using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Key2Joy.Config;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.Interop;
using Key2Joy.LowLevelInput;
using Key2Joy.Mapping;
using Key2Joy.Mapping.Actions.Logic;
using Key2Joy.Mapping.Actions.Scripting;
using Key2Joy.Mapping.Triggers.Keyboard;
using Key2Joy.Mapping.Triggers.Mouse;
using Key2Joy.Plugins;
using SimWinInput;

namespace Key2Joy;

public delegate bool AppCommandRunner(AppCommand command);

public class Key2JoyManager : IMessageFilter
{
    private const string READY_MESSAGE = "Key2Joy is ready";
    private static AppCommandRunner commandRunner;
    private MappingProfile armedProfile;
    private Form mainForm;
    private readonly List<IWndProcHandler> wndProcListeners = new();

    public static Key2JoyManager instance;

    public static Key2JoyManager Instance
    {
        get
        {
            if (instance == null)
            {
                throw new Exception("Key2JoyManager not initialized using InitSafely yet!");
            }

            return instance;
        }
    }

    public event EventHandler<StatusChangedEventArgs> StatusChanged;

    private Key2JoyManager()
    { }

    /// <summary>
    /// Ensures Key2Joy is running and ready to accept commands as long as the main loop does not end.
    /// </summary>
    public static void InitSafely(AppCommandRunner commandRunner, Action<PluginSet> mainLoop)
    {
        instance = new Key2JoyManager();

        var pluginDirectoriesPaths = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        pluginDirectoriesPaths = Path.Combine(pluginDirectoriesPaths, "Plugins");

        // TODO: Move this to a seperate method/property that the plugin can override
        BaseScriptAction.ExposedEnumerations.AddRange(new List<Type>
                {
                    typeof(Mouse.MoveType),
                    typeof(Mouse.Buttons),
                    typeof(GamePadControl),
                    typeof(PressState),
                    typeof(Simulator.GamePadStick),
                    typeof(AppCommand),
                    typeof(KeyboardKey)
                }.Select(ExposedEnumeration.FromType).ToList());

        PluginSet plugins = new(pluginDirectoriesPaths);
        plugins.LoadAll();
        plugins.RefreshPluginTypes();

        Key2JoyManager.commandRunner = commandRunner;

        try
        {
            InteropServer.Instance.RestartListening();
            mainLoop(plugins);
        }
        finally
        {
            InteropServer.Instance.StopListening();
            SimGamePad.Instance.ShutDown();
        }
    }

    // Run the event on the same thread as the main form
    internal void CallOnUiThread(Action action) => this.mainForm.Invoke(action);

    private static IList<AbstractTriggerListener> GetScriptingListeners()
    {
        List<AbstractTriggerListener> listeners = new()
        {
            // Always add these listeners so scripts can ask them if stuff has happened.
            KeyboardTriggerListener.Instance,
            MouseButtonTriggerListener.Instance,
            MouseMoveTriggerListener.Instance
        };

        return listeners;
    }

    internal static bool RunAppCommand(AppCommand command) => commandRunner(command);

    public bool PreFilterMessage(ref System.Windows.Forms.Message m)
    {
        for (var i = 0; i < this.wndProcListeners.Count; i++)
        {
            // Check if the proc listeners haven't changed (this can happen when a plugin opens a MessageBox, the user aborts, and we then close the messagebox)
            if (i >= this.wndProcListeners.Count)
            {
                Debug.WriteLine("Key2JoyManager.PreFilterMessage: wndProcListeners changed while processing message!");
                break;
            }

            var wndProcListener = this.wndProcListeners[i];

            wndProcListener.WndProc(new Contracts.Mapping.Message(m.HWnd, m.Msg, m.WParam, m.LParam));
        }

        return false;
    }

    public void SetMainForm(Form form)
    {
        this.mainForm = form;
        Application.AddMessageFilter(this);

        Console.WriteLine(READY_MESSAGE);
    }

    public bool GetIsArmed(MappingProfile profile = null)
    {
        if (profile == null)
        {
            return this.armedProfile != null;
        }

        return this.armedProfile == profile;
    }

    public void ArmMappings(MappingProfile profile)
    {
        this.armedProfile = profile;

        var allListeners = GetScriptingListeners();
        var allActions = (IList<AbstractAction>)profile.MappedOptions.Select(m => m.Action).ToList();

        foreach (var mappedOption in profile.MappedOptions)
        {
            if (mappedOption.Trigger == null)
            {
                continue;
            }

            var listener = mappedOption.Trigger.GetTriggerListener();

            if (!allListeners.Contains(listener))
            {
                allListeners.Add(listener);
            }

            if (listener is IWndProcHandler listenerWndProcHAndler)
            {
                this.wndProcListeners.Add(listenerWndProcHAndler);
            }

            mappedOption.Action.OnStartListening(listener, ref allActions);
            listener.AddMappedOption(mappedOption);
        }

        foreach (var listener in allListeners)
        {
            if (listener is IWndProcHandler listenerWndProcHAndler)
            {
                listenerWndProcHAndler.Handle = this.mainForm.Handle;
            }

            listener.StartListening(ref allListeners);
        }

        StatusChanged?.Invoke(this, new StatusChangedEventArgs
        {
            IsEnabled = true,
            Profile = this.armedProfile
        });
    }

    public void DisarmMappings()
    {
        var listeners = GetScriptingListeners();
        this.wndProcListeners.Clear();

        // Clear all intervals
        IdPool.CancelAll();

        foreach (var mappedOption in this.armedProfile.MappedOptions)
        {
            if (mappedOption.Trigger == null)
            {
                continue;
            }

            var listener = mappedOption.Trigger.GetTriggerListener();
            mappedOption.Action.OnStopListening(listener);

            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        foreach (var listener in listeners)
        {
            listener.StopListening();
        }

        GamePadManager.Instance.EnsureAllUnplugged();
        this.armedProfile = null;

        StatusChanged?.Invoke(this, new StatusChangedEventArgs
        {
            IsEnabled = false,
        });
    }

    /// <summary>
    /// Starts Key2Joy, pausing until it's ready
    /// </summary>
    public static void StartKey2Joy(bool startMinimized = true, bool pauseUntilReady = true)
    {
        var executablePath = ConfigManager.Config.LastInstallPath;

        if (executablePath == null)
        {
            Console.WriteLine("Error! Key2Joy executable path is not known, please start Key2Joy at least once!");
            return;
        }

        if (!File.Exists(executablePath))
        {
            Console.WriteLine("Error! Key2Joy executable path is invalid, please start Key2Joy at least once (and don't move the executable)!");
            return;
        }

        Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = startMinimized ? "--minimized" : "",
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };

        process.Start();

        if (!pauseUntilReady)
        {
            return;
        }

        while (!process.StandardOutput.EndOfStream)
        {
            if (process.StandardOutput.ReadLine() == READY_MESSAGE)
            {
                break;
            }
        }
    }
}
