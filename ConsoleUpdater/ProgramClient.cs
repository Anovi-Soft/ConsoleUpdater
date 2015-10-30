using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GitHub.Network;

namespace ConsoleUpdater
{
    class ProgramClient
    {
        private static string serverIp = "127.0.0.1";
        private static string processName = "Looper.exe";
        private static string folderProject = "looper";
        private static AdvancedSocket socket;

        static void Main(string[] args)
        {
            Console.WriteLine("{0}Hello client{0}", new string('-', 30));
            socket =
               new AdvancedSocket(
                   new Socket(
                       AddressFamily.InterNetwork,
                       SocketType.Stream,
                       ProtocolType.Tcp));
            try
            {
                //HELLO
                Hello();
                Console.WriteLine("{0}GetStart{0}", new string('-', 30));
                //WORK
                Work();
            }
            catch (SocketException)
            {
                Console.WriteLine("{0}Connection lost{0}", new string('-', 30));
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                socket.Close();
            }
        }

        private static void Work()
        {
            while (true)
            {
                var packet = socket.RecivePacket(CommandType.Revert | CommandType.Update);

                Console.WriteLine("{0}NEW VERSION{0}", new string('-', 30));
                Console.WriteLine($"New in version ({packet.Args.First()}):");

                packet.Args.Skip(1).ToList().ForEach(Console.WriteLine);

                Console.WriteLine("{0}-{0}", new string('-', 33));
                if (packet.Command == CommandType.Revert)
                {
                    while (true)
                    {
                        Console.WriteLine("Do you want to update? [y/n]");
                        var answer = Console.ReadLine().Trim().ToLower();
                        if (!"y,n".Split(',').Contains(answer)) continue;
                        if (answer == "n")
                            packet.Error = 1;
                        break;
                    }
                }
                socket.SendPacket(packet);
                if (packet.Error == 1)
                    continue;


                var archive = socket.RecieveArchive();
                KillProcess();
                archive.UnpackTo(folderProject);
                StartProcess();
            }
        }

        private static void Hello()
        {
            socket.Connect(serverIp, AdvancedSocket.Port);
            var packet = socket.SendAndRecivePacket(CommandType.Hello, "");
            if (packet.Error == 0)
            {
                Console.WriteLine($"Connect to server done");
                return;
            }
            Console.WriteLine(packet.ErrorInfo);
            throw new Exception();
        }

        static void KillProcess()
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                process.Kill();
            }
        }
        static void StartProcess()
        {
            Process.Start(Path.Combine(folderProject, processName));
        }
    }
}
