public static class PathExtensions
{
    public static string ToUnixPath(this string path) // DESC :: Windows 경로를 Unix 스타일로 변환
    {
        return path?.Replace('\\', '/');
    }
}