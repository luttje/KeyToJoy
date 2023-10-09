using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Key2Joy.Config;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.Contracts.Plugins;
using Key2Joy.Mapping;
using Key2Joy.Mapping.Actions;
using Key2Joy.Mapping.Triggers;

namespace Key2Joy.Plugins;

public class PluginSet : IDisposable
{
    private readonly List<PluginBase> loadedPlugins = new();
    public IReadOnlyList<PluginBase> LoadedPlugins => this.loadedPlugins;

    private readonly Dictionary<string, PluginLoadState> pluginLoadStates = new();
    public IReadOnlyDictionary<string, PluginLoadState> AllPluginLoadStates => this.pluginLoadStates;

    private readonly string[] pluginDirectoriesPaths;
    public IReadOnlyList<string> PluginAssemblyPaths => this.pluginDirectoriesPaths;
    private readonly IList<PluginHostProxy> proxiesToDispose = new List<PluginHostProxy>();

    /**
     * Plugin customizations
     */
    private readonly List<MappingTypeFactory<AbstractAction>> actionFactories = new();
    private readonly List<MappingTypeFactory<AbstractTrigger>> triggerFactories = new();
    private readonly List<MappingControlFactory> mappingControlFactories = new();

    public string PluginsFolder { get; private set; }

    /// <summary>
    /// Loads plugins from the specified directory
    /// </summary>
    /// <param name="pluginDirectoriesPaths">The path to the directory containing the plugins</param>
    /// <returns></returns>
    internal PluginSet(string pluginDirectoriesPaths)
    {
        this.PluginsFolder = pluginDirectoriesPaths;

        if (!Directory.Exists(pluginDirectoriesPaths))
        {
            Directory.CreateDirectory(pluginDirectoriesPaths);
        }

        this.pluginDirectoriesPaths = Directory.GetDirectories(pluginDirectoriesPaths);
    }

    internal void LoadAll()
    {
        var enabledPlugins = ConfigManager.Config.EnabledPlugins;

        foreach (var pluginDirectoryPath in this.pluginDirectoriesPaths)
        {
            var pluginAssemblyName = Path.GetFileName(pluginDirectoryPath);
            var pluginAssemblyFileName = $"{pluginAssemblyName}.dll";
            var pluginAssemblyPath = Path.Combine(pluginDirectoryPath, pluginAssemblyFileName);
            var expectedChecksum = ConfigManager.Instance.GetExpectedChecksum(pluginAssemblyPath);

            if (ConfigManager.Instance.IsPluginEnabled(pluginAssemblyPath))
            {
                var plugin = this.LoadPlugin(pluginAssemblyPath, expectedChecksum);
                this.AddPluginState(PluginLoadStates.Loaded, pluginAssemblyPath, null, plugin);
            }
            else
            {
                this.AddPluginState(
                    PluginLoadStates.NotLoaded,
                    pluginAssemblyPath,
                    "Plugin disabled. Enable it if you trust the author."
                );
            }
        }
    }

    public void RefreshPluginTypes()
    {
        ActionsRepository.Buffer(this.actionFactories);
        TriggersRepository.Buffer(this.triggerFactories);
        MappingControlRepository.Buffer(this.mappingControlFactories);
    }

    public PluginHostProxy LoadPlugin(string pluginAssemblyPath, string expectedChecksum = null)
    {
        var pluginDirectoryPath = Path.GetDirectoryName(pluginAssemblyPath);
        var pluginAssemblyName = Path.GetFileName(pluginAssemblyPath).Replace(".dll", "");
        PluginLoadState pluginLoadState = new(pluginAssemblyPath);

        PluginHostProxy pluginHost = new(pluginAssemblyPath, pluginAssemblyName);
        string loadedChecksum;

        try
        {
            pluginHost.LoadPlugin(out loadedChecksum, expectedChecksum);

            this.actionFactories.AddRange(pluginHost.GetActionFactories());
            this.triggerFactories.AddRange(pluginHost.GetTriggerFactories());
            this.mappingControlFactories.AddRange(pluginHost.GetMappingControlFactories());
        }
        catch (PluginLoadException ex)
        {
            pluginHost.Dispose();

            this.AddPluginState(
                PluginLoadStates.FailedToLoad,
                pluginAssemblyPath,
                ex.Message
            );

            return null;
        }

        this.AddPluginState(PluginLoadStates.Loaded, pluginAssemblyPath, null, pluginHost);

        ConfigManager.Instance.SetPluginEnabled(pluginAssemblyPath, loadedChecksum);

        this.proxiesToDispose.Add(pluginHost);

        return pluginHost;
    }

    public void DisablePlugin(string pluginAssemblyPath)
    {
        ConfigManager.Instance.SetPluginEnabled(pluginAssemblyPath, null);
        MessageBox.Show("When disabling loaded plugins you have to restart the application for these changes to take effect.");
    }

    internal void AddPluginState(PluginLoadStates state, string pluginAssemblyPath, string errorMessage, PluginHostProxy loadedPlugin = null)
    {
        if (state == PluginLoadStates.FailedToLoad)
        {
            MessageBox.Show(
                $"One of your plugins located at {pluginAssemblyPath} failed to load. This was the error: " +
                errorMessage,
                "Failed to load plugin!",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        if (this.pluginLoadStates.TryGetValue(pluginAssemblyPath, out var loadState))
        {
            loadState.LoadState = state;
            loadState.SetPluginHost(loadedPlugin);
            loadState.LoadErrorMessage = errorMessage ?? string.Empty;
            return;
        }

        loadState = new PluginLoadState(pluginAssemblyPath)
        {
            LoadState = state,
            LoadErrorMessage = errorMessage
        };
        loadState.SetPluginHost(loadedPlugin);

        this.pluginLoadStates.Add(
            pluginAssemblyPath,
            loadState
        );
    }

    public void Dispose()
    {
        foreach (var keyValuePair in this.pluginLoadStates)
        {
            var pluginLoadState = keyValuePair.Value;
            pluginLoadState.PluginHost?.Dispose();
        }
    }
}
