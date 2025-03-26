using Blazor.LoveJS.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;

namespace Blazor.LoveJS;

public class Script<TComponent> : ComponentBase, IAsyncDisposable
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public ElementReference? HostRef { get; set; }

    /// <summary>
    /// Use a global bundle for the script. If false the script will be generated depending on the type of <typeparamref name="TComponent"/>.
    /// </summary>
    [Parameter] public bool GlobalBundle { get; set; } = false;
    /// <summary>
    /// The name of the bundle to use.
    /// 
    /// If <see cref="GlobalBundle"/> is set to true, the bundle will be loaded from the path <c>"./_content/{typeof(TComponent).Assembly.GetName().Name}/{BundleName}"</c>.
    /// e.g. <c>"./_content/Blazor.LoveJS/index.g.js"</c>
    /// 
    /// If <see cref="GlobalBundle"/> is set to false, the bundle will be loaded from the path <c>"./_content/{typeof(TComponent).Assembly.GetName().Name}/{typeof(TComponent).Namespace}.{typeof(TComponent).Name}.{BundleName}.js"</c>.
    /// </summary>
    [Parameter] public string? BundleName { get; set; } = "index";

    /// <summary>
    /// The path to the script file to load.
    /// 
    /// If provided, the script will be loaded from the path specified. If not provided, the script will be loaded depending on the <see cref="GlobalBundle"/> and <see cref="BundleName"/> parameters.
    /// </summary>
    //[Parameter] public string? ScriptFile { get; set; } = null;

    /// <summary>
    /// Wrap the script in a class with the same name as the component.
    /// </summary>
    [Parameter] public bool AsClass { get; set; } = true;

    /// <summary>
    /// Only if <see cref="AsClass"/> is set to true. 
    /// <br/><br/>
    /// Name of generated class. By default the class will be named namespace + component name. (all dots replaced with underscores), e.g. <c>MyApp_ComponentWithScript</c>.
    /// <br/><br/>
    /// <example>For example:
    /// <code>
    /// static function run(message) 
    /// {
    ///     return prompt(message, 'Type your name here');
    /// }
    /// </code>
    /// result:
    /// <code>
    /// class ClassName 
    /// {
    ///     static function(message)
    ///     {        
    ///         return prompt(message, 'Type your name here');
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    [Parameter] public string? ClassName { get; set; }

    ///// <summary>
    ///// Only if <see cref="AsClass"/> is set to true. 
    ///// <br/><br/>
    ///// Determines if the class should be exported.
    ///// </summary>
    //[Parameter] public bool Export { get; set; } = false;

    /// <summary>
    /// Only if <see cref="AsClass"/> is set to true. 
    /// <br/><br/>
    /// Add the script to the global scope.
    /// <br/><br/>
    /// <example>For example:
    /// <code>
    /// static function run(message) 
    /// {
    ///     return prompt(message, 'Type your name here');
    /// }
    /// </code>
    /// result:
    /// <code>
    /// class ComponentName 
    /// {
    ///     static function(message)
    ///     {        
    ///         return prompt(message, 'Type your name here');
    ///     }
    /// }
    ///
    /// window.ComponentName = ComponentName;
    /// </code>
    /// </example>
    /// <remarks>
    /// JS Function should be static.
    /// </remarks>
    /// </summary>
    [Parameter] public bool AddToGlobal { get; set; } = false;

    /// <summary>
    /// Only if <see cref="AsClass"/> and <see cref="AddToGlobal"/> are set to true. 
    /// <br/><br/>
    /// Add the script to the global scope.
    /// <br/><br/>
    /// <example>For example:
    /// <code>
    /// function run(message) 
    /// {
    ///     return prompt(message, 'Type your name here');
    /// }
    /// </code>
    /// result:
    /// <code>
    /// class ComponentName 
    /// {
    ///     function(message)
    ///     {        
    ///         return prompt(message, 'Type your name here');
    ///     }
    /// }
    ///
    /// window.ComponentName = new ComponentName();
    /// </code>
    /// </example>
    /// </summary>
    [Parameter] public bool AddAsInstance { get; set; } = false;


    [Parameter] public string? OnInit { get; set; }

    // Injects:
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // State:
    private static readonly Dictionary<string, IJSObjectReference> s_globalBundles = [];

    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    private RenderHandle _renderHandle;
    private bool _waitingForFirstRender = true;
    private bool _isInitialized;

    private string? _jsClassName;
    private string _bundleName = null!;

    // Properties
    public string ScriptFile { get; private set; } = null!;

    public Script()
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(LoadModuleAsync);
    }

    // Methods:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string UnboxFunctionName(string identifier) => !AsClass || _jsClassName is null ? identifier : $"{_jsClassName}.{identifier}";

    public async ValueTask InvokeVoidAsync(string identifier, params object[] args)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync(UnboxFunctionName(identifier), args);
    }

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, params object[] args)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<TValue>(UnboxFunctionName(identifier), args);
    }

    // Copy from Component base
    //void IComponent.Attach(RenderHandle renderHandle)
    //{
    //    if (_renderHandle.IsInitialized)
    //        throw new InvalidOperationException("The render handle is already set. Cannot initialize a Script more than once.");

    //    _renderHandle = renderHandle;
    //}
    override protected void OnInitialized()
    {
        _jsClassName = AsClass ? ClassName ?? JSUtils.GetJsClassName(typeof(TComponent).Namespace, typeof(TComponent).Name) : null;
        _bundleName = FilesUtils.GetJsFilename(GlobalBundle, BundleName, $"{typeof(TComponent).Namespace}.{typeof(TComponent).Name}");// GlobalBundle ? (BundleName ?? Consts.GLOBAL_INDEX) : $"{typeof(TComponent).Namespace}.{typeof(TComponent).Name}.{BundleName})";

        ScriptFile ??= $"./_content/{typeof(TComponent).Assembly.GetName().Name}/{Consts.JS_OUTPUT}/{_bundleName}.g.js";
    }

    //Task IComponent.SetParametersAsync(ParameterView parameters)
    //{
    //    // TODO: Implement SetParametersAsync
    //    parameters.SetParameterProperties(this);

    //    //if (_isInitialized)
    //    //    throw new InvalidOperationException("The Script component has already been initialized - cannot change parameters after first init.");
    //    //else
    //    //{
    //    //    _isInitialized = true;

    //    //    //var content = ChildContent?.RenderAsSimpleString();
    //    //}
    //    _jsClassName = AsClass ? ClassName ?? $"{typeof(TComponent).Namespace}_{typeof(TComponent).Name}".Replace('.', '_') : null;

    //    return Task.CompletedTask;
    //}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            _ = await _moduleTask.Value;
    }

    private async Task<IJSObjectReference> LoadModuleAsync()
    {
        IJSObjectReference module;

        if (GlobalBundle)
        {
            if (!s_globalBundles.TryGetValue(_bundleName, out module))
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
            await module.InvokeVoidAsync(UnboxFunctionName(OnInit), HostRef);        

        return module;
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

