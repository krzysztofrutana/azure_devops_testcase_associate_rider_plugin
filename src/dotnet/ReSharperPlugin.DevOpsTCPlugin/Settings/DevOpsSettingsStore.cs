using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;

namespace ReSharperPlugin.DevOpsTCPlugin.Settings;

[ShellComponent]
public class DevOpsSettingsStore
{
    private readonly ISettingsStore _settingsStore;
    private readonly DataContexts _dataContexts;

    public DevOpsSettingsStore(
        ISettingsStore settingsStore,
        DataContexts dataContexts)
    {
        _settingsStore = settingsStore;
        _dataContexts = dataContexts;
    }
    
    public DevOpsSettings GetSettings()
    {
        var boundSettings = BindSettingsStore();
        
        return boundSettings.GetKey<DevOpsSettings>(SettingsOptimization.OptimizeDefault);;
    }

    public void SetSettings(DevOpsSettings settings)
    {
        settings.Tokens = settings.TokensListEvent.ToArray();
        
        var boundSettings = BindSettingsStore();
        boundSettings.SetKey(settings, SettingsOptimization.OptimizeDefault);
    }

    private IContextBoundSettingsStore BindSettingsStore()
    {
        var store = _settingsStore.BindToContextTransient(ContextRange.Smart((l, _) => _dataContexts.CreateOnSelection(l)));
        return store;
    }
}