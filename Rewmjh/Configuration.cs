using System;
using System.IO;

namespace Rewmjh
{
    public class Configuration
    {
        public int Port { get; private set; } = 7839;
        public int PortLiveData { get; private set; } = 7840;
        public int UpdateDelay { get; private set; }

        private int _updateRate = 10;

        public static Configuration Load(string path)
        {
            Configuration configuration = new Configuration();

            if (!File.Exists(path))
            {
                Logger.Log($"The configuration file \"{path}\" does not exist!");

                string contents = $"Port = {configuration.Port}\n" +
                                  $"PortLiveData = {configuration.PortLiveData}\n" +
                                  $"UpdateRate = {configuration._updateRate}";

                File.WriteAllText(path, contents);

                Logger.Log($"Generated a new configuration file \"{path}\"...");
            }

            string[] lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                string[] tokens = line.Split('=');
                string name = tokens[0].Trim(), value = tokens[1].Trim();

                if (name.Equals("Port", StringComparison.OrdinalIgnoreCase)) configuration.Port = int.Parse(value);
                if (name.Equals("PortLiveData", StringComparison.OrdinalIgnoreCase)) configuration.PortLiveData = int.Parse(value);
                if (name.Equals("UpdateRate", StringComparison.OrdinalIgnoreCase)) configuration._updateRate = int.Parse(value);
            }


            // Minimum: 1 update per second, Maximum: 100 updates per second.
            configuration.UpdateDelay = Math.Clamp(1000 / configuration._updateRate, 10, 1000);

            Logger.Log($"Loaded the configuration file \"{path}\".");

            return configuration;
        }
    }
}