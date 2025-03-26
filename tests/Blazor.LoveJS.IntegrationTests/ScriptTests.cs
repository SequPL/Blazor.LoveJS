using Blazor.LoveJS.Common;

namespace Blazor.LoveJS.IntegrationTests;

public class ScriptTests : TestContext
{
    [Fact]
    public void Should_Invoke_GlobalBundle_OnInit()
    {
        // Arrange
        var module = JSInterop.SetupModule($"./_content/Blazor.LoveJS.IntegrationTests/{Consts.JS_OUTPUT}/{Consts.GLOBAL_INDEX}.g.js")
                              .SetupVoid("run", _ => true);

        // Act
        RenderComponent<Script<Foo>>(ps => ps.AddChildContent("export const run = () => { }")
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
        var module = JSInterop.SetupModule($"./_content/Blazor.LoveJS.IntegrationTests/{Consts.JS_OUTPUT}/Blazor.LoveJS.IntegrationTests.Foo.index.g.js")
                              .SetupVoid("run", _ => true);

        // Act
        RenderComponent<Script<Foo>>(ps => ps.AddChildContent("export const run = () => { }")
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
        var module = JSInterop.SetupModule($"./_content/Blazor.LoveJS.IntegrationTests/{Consts.JS_OUTPUT}/test.g.js")
                              .SetupVoid("run", _ => true);

        // Act
        RenderComponent<Script<Foo>>(ps => ps.AddChildContent("export const run = () => { }")
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
        var module = JSInterop.SetupModule($"./_content/Blazor.LoveJS.IntegrationTests/{Consts.JS_OUTPUT}/Blazor.LoveJS.IntegrationTests.Foo.test.g.js")
                              .SetupVoid("run", _ => true);

        // Act
        RenderComponent<Script<Foo>>(ps => ps.AddChildContent("export const run = () => { }")
                                             .Add(p => p.GlobalBundle, false)
                                             .Add(p => p.OnInit, "run")
                                             .Add(p => p.BundleName, "test")
        );

        // Assert
        module.VerifyInvoke("run");
    }
}
