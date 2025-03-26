using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using System.Text;

namespace Blazor.LoveJS;

public static class RenderFragmentExtensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0006:Do not use RenderTree types", Justification = "<Pending>")]
    public static string RenderAsSimpleString(this RenderFragment childContent)
    {
        using var builder = new RenderTreeBuilder();
        builder.AddContent(0, childContent);

        var array = builder.GetFrames();
        var sb = new StringBuilder();

        for (int i = 0; i < array.Count; i++)
        {
            ref var frame = ref array.Array[i];

            if (frame.FrameType == RenderTreeFrameType.Text)
                sb.Append(frame.TextContent);

            if (frame.FrameType == RenderTreeFrameType.Markup)
                sb.Append(frame.MarkupContent);

        }
        return sb.ToString();
    }
}