using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

namespace ProxyServer
{
    public static class Server
    {

        public static async Task Run()
        {
            TcpListener listener = null;
            try
            {
                ThreadPool.SetMaxThreads(500, 250);
                ThreadPool.SetMinThreads(250, 125);
                var port = 8080;
                listener = new TcpListener(IPAddress.Any, 8080);
                listener.Start();
                WriteLine("Server is running. Waiting for requests on port " + port);
                while (true)
                {
                    var request = await listener.AcceptSocketAsync();
                    ThreadPool.QueueUserWorkItem(HandleSocket, request);
                }
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
            }
            finally
            {
                listener?.Stop();
            }
        }

        static void HandleSocket(object request)
        {
            var buffer = new byte[8192];
            int bytes;
            
            var socketClient = (Socket) request;
            
            var clientStream = new NetworkStream(socketClient);
            var clientReader = new BinaryReader(clientStream);
            var clientWriter = new BinaryWriter(clientStream);
            
            var builder = new StringBuilder();
            do
            {
                bytes = clientStream.Read(buffer, 0, buffer.Length);
                builder.Append(Encoding.Default.GetString(buffer, 0, bytes));
            } while (clientStream.DataAvailable);
            WriteLine(builder.ToString());
            
            var httpRequest = new HttpRequest(builder.ToString());
            if (httpRequest.Method == "CONNECT")
            {
                TcpClient server;
                try
                {
                    server = new TcpClient(httpRequest.Host, httpRequest.Port);
                } catch (Exception e)
                {
                    WriteLine(e.Message);
                    return;
                }

                var serverStream = server.GetStream();
                var serverReader = new BinaryReader(serverStream);
                var serverWriter = new BinaryWriter(serverStream);

                var okResponse = Encoding.Default.GetBytes("HTTP/1.1 200 OK" 
                                                           + Environment.NewLine 
                                                           + Environment.NewLine);
                clientWriter.Write(okResponse, 0, okResponse.Length);
                clientWriter.Flush();

                while (socketClient.Connected && server.Connected &&
                       serverStream.CanRead && serverStream.CanWrite &&
                       clientStream.CanRead && clientStream.CanWrite)
                {
                    while (clientStream.DataAvailable)
                    {
                        Thread.Sleep(200);
                        bytes = clientReader.Read(buffer, 0, buffer.Length);
                        serverWriter.Write(buffer, 0, bytes);
                        Thread.Sleep(100);
                    }
                    serverWriter.Flush();
                    while (serverStream.DataAvailable)
                    {
                        bytes = serverReader.Read(buffer, 0, buffer.Length);
                        clientWriter.Write(buffer, 0, bytes);
                        Thread.Sleep(100);
                    }
                    clientWriter.Flush();

                }

                socketClient.Close();
                server.Close();
            }
            else
            {
                TcpClient server;
                try
                {
                    server = new TcpClient(httpRequest.Host, httpRequest.Port);

                }
                catch (Exception e)
                {
                    WriteLine(e.Message);
                    return;
                }
                var serverStream  = server.GetStream();
                var serverReader = new BinaryReader(serverStream);
                var serverWriter = new BinaryWriter(serverStream);
                
                serverWriter.Write(buffer, 0, bytes);
                serverWriter.Flush();
                
                bytes = serverReader.Read(buffer, 0, buffer.Length);
                serverWriter.Write(buffer, 0, bytes);

                socketClient.Close();
                server.Close();
            }
        }
    }
}