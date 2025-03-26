using Blazor.LoveJS.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazor.LoveJS;

public class Script<TComponent> : IComponent, IHandleAfterRender, IAsyncDisposable
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
    /// export const run = (message) => {
    ///     document.getElementById("test").innerText = message;
    /// }
    /// </code>
    /// </summary>
    [Parameter] public string? OnInit { get; set; }

    /// <summary>
    /// Optional reference to the element, that will be passed to the OnInit fu.
    /// </summary>
    [Parameter] public ElementReference? HostRef { get; set; }

    // Injects:
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // State:
    private static readonly Dictionary<string, IJSObjectReference> s_globalBundles = [];

    private Lazy<Task<IJSObjectReference>> _moduleTask = null!;

    private RenderHandle _renderHandle;
    private bool _waitingForFirstRender = true;
    private bool _isInitialized;

    private string _bundleName = null!;

    // Properties
    public string ScriptFile { get; private set; } = null!;

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
            if (!s_globalBundles.TryGetValue(_bundleName, out module!))
            {
                module = await JS.InvokeAsync<IJSObjectReference>("import", ScriptFile)
                    ?? throw new InvalidOperationException($"Failed to load script {ScriptFile}");

                s_globalBundles.Add(_bundleName, module);
            }
        }
        else
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", ScriptFile)
                ?? throw new InvalidOperationException($"Failed to load script {ScriptFile}");
        }

        if (OnInit is not null)
            await module.InvokeVoidAsync(OnInit, HostRef);

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
        // TODO: Implement SetParametersAsync
        parameters.SetParameterProperties(this);

        if (_isInitialized)
            throw new InvalidOperationException("The Script component has already been initialized - cannot change parameters after first init.");
        else
        {
            _isInitialized = true;

            _moduleTask = new Lazy<Task<IJSObjectReference>>(LoadModuleAsync);
            _bundleName = FilesUtils.GetJsFilename(GlobalBundle, BundleName, $"{typeof(TComponent).Namespace}.{typeof(TComponent).Name}");// GlobalBundle ? (BundleName ?? Consts.GLOBAL_INDEX) : $"{typeof(TComponent).Namespace}.{typeof(TComponent).Name}.{BundleName})";

            ScriptFile ??= $"./_content/{typeof(TComponent).Assembly.GetName().Name}/{Consts.JS_OUTPUT}/{_bundleName}.g.js";

            // Init render
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
        if (!GlobalBundle && _moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
