namespace Blazor.LoveJS.Common
{
    public static class JSUtils
    {
        public static string GetJsClassName(string componentNamespace, string componentName)
        {
            return $"{componentNamespace}_{componentName}".Replace('.', '_');
        }
    }
}
