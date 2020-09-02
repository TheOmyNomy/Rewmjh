using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Rewmjh
{
    internal class Program
    {
        private static readonly Dictionary<string, MemoryMappedFile> MemoryMappedFiles = new Dictionary<string, MemoryMappedFile>();
        private const int MemoryMappedFileCapacity = 16384;

        private readonly Configuration _configuration;
        private readonly Thread _thread, _threadLiveData;

        private Program()
        {
            Console.Title = "Rewmjh";

            _configuration = Configuration.Load("configuration.ini");

            NetworkStream stream = CreateListener(_configuration.Port);
            NetworkStream streamLiveData = CreateListener(_configuration.PortLiveData);

            _thread = CreateListenerThread(stream);
            _threadLiveData = CreateListenerThread(streamLiveData);
        }

        private void Start()
        {
            _thread.Start();
            _threadLiveData.Start();

            while (_thread.IsAlive && _threadLiveData.IsAlive) { }
        }

        private static NetworkStream CreateListener(int port)
        {
            TcpListener listener = TcpListener.Create(port);
            listener.Start();

            Logger.Log($"Waiting for connection on Port {port}...");

            TcpClient client = listener.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            Logger.Log($"Client connected on Port {port}.");

            return stream;
        }

        private Thread CreateListenerThread(NetworkStream stream)
        {
            return new Thread(() =>
            {
                while (true)
                {
                    List<string> lines = Read(stream);
                    if (lines.Count == 0) continue;

                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        // Console.WriteLine(line);

                        Dictionary<string, string> patterns = JsonConvert.DeserializeObject<Dictionary<string, string>>(line);
                        foreach (KeyValuePair<string, string> pattern in patterns) Send(pattern.Key, pattern.Value);
                    }

                    Thread.Sleep(_configuration.UpdateDelay);
                }
            });
        }

        private static List<string> Read(NetworkStream stream)
        {
            byte[] buffer = new byte[256];

            int count = stream.Read(buffer, 0, buffer.Length);
            if (count == 0) return new List<string>();

            string data = Encoding.ASCII.GetString(buffer, 0, count);
            List<string> lines = new List<string>();

            bool inObject = false, inQuotes = false;
            int lastIndex = 0;

            for (int i = 0; i < data.Length; i++)
            {
                char character = data[i];

                if (character == '{')
                {
                    if (!inObject && !inQuotes)
                    {
                        lastIndex = i;
                        inObject = true;
                    }
                }

                if (character == '}')
                {
                    if (inObject && !inQuotes)
                    {
                        string line = data.Substring(lastIndex, i - lastIndex + 1);
                        lines.Add(line);

                        lastIndex = i;
                        inObject = false;
                    }
                }

                if (character == '"') inQuotes = !inQuotes;
            }

            return lines;
        }

        private static void Send(string name, string value)
        {
            try
            {
                MemoryMappedFile file;

                if (!MemoryMappedFiles.ContainsKey(name))
                {
                    file = MemoryMappedFile.CreateOrOpen(name, MemoryMappedFileCapacity);

                    // Apparently file can be null when it can't ... ?
                    // Not sure, but an error can occur here.
                    MemoryMappedFiles.Add(name, file);
                }
                else file = MemoryMappedFiles[name];

                byte[] buffer = Encoding.Unicode.GetBytes(value);

                using MemoryMappedViewStream stream = file.CreateViewStream();
                stream.Write(buffer, 0, buffer.Length);
                stream.WriteByte(0);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }

        private static void Main() => new Program().Start();
    }
}