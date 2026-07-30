using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace CrudClient
{
    internal class Program
    {
        public static int serverPort = 5000;
        public static IPAddress IPAddress = IPAddress.Loopback;

        static void Main(string[] args)
        {
            Console.Write("Enter authentication code: ");
            string authCode = Console.ReadLine();
            string userChoice;

            while (true)
            {
                Console.WriteLine("\n1- Add Account");
                Console.WriteLine("2- Deposit Amount");
                Console.WriteLine("3- Withdraw Amount");
                Console.WriteLine("4- Retrieve Account");
                Console.WriteLine("5- Delete Account");
                Console.WriteLine("6- Exit");
                Console.Write("Choice: ");
                userChoice = Console.ReadLine();

                if (userChoice == "6")
                {
                    Console.WriteLine("Terminating...");
                    return;
                }

                string request = BuildRequest(userChoice, authCode);
                if (request == null) continue;

                try
                {
                    string response = SendRequest(request);
                    Console.WriteLine($"\n[Server Response] \n{response}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[Connection Error] Make sure the server is running. Details: {ex.Message}");
                }
            }
        }

        public static string SendRequest(string request)
        {
            using (TcpClient client = new TcpClient())
            {
                client.Connect(IPAddress, serverPort);
                using (NetworkStream stream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                using (StreamReader reader = new StreamReader(stream))
                {
                    // Remove newlines so the server can read it as a single line
                    writer.WriteLine(request.Replace("\r", "").Replace("\n", ""));
                    return reader.ReadLine();
                }
            }
        }

        static string BuildRequest(string userChoice, string authCode)
        {
            string operation = "";
            object payLoad = null;

            switch (userChoice)
            {
                case "1":
                    operation = "Add";
                    payLoad = GetAccountDetail();
                    break;
                case "2":
                    operation = "Deposit";
                    Console.Write("Enter Account Number: ");
                    string depAcc = Console.ReadLine();
                    Console.Write("Enter Amount to Deposit: ");
                    decimal depAmount = decimal.Parse(Console.ReadLine());
                    payLoad = new { AccountNumber = depAcc, Amount = depAmount };
                    break;
                case "3":
                    operation = "Withdraw";
                    Console.Write("Enter Account Number: ");
                    string withAcc = Console.ReadLine();
                    Console.Write("Enter Amount to Withdraw: ");
                    decimal withAmount = decimal.Parse(Console.ReadLine());
                    payLoad = new { AccountNumber = withAcc, Amount = withAmount };
                    break;
                case "4":
                    operation = "Retrieve";
                    Console.Write("Enter Account Number: ");
                    payLoad = Console.ReadLine();
                    break;
                case "5":
                    operation = "Delete";
                    Console.Write("Enter Account Number: ");
                    payLoad = Console.ReadLine();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    return null;
            }

            var requestObj = new
            {
                Operation = operation,
                authCode = authCode,
                payLoad = payLoad
            };

            return JsonConvert.SerializeObject(requestObj);
        }

        public static object GetAccountDetail()
        {
            Console.Write("Enter Account Number: ");
            string accNo = Console.ReadLine();

            Console.Write("Enter Account Holder Name: ");
            string name = Console.ReadLine();

            Console.Write("Enter Initial Balance: ");
            decimal balance = decimal.Parse(Console.ReadLine());

            return new
            {
                AccountNumber = accNo,
                AccountHolderName = name,
                Balance = balance
            };
        }
    }
}