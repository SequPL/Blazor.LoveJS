using Blazor.LoveJS.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazor.LoveJS;

public class Script : IComponent, IHandleAfterRender, IAsyncDisposable
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Use a global bundle for the script. If false the script will be generated depending on the type of <typeparamref name="TComponent"/>.
    /// </summary>
    [Parameter] public bool GlobalBundle { get; set; } = false;
    /// <summary>
    /// The name of the bundle to use.
    /// 
    /// If <see cref="GlobalBundle"/> is set to true, the bundle will be loaded from the path <c>"./_content/{packageId}/blazorLovejs/{BundleName}"</c>.
    /// e.g. <c>"./_content/{packageId}/blazorLovejs/index.g.js"</c>
    /// 
    /// If <see cref="GlobalBundle"/> is set to false, the bundle will be loaded from the path <c>"./_content/{packageId}/blazorLovejs/{typeof(TComponent).Namespace}.{typeof(TComponent).Name}.{BundleName}.js"</c>.
    /// </summary>
    [Parameter] public string? BundleName { get; set; } = "index";

    /// <summary>
    /// The name of the function to call when the script is loaded.
    /// 
    /// Function have to be exported in the script.
    /// <br />
    /// Example:
    /// <code>
    /// export const run = () => {
    ///     document.getElementById("test").innerText = "init";
    /// }
    /// </code>
    /// </summary>
    [Parameter] public string? OnInit { get; set; }


    /// <summary>
    /// The name of the function to call when the script is unloaded.
    /// 
    /// Function have to be exported in the script.
    /// <br />
    /// Example:
    /// <code>
    /// export const unload = () => {
    ///     document.getElementById("test").innerText = "unloaded";
    /// }
    /// </code>
    /// </summary>
    [Parameter] public string? OnUnload { get; set; }

    /// <summary>
    /// Specifies a custom file path for the script. If not set, the file path will be determined based on the values of <see cref="GlobalBundle"/> and <see cref="BundleName"/>.
    /// <br/><br/>
    /// Note: This value is not used by the Generator.
    /// </summary>
    [Parameter] public string? ScriptFile { get; set; }
    
    // Injects:
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // State:
    private static readonly Dictionary<string, IJSObjectReference> s_globalScripts = [];

    private Lazy<Task<IJSObjectReference>> _moduleTask = null!;

    private RenderHandle _renderHandle;
    private bool _waitingForFirstRender = true;
    private bool _isInitialized;

    // Properties
    public string LoadedScriptFile { get; private set; } = null!;

    // Methods:
    public virtual async ValueTask InvokeVoidAsync(string identifier, params object[] args)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync(identifier, args);
    }

    public virtual async ValueTask<TValue> InvokeAsync<TValue>(string identifier, params object[] args)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<TValue>(identifier, args);
    }

    private async Task<IJSObjectReference> LoadModuleAsync()
    {
        IJSObjectReference module;

        if (GlobalBundle)
        {
            if (!s_globalScripts.TryGetValue(LoadedScriptFile, out module!))
            {
                module = await JS.InvokeAsync<IJSObjectReference>("import", LoadedScriptFile)
                    ?? throw new InvalidOperationException($"Failed to load script {LoadedScriptFile}");

                s_globalScripts.Add(LoadedScriptFile, module);
            }
        }
        else
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", LoadedScriptFile)
                ?? throw new InvalidOperationException($"Failed to load script {LoadedScriptFile}");
        }

        if (OnInit is not null)
            await module.InvokeVoidAsync(OnInit);

        return module;
    }

    // Copy from Component base
    void IComponent.Attach(RenderHandle renderHandle)
    {
        if (_renderHandle.IsInitialized)
            throw new InvalidOperationException("The render handle is already set. Cannot initialize a Script more than once.");

        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        if (!_isInitialized)
        {
            _isInitialized = true;

            foreach (var parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment?)parameter.Value;
                        break;
                    case nameof(GlobalBundle):
                        GlobalBundle = (bool)parameter.Value;
                        break;
                    case nameof(BundleName):
                        BundleName = (string?)parameter.Value;
                        break;
                    case nameof(OnInit):
                        OnInit = (string?)parameter.Value;
                        break;
                    case nameof(ScriptFile):
                        ScriptFile = (string?)parameter.Value;
                        break;
                    case nameof(OnUnload):
                        OnUnload = (string?)parameter.Value;
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown parameter: {parameter.Name}");
                }
            }

            // get or add bundle 
            if (ScriptFile is null)
            {                
                var bundleInfo = ComponentBundleInfoHelper.GetBundleInfo(parameters);
                var bundleName = FilesUtils.GetJsFilename(GlobalBundle, BundleName, bundleInfo.BundleName);

                LoadedScriptFile = bundleInfo.IsFromLib ? $"./_content/{bundleInfo.PackageId}/{Consts.JS_OUTPUT}/{bundleName}.g.js" : $"./{Consts.JS_OUTPUT}/{bundleName}.g.js";
            }
            else
            {
                LoadedScriptFile = ScriptFile;
            }

            // Init
            _moduleTask = new Lazy<Task<IJSObjectReference>>(LoadModuleAsync);
            _renderHandle.Render((_) => { });
        }

        return Task.CompletedTask;
    }

    async Task IHandleAfterRender.OnAfterRenderAsync()
    {
        if (_waitingForFirstRender)
        {
            _waitingForFirstRender = false;
            _ = await _moduleTask.Value;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!GlobalBundle && _moduleTask is not null && _moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;

            if (OnUnload is not null)
                await module.InvokeVoidAsync(OnUnload);

            await module.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
