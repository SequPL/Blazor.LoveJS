
namespace Blazor.LoveJS.IntegrationTests;

public class ComponentCSharpTests : TestContext
{
    [Fact]
    public void Should_Invoke_RunFunction()
    {
        // Arrange
        JSInterop.SetupModule("./_content/Blazor.LoveJS.IntegrationTests/blazorLoveJS/index.g.js");

        var module = JSInterop.SetupModule("./_content/Blazor.LoveJS.IntegrationTests/blazorLoveJS/Blazor.LoveJS.IntegrationTests.Component.index.g.js")
                              .SetupVoid("run", "testMessage");
        
        // Act
        RenderComponent<Component>();

        // Assert
        module.VerifyInvoke("run");
    }
}