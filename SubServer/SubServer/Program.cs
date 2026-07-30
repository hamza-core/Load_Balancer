using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SubServer
{
    public class BankAccount
    {
        public string AccountNumber { get; set; }
        public string AccountHolderName { get; set; }
        public decimal Balance { get; set; }
    }

    internal class Program
    {
        public static ConcurrentDictionary<string, BankAccount> Accounts = new ConcurrentDictionary<string, BankAccount>();
        public static HashSet<string> AuthCodes = new HashSet<string>() { "auth123", "auth456" };

        static void Main(string[] args)
        {
            Console.WriteLine("Enter Port Number:");
            int port = int.Parse(Console.ReadLine());
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Start();
            Console.WriteLine($"SubServer started and listening on port {port}...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Received a task from the Server!");

                Task.Run(() =>
                {
                    using (client)
                    using (NetworkStream stream = client.GetStream())
                    using (StreamReader reader = new StreamReader(stream))
                    using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                    {
                        try
                        {
                            string clientJsonRequest = reader.ReadLine();
                            if (!string.IsNullOrEmpty(clientJsonRequest))
                            {
                                string response = ProcessRequest(clientJsonRequest);
                                writer.WriteLine(response.Replace("\r", "").Replace("\n", ""));
                            }
                        }
                        catch (Exception ex)
                        {
                            writer.WriteLine($"Error: {ex.Message}");
                        }
                    }
                });
            }
        }

        public static string ProcessRequest(string clientJsonRequest)
        {
            var clientRequest = JsonConvert.DeserializeObject<Dictionary<string, object>>(clientJsonRequest);

            string authCode = clientRequest["authCode"].ToString();
            if (!AuthCodes.Contains(authCode))
            {
                return "Unauthorized: Invalid Auth Code";
            }

            string operation = clientRequest["Operation"].ToString().Trim();
            var payLoad = clientRequest["payLoad"];

            BankAccount accountInstance;
            string accNo;

            switch (operation)
            {
                case "Add":
                    accountInstance = JsonConvert.DeserializeObject<BankAccount>(payLoad.ToString());
                    return AddAccount(accountInstance);

                case "Deposit":
                    var depData = JsonConvert.DeserializeObject<dynamic>(payLoad.ToString());
                    return DepositAmount((string)depData.AccountNumber, (decimal)depData.Amount);

                case "Withdraw":
                    var withData = JsonConvert.DeserializeObject<dynamic>(payLoad.ToString());
                    return WithdrawAmount((string)withData.AccountNumber, (decimal)withData.Amount);

                case "Retrieve":
                    accNo = payLoad.ToString();
                    return RetrieveAccount(accNo);

                case "Delete":
                    accNo = payLoad.ToString();
                    return DeleteAccount(accNo);

                default:
                    return "Invalid Operation";
            }
        }

        public static string AddAccount(BankAccount newAccount)
        {
            if (Accounts.ContainsKey(newAccount.AccountNumber))
            {
                return "Account with this Account Number already exists.";
            }
            Accounts.TryAdd(newAccount.AccountNumber, newAccount);
            return "Account added successfully.";
        }

        public static string RetrieveAccount(string id)
        {
            if (!Accounts.ContainsKey(id))
            {
                return "Account not found.";
            }
            return JsonConvert.SerializeObject(Accounts[id], Formatting.None); // Using None for single-line sending
        }

        public static string DepositAmount(string accNo, decimal amount)
        {
            if (!Accounts.TryGetValue(accNo, out BankAccount account)) return "Account not found.";

            // Lock the specific account to prevent race conditions during updates
            lock (account)
            {
                account.Balance += amount;
                return $"Amount deposited successfully. New Balance: {account.Balance}";
            }
        }

        public static string WithdrawAmount(string accNo, decimal amount)
        {
            if (!Accounts.TryGetValue(accNo, out BankAccount account)) return "Account not found.";

            lock (account)
            {
                if (account.Balance < amount) return "Insufficient funds.";

                account.Balance -= amount;
                return $"Amount withdrawn successfully. New Balance: {account.Balance}";
            }
        }

        public static string DeleteAccount(string accNo)
        {
            if (!Accounts.ContainsKey(accNo)) return "Account not found.";

            Accounts.TryRemove(accNo, out _);
            return "Account deleted successfully.";
        }
    }
}