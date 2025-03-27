# Blazor.LoveJS
Blazor.LoveJS makes working with JavaScript in Blazor easier by allowing you to define JavaScript code directly inside Razor components.

This approach keeps everything in one place, simplifying interactions and making it easier to manage JavaScript modules using a [Script](./src/Blazor.LoveJS/Script.cs) component reference.

> Blazor.LoveJS is particularly useful for small components or quick prototypes, where keeping JavaScript close to the usage improves readability and development speed. 
>
> For larger components, it's still best to store JavaScript in separate files for better maintainability.

## Example Usage

```razor
@using Blazor.LoveJS;

<div id="test" />

<Script @ref="_scriptRef">    
    /* Functions have to be exported */
    export const run = (message) => {
        document.getElementById("test").innerText = message;
    };
</Script>

@code {
    private Script _scriptRef = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await _scriptRef.InvokeVoidAsync("run", "testMessage");
        }
    }
}
```

## Getting Started
Install the following NuGet packages:
- Blazor.LoveJS
- Blazor.LoveJS.Generators

**Important**: Add the following line to your .csproj file:

```xml
<PropertyGroup>
    <!-- Required for LoveJS Generator to work -->
    <UseRazorSourceGenerator>false</UseRazorSourceGenerator>
</PropertyGroup>
```

After compilation, the generator will detect all Script component usages and generate the corresponding JavaScript files to **wwwroot/blazorLoveJS/..** .

## Managing Output Files
You can control the output file names using the **GlobalBundle** and **BundleName** parameters.

> The Script component will automatically determine how to properly import these files (e.g., loading them from _content/ or directly from wwwroot).