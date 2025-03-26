
namespace Blazor.LoveJS.IntegrationTests;

public class BundledCSharpTests : TestContext
{
    [Fact]
    public void Should_Invoke_RunFunction()
    {
        // Arrange
        JSInterop.SetupModule("./_content/Blazor.LoveJS.IntegrationTests/blazorLoveJS/index.g.js");

        var module = JSInterop.SetupModule("./_content/Blazor.LoveJS.IntegrationTests/blazorLoveJS/Blazor.LoveJS.IntegrationTests.Bundled.index.g.js")
                              .SetupVoid("run", "testMessage");
        
        // Act
        RenderComponent<Bundled>();

        // Assert
        module.VerifyInvoke("run");
    }
}