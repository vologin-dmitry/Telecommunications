using System;
using System.Linq;
using System.Threading;

namespace ProxyServer
{
    public class HttpRequest
    {
        public string Method { get; }
        public string Host { get; }
        public int Port { get; }
        
        public HttpRequest(string data)
        {
            if (data != "")
            {
                var lines = data.Split(Environment.NewLine);

                var firstLine = data.Split(' ');
                Method = firstLine[0];
                var hostLine = Array.Find(lines,
                    str => str.ToLower().StartsWith("host:"));
                var hostLineArgs = hostLine?.Replace(" ", "")?.Split(':');
                Host = hostLineArgs?[1];
                Port = hostLineArgs is {Length: 3} ? Port = int.Parse(hostLineArgs[2]) : 80;
            }
        }
    }
}