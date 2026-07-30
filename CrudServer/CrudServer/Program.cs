using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
                Task.Run(() => { HandleClient(tcpClient); });
            }
        }

        static void HandleClient(TcpClient tcpClient)
        {
            try
            {
                using (tcpClient)
                using (var stream = tcpClient.GetStream())
                using (var clientReader = new StreamReader(stream))
                using (var clientWriter = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Read JSON request from Client
                    string clientJsonRequest = clientReader.ReadLine();
                    if (string.IsNullOrEmpty(clientJsonRequest)) return;

                    // Parse request to find AccountNumber for consistent routing
                    string accNo = ExtractAccountNumber(clientJsonRequest);
                    int portNo = FindServerToSendRequest(accNo);

                    // Forward request to SubServer
                    using (TcpClient subServerClient = new TcpClient("127.0.0.1", portNo))
                    using (NetworkStream subServerStream = subServerClient.GetStream())
                    using (StreamReader subServerReader = new StreamReader(subServerStream))
                    using (StreamWriter subServerWriter = new StreamWriter(subServerStream) { AutoFlush = true })
                    {
                        subServerWriter.WriteLine(clientJsonRequest);

                        // Send response back to original Client
                        string subServerResponse = subServerReader.ReadLine();
                        clientWriter.WriteLine(subServerResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }

        // Consistent routing: ensures the same account always goes to the same server
        public static int FindServerToSendRequest(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber)) return 9000;

            int hash = Math.Abs(accountNumber.GetHashCode());
            int x = hash % 3;

            if (x == 0) return 9000;
            else if (x == 1) return 9001;
            else return 9002;
        }

        private static string ExtractAccountNumber(string jsonString)
        {
            try
            {
                var jObj = JObject.Parse(jsonString);
                var payLoad = jObj["payLoad"];
                if (payLoad == null) return "";

                // If payload is just a string, it's the account number
                if (payLoad.Type == JTokenType.String) return payLoad.ToString();

                // If it's an object, it contains AccountNumber
                return payLoad["AccountNumber"]?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}