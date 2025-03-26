﻿#define UNSAFE_ACCESSORS
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using System.Runtime.CompilerServices;

namespace Blazor.LoveJS;

public readonly record struct ComponentBundleInfo(string PackageId, string BundleName)
{
    public string PackageId { get; } = PackageId;
    public string BundleName { get; } = BundleName;
}

public static class ComponentBundleInfoHelper
{
    private static readonly Dictionary<Type, ComponentBundleInfo> s_componentBundles = [];

    public static ComponentBundleInfo GetBundleInfo(ParameterView parameters)
    {
        var parentComponentType = GetParentComponentType(parameters)!;
        if (!s_componentBundles.TryGetValue(parentComponentType, out ComponentBundleInfo componentBundle))
        {
            componentBundle = new ComponentBundleInfo(parentComponentType.Assembly!.GetName()!.Name!, $"{parentComponentType.Namespace}.{parentComponentType.Name}");
            s_componentBundles.Add(parentComponentType, componentBundle);
        }

        return componentBundle;
    }
#if UNSAFE_ACCESSORS
    private static Type GetParentComponentType(ParameterView parameters)
    {
        var frames = FramesAccessor(ref parameters);
        int ownerIndex = OwnerIndexAccessor(ref parameters);

        if (frames is null || ownerIndex < 0 || ownerIndex >= frames.Length)
            throw new InvalidOperationException("Failed to get parent component type.");

        var frame = frames[ownerIndex];
        var componentState = ComponentStateAccessor(ref frame) ?? throw new InvalidOperationException("Failed to get parent component type.");

        return componentState.ParentComponentState?.Component?.GetType() ?? throw new InvalidOperationException("Failed to get parent component type.");
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_frames")]
    private extern static ref RenderTreeFrame[]? FramesAccessor(ref ParameterView parameters);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_ownerIndex")]
    private extern static ref int OwnerIndexAccessor(ref ParameterView parameters);

#pragma warning disable BL0006 // Do not use RenderTree types
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "ComponentStateField")]
    private extern static ref ComponentState? ComponentStateAccessor(ref RenderTreeFrame frame);
#pragma warning restore BL0006 // Do not use RenderTree types

#else
    private static Type GetParentComponentType(ParameterView parameters)
    {
        var framesField = typeof(ParameterView).GetField("_frames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var ownerIndexField = typeof(ParameterView).GetField("_ownerIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int ownerIndex = (int)(ownerIndexField?.GetValue(parameters) ?? -1);

        if (framesField?.GetValue(parameters) is not Array frames || ownerIndex < 0 || ownerIndex >= frames.Length)
            throw new InvalidOperationException("Failed to get parent component type.");

        var frame = (RenderTreeFrame)frames.GetValue(ownerIndex)!;
        var componentStateField = frame.GetType().GetField("ComponentStateField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var componentState = (ComponentState?)componentStateField?.GetValue(frame);

        return componentState?.ParentComponentState?.Component.GetType() ?? throw new InvalidOperationException("Failed to get parent component type.");
    }
#endif
}
