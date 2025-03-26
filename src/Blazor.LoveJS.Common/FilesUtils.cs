namespace Blazor.LoveJS.Common
{
    public static class FilesUtils
    {
        public static string GetJsFilename(bool globalBundle, string bundleName, string component)
        {
            return globalBundle ? (bundleName ?? Consts.GLOBAL_INDEX) : $"{component}.{bundleName}";
        }
    }
}
