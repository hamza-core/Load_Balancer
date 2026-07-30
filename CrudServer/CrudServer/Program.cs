using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace CrudServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
            int portNumber = 5000;
            TcpListener tcpListener = new TcpListener(iPAddress, portNumber);
            tcpListener.Start();

            Console.WriteLine("Server started on port 5000...");

            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                Console.WriteLine("Client connected to Server");

                int port = FindServerToSendRequest();
                Task.Run(() => { HandleClient(tcpClient, port); });
            }
        }

        static void HandleClient(TcpClient tcpClient, int portNo)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                var stream = tcpClient.GetStream();

                // Receive message from Client
                string clientJsonRequest = (string)formatter.Deserialize(stream);

                // Proxy acts as a client to the SubServer
                TcpClient subServerClient = new TcpClient();
                subServerClient.Connect("127.0.0.1", portNo);
                NetworkStream subServerStream = subServerClient.GetStream();

                // Forward request to SubServer
                formatter.Serialize(subServerStream, clientJsonRequest);
                subServerStream.Flush();

                // Get response from SubServer
                string subServerResponse = (string)formatter.Deserialize(subServerStream);

                // Send response back to original Client
                formatter.Serialize(stream, subServerResponse);
                stream.Flush();

                subServerClient.Close();
                tcpClient.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }

        static int SubServersRequestCount = 0;

        public static int FindServerToSendRequest()
        {
            int x = SubServersRequestCount++ % 3;

            if (x == 0) return 9000;
            else if (x == 1) return 9001;
            else return 9002;
        }
    }
}