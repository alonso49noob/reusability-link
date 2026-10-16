using UnityEngine;

namespace ReusabilityLink
{
    public static class Log
    {
        const string Prefix = "[ReusabilityLink] ";

        public static void Info(string message) => Debug.Log(Prefix + message);
        public static void Warn(string message) => Debug.LogWarning(Prefix + message);
        public static void Error(string message) => Debug.LogError(Prefix + message);
    }
}
