using StardewModdingAPI;

namespace StardewNavigator
{
    public static class Log
    {
        public static void Debug(string message) => ModEntry.Monitor.Log(message, LogLevel.Debug);
        public static void Info(string message) => ModEntry.Monitor.Log(message, LogLevel.Info);
        public static void Warn(string message) => ModEntry.Monitor.Log(message, LogLevel.Warn);
        public static void Error(string message) => ModEntry.Monitor.Log(message, LogLevel.Error);
        public static void Trace(string message) => ModEntry.Monitor.Log(message, LogLevel.Trace);
    }
}
