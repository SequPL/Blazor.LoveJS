using Blazor.LoveJS.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Blazor.LoveJS.Generators;

[Generator]
public class ScriptSourceGenerator : IIncrementalGenerator
{
    private record Script(string Component, string Content)
    {
        public string Component { get; set; } = Component;
        public string Content { get; set; } = Content;

        public bool AsModule { get; set; }

        public bool GlobalBundle { get; set; }
        public string? BundleName { get; set; }

        public string? ScriptFile { get; set; }

        public bool AsClass { get; set; }
        public string? ClassName { get; set; }
        public bool Export { get; set; }

        public bool AddToGlobal { get; set; }
        public bool AddAsInstance { get; set; }
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<string?> projectDirProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                provider.GlobalOptions.TryGetValue("build_property.projectdir", out string? projectDirectory);
                return projectDirectory;
            });

        var syntaxProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        var compilationAndScripts = projectDirProvider.Combine(context.CompilationProvider.Combine(syntaxProvider.Collect()));

        context.RegisterSourceOutput(compilationAndScripts, static (spc, source) => Execute(source.Right.Left, source.Right.Right, spc, source.Left!));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax method && method.Identifier.Text == "BuildRenderTree";
    }

    private static Script? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // Hacky way to debug the generator
        //#if DEBUG
        //        if (!Debugger.IsAttached)
        //        {
        //            Debugger.Launch();
        //        }
        //#endif

        var m_buildRenderTree = (MethodDeclarationSyntax)context.Node;
        var component = m_buildRenderTree.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (component == null) return null;

        var componentName = component.Identifier.Text;
        var componentNamespace = component.FirstAncestorOrSelf<NamespaceDeclarationSyntax>()?.Name.ToString();

        var descendants = m_buildRenderTree.DescendantNodes().ToList();
        var invocations = descendants.OfType<InvocationExpressionSyntax>().ToList();
        var scriptNodes = invocations.Where(q => q.Expression.ToString().Contains("OpenComponent<global::Blazor.LoveJS.Script<"));

        foreach (var script in scriptNodes)
        {
            var openPosition = invocations.IndexOf(script);
            var closePosition = invocations.FindIndex(openPosition, q => q.Expression.ToString().Contains("CloseComponent"));

            var childContentArg = invocations.Skip(openPosition)
                                             .Take(closePosition - openPosition)
                                             .LastOrDefault(q => q.Expression.ToString().Contains("AddMarkupContent"))
                                            ?.ArgumentList.Arguments[1]
                                             .Expression as LiteralExpressionSyntax;

            if (childContentArg is not null)
            {
                var markupContent = childContentArg.Token.ValueText;// ExtractMarkupContent(block);
                if (!string.IsNullOrWhiteSpace(markupContent))
                {
                    var globalBundle = GetComponentParameter(ref invocations, openPosition, closePosition, "GlobalBundle", false);
                    var bundleName = GetComponentParameter(ref invocations, openPosition, closePosition, "BundleName", Consts.GLOBAL_INDEX);

                    var scriptFile = GetComponentParameter(ref invocations, openPosition, closePosition, "ScriptFile", (string?)null);

                    var asClass = GetComponentParameter(ref invocations, openPosition, closePosition, "AsClass", true);
                    var className = GetComponentParameter(ref invocations, openPosition, closePosition, "ClassName", JSUtils.GetJsClassName(componentNamespace, componentName));
                    var export = true;// GetComponentParameter(ref invocations, openPosition, closePosition, "Export", false);

                    var addToGlobal = GetComponentParameter(ref invocations, openPosition, closePosition, "AddToGlobal", false);
                    var addAsInstance = GetComponentParameter(ref invocations, openPosition, closePosition, "AddAsInstance", false);

                    return new Script($"{componentNamespace}.{componentName}", markupContent!.Trim())
                    {
                        GlobalBundle = globalBundle,
                        BundleName = bundleName,

                        ScriptFile = scriptFile,

                        AsClass = asClass,
                        ClassName = className,
                        Export = export,

                        AddToGlobal = addToGlobal,
                        AddAsInstance = addAsInstance
                    };
                }
            }
        }

        return null;
    }

    private static T GetComponentParameter<T>(ref List<InvocationExpressionSyntax> invocations, int openPosition, int closePosition, string parameterName, T defaultValue)
    {
        var arg = invocations.Skip(openPosition)
                             .Take(closePosition - openPosition)
                             .FirstOrDefault(inv => inv.Expression.ToString().Contains("AddComponentParameter") &&
                                                    inv.ArgumentList.Arguments.Any(a => a.ToString().Contains(parameterName)));

        if (arg == null)
            return defaultValue;

        //__builder.AddComponentParameter(3, "GlobalBundle", global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.Boolean>(
        //#nullable restore
        //#line (5, 71) - (5, 75) "C:\D\Priv\GreyParrot.Reader\submodules\Blazor.LoveJS\tests\Blazor.LoveJS.IntegrationTests\Bundled.razor"
        //true

        //#line default
        //#line hidden
        //#nullable disable
        //));

        var argNode = arg?.ArgumentList.Arguments[2];
        var argStr = argNode?.DescendantNodes().OfType<LiteralExpressionSyntax>().FirstOrDefault()?.Token.ValueText;

        return (T)Convert.ChangeType(argStr, typeof(T));
    }

    private static void Execute(Compilation compilation, ImmutableArray<Script?> scripts, SourceProductionContext context, string projectPath)
    {
        var outputPath = Path.Combine(projectPath, "wwwroot", Consts.JS_OUTPUT);
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        // Hacky way to debug the generator
        //#if DEBUG
        //        if (!Debugger.IsAttached)
        //        {
        //            Debugger.Launch();
        //        }
        //#endif
        var validScripts = scripts.Where(s => s != null).Cast<Script>();
        foreach (var bundle in validScripts.GroupBy(GetBundleName).Where(q => q.Any()))
            GenerateBundleFile(context, bundle, outputPath);
    }

    private static string GetBundleName(Script script) => FilesUtils.GetJsFilename(script.GlobalBundle, script.BundleName, script.Component);
    //{
    //    if (script.GlobalBundle)
    //        return script.BundleName ?? "index";
    //    else
    //        return $"{script.Component}.{script.BundleName}";
    //}

    private static void GenerateBundleFile(SourceProductionContext context, IGrouping<string, Script> bundle, string outputPath)
    {
        var sb = new StringBuilder();
        foreach (var script in bundle)
            sb.AppendLine(script.Content);

        var content = sb.ToString();
        if (!string.IsNullOrWhiteSpace(content))
        {
            sb.Insert(0, $"// Auto-generated bundle bundle: {bundle.Key}\n");

            //context.AddSource("index.blazor.loveJS.js", SourceText.From(sb.ToString(), Encoding.UTF8));
            File.WriteAllText(Path.Combine(outputPath, $"{bundle.Key}.g.js"), content);
        }

    }
}

