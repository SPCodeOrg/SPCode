namespace SPCode.Utils;

public static class NamesHelper
{
#if BETA
    public static bool Beta = true;
#else
    public static bool Beta = false;
#endif
    public static string ProgramPublicName => Beta ? $"SPCode Beta (rev.{VersionHelper.GetRevisionNumber()})" : "SPCode";
    public static string PipeServerName => Beta ? "SPCodeBetaNamedPipeServer" : "SPCodeNamedPipeServer";
    public static string MutexName => Beta ? "SPCodeGlobalMutex" : "SPCodeGlobalMutexBeta";
    public static string VersionString => Beta ? VersionHelper.GetAssemblyInformationalVersion() : VersionHelper.GetAssemblyVersion().ToString();
}
