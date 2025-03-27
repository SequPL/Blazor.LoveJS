# Blazor.LoveJS

```razor
@using Blazor.LoveJS;

<div id="test" />

<Script @ref=_scriptRef>
    export const run = (message) => {
            document.getElementById("test").innerText = message;
        }
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
