using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CavemanTcp;

namespace Test.HttpLoopback
{
    class Program
    {
        static string _Hostname = "127.0.0.1";
        static int _Port = 3002;
        static CavemanTcpServer _Server = null;
        static string _HttpResponse =
            "HTTP/1.1 200 OK\r\n" +
            "Access-Control-Allow-Origin: *\r\n" +
            "Content-Type: text/plain\r\n" +
            "\r\n" +
            "Hello,Https!\r\n\r\n";

        static void Main(string[] args)
        {
            InitializeServer();
            _Server.Start();
            Console.WriteLine("CavemanTcp listening on https://" + _Hostname + ":" + _Port + "/");
            Console.WriteLine("ENTER to exit");
            Console.ReadLine();
        }

        static void InitializeServer()
        {
            _Server = new CavemanTcpServer(_Hostname, _Port, true, "cavemantcp.pfx", "simpletcp");
            _Server.Settings.MonitorClientConnections = false; 
            _Server.Events.ClientConnected += ClientConnected;
        }

        static async void ClientConnected(object sender, ClientConnectedEventArgs args)
        {
            Console.WriteLine("Client " + args.Client.ToString() + " connected to server");
            try
            {
                string data = await ReadFully(args.Client.Guid);
                await _Server.SendAsync(args.Client.Guid, _HttpResponse);
                _Server.DisconnectClient(args.Client.Guid);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static async Task<string> ReadFully(Guid guid)
        {
            StringBuilder sb = new StringBuilder();
             
            ReadResult readInitial = await _Server.ReadAsync(guid, 18);
            if (readInitial.Status != ReadResultStatus.Success)
            { 
                throw new IOException("Unable to read data");
            }
             
            sb.Append(Encoding.ASCII.GetString(readInitial.Data));
            while (true)
            {
                string delimCheck = sb.ToString((sb.Length - 4), 4);
                if (delimCheck.EndsWith("\r\n\r\n"))
                { 
                    break;
                }
                else
                { 
                    ReadResult readSubsequent = await _Server.ReadAsync(guid, 1);
                    if (readSubsequent.Status != ReadResultStatus.Success)
                    { 
                        throw new IOException("Unable to read data");
                    }
                     
                    sb.Append((char)(readSubsequent.Data[0]));
                }
            }
             
            return sb.ToString();
        }
    }
}
