using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleUpdaterServer.Workers;
using GitHub.Network;

namespace ConsoleUpdaterServer
{
    class ProgramServer
    {

        private static string processName = "Looper.exe";
        private static event Action<bool, string[]> UpdateAll; 
        private const int maxClientsCount = 100;
        private static Socket socket =
            new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

        private static int currentTasksCount;
        private static List<Task> listTasks = new List<Task>();

        private static void MainLoop(IWorker currentWorker)
        {
            socket.Bind(
                new IPEndPoint(
                    IPAddress.Any,
                    AdvancedSocket.Port));
            socket.Listen(maxClientsCount + 1);
            while (true)
                try
                {
                    var _socket = new AdvancedSocket(socket.Accept());
                    var packet = _socket.RecivePacket(CommandType.Hello);

                    if (currentTasksCount == maxClientsCount)
                        packet.ErrorInfo = "Server is bussy, please try again later";
                    _socket.SendPacket(packet);

                    if (packet.Error != 0)
                    {
                        _socket.Close();
                        continue;
                    }

                    var task = Task.Factory.StartNew(() =>
                    {
                        currentTasksCount++;
                        var numOfTask = listTasks.Count;
                        Console.WriteLine($"[{DateTime.Now}] TaskN{numOfTask} start working");
                        var worker = new BaseServerWorker();
                        worker.Init(_socket);
                        UpdateAll += worker.Update;
                        while (true) Thread.Sleep(int.MaxValue);
                    });

                    listTasks.Add(task);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
        }
        
        private static string ReadLine() => string.Join(" ", Console.ReadLine().Trim().ToLower().Split(' ').Select(a => a.Trim()));

        static void Main(string[] args)
        {
            Console.WriteLine($"[{DateTime.Now}] Hello Server!");
            var task = Task.Factory.StartNew(() => MainLoop(new BaseServerWorker()));
            var inputTask = Task.Run(() => ReadLine());

            while (!task.IsCompleted)
            {
                Thread.Sleep(1000);
                if (inputTask.IsCompleted)
                {
                    var input = inputTask.Result.Split(' ');
                    if (input.Length > 3 ||
                        input.Length < 1 || 
                        input.First() != "send" || 
                        !Directory.Exists(input[1]) ||
                        !File.Exists(Path.Combine(input[1], processName)))
                    {
                        Console.WriteLine("What?");
                        inputTask = Task.Run(() => ReadLine());
                        continue;
                    }
                    var hard = input.Length == 3 && input.Last() == ".";
                    Console.WriteLine("Write version in first line, next write info\nTo send update input 2 empty lines");
                    var array = new List<string> {input[1]};
                    bool firstLineEmpty = false;
                    while (true)
                    {
                        array.Add(Console.ReadLine());
                        if (array.Last() == "")
                        {
                            if (firstLineEmpty)
                                break;
                            firstLineEmpty = true;
                        }
                        else
                            firstLineEmpty = false;
                    }
                    UpdateAll?.Invoke(hard, array.ToArray());
                    inputTask = Task.Run(() => ReadLine());
                }
            }

        }

    }
}
