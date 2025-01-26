using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CommonServiceLocator;
using Key2Joy.Config;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.Interop;
using Key2Joy.Interop.Commands;
using Key2Joy.LowLevelInput.SimulatedGamePad;
using Key2Joy.LowLevelInput.XInput;
using Key2Joy.Mapping;
using Key2Joy.Mapping.Actions.Logic;
using Key2Joy.Mapping.Triggers;
using Key2Joy.Mapping.Triggers.GamePad;
using Key2Joy.Mapping.Triggers.Keyboard;
using Key2Joy.Mapping.Triggers.Mouse;
using Key2Joy.Plugins;
using Key2Joy.Util;

namespace Key2Joy;

public delegate bool AppCommandRunner(AppCommand command);

public class Key2JoyManager : IKey2JoyManager
{
    /// <summary>
    /// Directory where plugins are located
    /// </summary>
    public const string PluginsDirectory = "Plugins";

    public event EventHandler<StatusChangedEventArgs> StatusChanged;

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

    private const string READY_MESSAGE = "Key2Joy is ready";
    private static AppCommandRunner commandRunner;
    private MappingProfile armedProfile;
    private List<AbstractTriggerListener> armedListeners;
    private IHaveHandleAndInvoke handleAndInvoker;

    private List<AbstractTriggerListener> wndProcListeners = new List<AbstractTriggerListener>();

    private Key2JoyManager()
    { }

    /// <summary>
    /// Ensures Key2Joy is running and ready to accept commands as long as the main loop does not end.
    /// </summary>
    /// <param name="commandRunner"></param>
    /// <param name="mainLoop"></param>
    /// <param name="configManager">Optionally a custom config manager (probably only useful for unit testing)</param>
    public static void InitSafely(AppCommandRunner commandRunner, Action<PluginSet> mainLoop, IConfigManager configManager = null)
    {
        // Setup dependency injection and services
        var serviceLocator = new DependencyServiceLocator();
        ServiceLocator.SetLocatorProvider(() => serviceLocator);

#pragma warning disable IDE0001 // Simplify Names
        serviceLocator.Register<IConfigManager>(configManager ??= new ConfigManager());
#pragma warning restore IDE0001 // Simplify Names

        var gamePadService = new SimulatedGamePadService();
        serviceLocator.Register<ISimulatedGamePadService>(gamePadService);

        var xInputService = new XInputService();
        serviceLocator.Register<IXInputService>(xInputService);

        var commandRepository = new CommandRepository();
        serviceLocator.Register<ICommandRepository>(commandRepository);

        instance = new Key2JoyManager();
        serviceLocator.Register<IKey2JoyManager>(instance);

        // Load plugins
        var pluginDirectoriesPaths = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        pluginDirectoriesPaths = Path.Combine(pluginDirectoriesPaths, PluginsDirectory);

        PluginSet plugins = new(pluginDirectoriesPaths);
        plugins.LoadAll();
        plugins.RefreshPluginTypes();

        foreach (var loadState in plugins.AllPluginLoadStates.Values)
        {
            if (loadState.LoadState == PluginLoadStates.FailedToLoad)
            {
                System.Windows.MessageBox.Show(
                    $"One of your plugins located at {loadState.AssemblyPath} failed to load. This was the error: " +
                    loadState.LoadErrorMessage,
                    "Failed to load plugin!",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning
                );
            }
        }

        Key2JoyManager.commandRunner = commandRunner;

        var interopServer = new InteropServer(instance, commandRepository);

        try
        {
            interopServer.RestartListening();
            mainLoop(plugins);
        }
        finally
        {
            interopServer.StopListening();
            gamePadService.ShutDown();
        }
    }

    // Run the event on the same thread as the main control/form
    public void CallOnUiThread(Action action) => this.handleAndInvoker.Invoke(action);

    internal static bool RunAppCommand(AppCommand command) => commandRunner != null && commandRunner(command);

    public void SetHandlerWithInvoke(IHaveHandleAndInvoke handleAndInvoker)
    {
        this.handleAndInvoker = handleAndInvoker;

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

    /// <inheritdoc/>
    public void ArmMappings(MappingProfile profile, bool withExplicitTriggerListeners = true)
    {
        this.armedProfile = profile;

        var allListeners = this.armedListeners = new();

        if (withExplicitTriggerListeners)
        {
            allListeners.AddRange([
                // Trigger listeners that should explicitly loaded. This ensures that they're available for scripts
                // even if no mapping option is mapped to be triggered by it.
                // Always add these listeners so scripts can ask them if stuff has happened.
                KeyboardTriggerListener.NewInstance(),
                MouseButtonTriggerListener.NewInstance(),
                MouseMoveTriggerListener.NewInstance(),
                GamePadButtonTriggerListener.NewInstance(),
                GamePadStickTriggerListener.NewInstance(),
                GamePadTriggerTriggerListener.NewInstance(),
            ]);
        }

        var allActions = (IList<AbstractAction>)profile.MappedOptions.Select(m => m.Action).ToList();

        var xInputService = ServiceLocator.Current.GetInstance<IXInputService>();
        // We must recognize physical devices before any simulated ones are added.
        // Otherwise we wont be able to tell the difference.
        xInputService.RecognizePhysicalDevices();
        xInputService.StartPolling();

        try
        {
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

                if (listener.HasWndProcHandle)
                {
                    wndProcListeners.Add(listener);
                }

                mappedOption.Action.OnStartListening(listener, ref allActions);
                listener.AddMappedOption(mappedOption);
            }

            var allListenersForSharing = (IList<AbstractTriggerListener>)allListeners;

            foreach (var listener in allListeners)
            {
                listener.StartListening(ref allListenersForSharing);
            }

            StatusChanged?.Invoke(this, new StatusChangedEventArgs
            {
                IsEnabled = true,
                Profile = this.armedProfile
            });
        }
        catch (MappingArmingFailedException ex)
        {
            this.DisarmMappings();
            throw ex;
        }
    }

    public void CallWndProc(ref Message m)
    {
        foreach (var wndProcListener in wndProcListeners)
        {
            wndProcListener.WndProc(ref m);
        }
    }

    public void DisarmMappings()
    {
        var listeners = this.armedListeners;

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

        wndProcListeners.Clear();

        foreach (var listener in listeners)
        {
            listener.StopListening();
        }

        var xInputService = ServiceLocator.Current.GetInstance<IXInputService>();
        xInputService.StopPolling();

        var gamePadService = ServiceLocator.Current.GetInstance<ISimulatedGamePadService>();
        gamePadService.EnsureAllUnplugged();

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
        var configManager = ServiceLocator.Current.GetInstance<IConfigManager>();
        var executablePath = configManager.GetConfigState().LastInstallPath;

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
