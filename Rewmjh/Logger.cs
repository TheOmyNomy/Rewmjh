using System;
using System.IO;

namespace Rewmjh
{
    public static class Logger
    {
        private const string LogPath = "log.txt";

        public static void Log(object message)
        {
            string text = $"[{DateTime.Now:HH:mm:ss}] {message}\n";

            Console.Write(text);
            File.AppendAllText(LogPath, text);
        }
    }
}