using Blazor.LoveJS.Common;

namespace Blazor.LoveJS.IntegrationTests;

public class ScriptTests : TestContext
{
    /// <summary>
    /// In tests Script does not have a parent so <see cref="ComponentBundleInfoHelper.GetBundleInfo(ParameterView)"/> will return Bunit.Core assembly as a parent ( in this case parent = renderer ).
    /// </summary>
    private const string SCRIPT_PARENT_PACKAGEID = "Bunit.Core";
    private const string SCRIPT_PARENT_NAMESPACE = "Bunit.Rendering.FragmentContainer";

    [Fact]
    public void Should_Invoke_GlobalBundle_OnInit()
    {
        // Arrange
        var module = JSInterop.SetupModule($"./_content/{SCRIPT_PARENT_PACKAGEID}/{Consts.JS_OUTPUT}/{Consts.GLOBAL_INDEX}.g.js")
                              .SetupVoid("run", _ => true);

        // Act
        RenderComponent<Script>(ps => ps.AddChildContent("export const run = () => { }")
                                        .Add(p => p.GlobalBundle, true)
                                        .Add(p => p.OnInit, "run")
        );

        // Assert
        module.VerifyInvoke("run");
    }

    [Fact]
    public void Should_InvokeOnInit()
    {
        // Arrange
        var module = JSInterop.SetupModule($"./_content/{SCRIPT_PARENT_PACKAGEID}/{Consts.JS_OUTPUT}/{SCRIPT_PARENT_NAMESPACE}.index.g.js")
                              .SetupVoid("run", _ => true);

        // Act
        RenderComponent<Script>(ps => ps.AddChildContent("export const run = () => { }")
                                             .Add(p => p.GlobalBundle, false)
                                             .Add(p => p.OnInit, "run")
        );

        // Assert
        module.VerifyInvoke("run");
    }


    [Fact]
    public void Should_Invoke_GlobalBundle_OnInit_CustomBundle()
    {
        // Arrange
        var module = JSInterop.SetupModule($"./_content/{SCRIPT_PARENT_PACKAGEID}/{Consts.JS_OUTPUT}/test.g.js")
                              .SetupVoid("run", _ => true);

        // Act
        RenderComponent<Script>(ps => ps.AddChildContent("export const run = () => { }")
                                             .Add(p => p.GlobalBundle, true)
                                             .Add(p => p.OnInit, "run")
                                             .Add(p => p.BundleName, "test")
        );

        // Assert
        module.VerifyInvoke("run");
    }

    [Fact]
    public void Should_InvokeOnInit_CustomBundle()
    {
        // Arrange
        var module = JSInterop.SetupModule($"./_content/{SCRIPT_PARENT_PACKAGEID}/{Consts.JS_OUTPUT}/{SCRIPT_PARENT_NAMESPACE}.test.g.js")
                              .SetupVoid("run", _ => true);

        // Act
        RenderComponent<Script>(ps => ps.AddChildContent("export const run = () => { }")
                                             .Add(p => p.GlobalBundle, false)
                                             .Add(p => p.OnInit, "run")
                                             .Add(p => p.BundleName, "test")
        );

        // Assert
        module.VerifyInvoke("run");
    }

    [Fact]
    public void Should_Handle_ScriptFile()
    {
        const string SCRIPT_FILE = "testFile.js";

        // Arrange
        var module = JSInterop.SetupModule(SCRIPT_FILE)
                              .SetupVoid("run", _ => true);

        // Act
        RenderComponent<Script>(ps => ps.AddChildContent("export const run = () => { }")
                                             .Add(p => p.ScriptFile, SCRIPT_FILE)
                                             .Add(p => p.OnInit, "run")
        );

        // Assert
        module.VerifyInvoke("run");
    }
}
