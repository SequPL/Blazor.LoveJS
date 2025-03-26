
namespace Blazor.LoveJS.IntegrationTests;

public class BundledCSharpTests : TestContext
{
    [Fact]
    public void CounterStartsAtZero()
    {
        // Arrange
        JSInterop.SetupModule("./_content/Blazor.LoveJS.IntegrationTests/blazorLoveJS/Blazor.LoveJS.IntegrationTests.Bundled.index.g.js");
        JSInterop.SetupModule("./_content/blazorLoveJS/index.g.js");
        var cut = RenderComponent<Bundled>();// (ps => ps.SetAssignedRenderMode(RenderMode.InteractiveServer));
        cut.SetParametersAndRender();

        cut.Find("div#test").MarkupMatches("<div id=\"test\">testMessage</div>");
    }

    [Fact]
    public void Should_InvokeOnInit()
    {
        // Arrange
        JSInterop.SetupModule("./_content/Blazor.LoveJS.IntegrationTests/blazorLoveJS/index.g.js")
                 .SetupVoid("Blazor_LoveJS_IntegrationTests_Bundled.run", _ => true);

        // Act
        RenderComponent<Script<Bundled>>(ps => ps.AddChildContent("run = () => { }")
                                                 .Add(p => p.GlobalBundle, true)
                                                 .Add(p => p.OnInit, "run")
        );

        // Assert
        JSInterop.VerifyInvoke("Blazor_LoveJS_IntegrationTests_Bundled.run");
    }
}