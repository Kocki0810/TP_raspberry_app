using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Timers;
using System.IO;

namespace TP_raspberry_app
{
    internal class Program
    {
        static string ip = File.ReadAllText("ip.txt");
        private static async Task<string> PayAPICall(string order, string card_number, string pin)
        {
            order = order.Replace("\\", @"");
            using(HttpClientHandler clientHandler = new HttpClientHandler())
            {
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                    Dictionary<string, string> parameters = new Dictionary<string, string> { { "bon", order }, { "from", card_number }, {"pin", pin } };
                    FormUrlEncodedContent encodedContent = new FormUrlEncodedContent(parameters);
                    var response = client.PostAsync("https://localhost/api/pay.php", encodedContent).ConfigureAwait(false);

                    var msg = await response;
                    
                    return msg.Content.ReadAsStringAsync().Result;
                }
            }
        }
        private static async Task LedAPICall(string pin, string setting)
        {
            using (HttpClientHandler clientHandler = new HttpClientHandler())
            {
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                    var parameters = new Dictionary<string, string> { };
                    var encodedContent = new FormUrlEncodedContent(parameters);
                    var response = client.PostAsync($"http://{ip}/led{pin}{setting}", encodedContent).ConfigureAwait(false);
                }
            }
            
        }
        private static async Task StartLed(string pin)
        {
            Timer t = new Timer();
            LedAPICall(pin, "on");
            t.Start();
            t.Interval = 5000;
            t.Elapsed += TurnOffLed;
        }

        private static void TurnOffLed(object sender, ElapsedEventArgs e)
        {
            LedAPICall("1", "off");
            LedAPICall("2", "off");
            ((Timer)sender).Dispose();//avoid memory leak

        }


        static async Task Main(string[] args)
        {
            bool continueOrder;
            string card_number;
            string pin_code;
            string order;
            string msg;
            while (true)
            {
                order = "{\"reciepts\":[";
                do
                {
                    order += @"{""name"":""" + Vare() + @""",";
                    order += @"""antal"":""" + Antal().ToString() + @""",";
                    order += @"""pris"":""" + Pris().ToString() + @"""},";
                    continueOrder = ContinueOrder();
                }
                while (continueOrder);
                card_number = CardNumber();
                pin_code = PinCode();
                order = order.Remove(order.Length - 1, 1);//remove uneccesarry , at the end of order string
                order += "]}";
                try
                {
                    msg = await PayAPICall(order, card_number, pin_code);
                }
                catch (Exception)
                {
                    Console.WriteLine("Kunne ikke kontake servern");
                    throw;
                }
                Console.Clear();
                if(msg == "Success")
                {
                    StartLed("2");
                    Console.WriteLine(msg);
                }
                else
                {
                    StartLed("1");
                    Console.WriteLine(msg);
                }
                GC.Collect();
                
                Console.ReadLine();
                Console.Clear();

            }
        }

        static public string Vare()
        {
            Console.WriteLine("Indtast den vare du vil købe");
            return Console.ReadLine();
        }
        static public int Antal()
        {
            Console.WriteLine("Indtast antal af vare du vil købe");
            bool retry;
            int antal = 0;
            do
            {
                try
                {
                    retry = false;
                    antal = Int32.Parse(Console.ReadLine());
                }
                catch (Exception)
                {
                    retry = true;
                    Console.WriteLine("Input skal være et tal");
                }
            }
            while (retry);
            return antal;
        }
        static public decimal Pris()
        {
            Console.WriteLine("Indast pris");
            bool retry;
            decimal pris = 0;
            do
            {
                try
                {
                    retry = false;
                    pris = Decimal.Parse(Console.ReadLine());
                }
                catch (Exception)
                {
                    retry = true;
                    Console.WriteLine("Input skal være et tal");
                }
            }
            while (retry);
            return pris;
        }
        static public bool ContinueOrder()
        {
            Console.WriteLine("Vil for fortsætte din ordre. Y for ja, andet input for nej");
            return Console.ReadLine().ToUpper() == "Y";
        }
        static public string CardNumber()
        {
            Console.WriteLine("Swipe dit kort igennem kortlæseren");
            string number = Console.ReadLine();
            return number.Substring(1, number.Length - 2);
        }
        static public string PinCode()
        {
            Console.WriteLine("Skriv din pin kode:");
            bool retry = true;
            string pin = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if(key.Key == ConsoleKey.Enter)
                {
                    retry = false;
                }
                else
                {
                    pin += key.KeyChar;
                    Console.Write("*");
                }
            }
            while (retry);
            return pin;
        }

    }
}
