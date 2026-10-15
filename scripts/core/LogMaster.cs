using Godot;

public static class LogMaster
{
    public static void LogInfo(string message, string sourceClass = "", string sourceFunction = "")
        => GD.Print(GetLogString(message, sourceClass, sourceFunction));
    public static void LogError(string message, string sourceClass = "", string sourceFunction = "")
        => GD.PrintErr(GetLogString(message, sourceClass, sourceFunction));

    private static string GetLogString(string msg, string srcClass, string srcMethod)
    {
        string m1 = $"{(string.IsNullOrWhiteSpace(srcClass) ? "" : $"{srcClass} - ")}";
        string m2 = $"{(string.IsNullOrWhiteSpace(srcMethod) ? "" : $"{srcMethod}: ")}";
        return $"{m1}{m2}{msg}";
    }
}
