using System;
using System.Linq;
using System.Net.Sockets;
using GitHub.Archive;
using GitHub.Network;
using GitHub.Packets;

namespace ConsoleUpdaterServer.Workers
{
    class BaseServerWorker:IWorker
    {
        private AdvancedSocket socket;
        private ICommandPacket packet;

        public void Init(object arg = null)
        {
            socket = arg as AdvancedSocket;
            if (socket == null) throw new ArgumentException();
        }

        public void Update(bool hard, string[] args)
        {
            if (socket == null) return;
            try
            {
                var packet = socket.SendAndRecivePacket(hard ? 
                    CommandType.Update : 
                    CommandType.Revert,
                    string.Join("\n", args.Skip(1)));
                if (packet.Error != 0) return;

                var archive = ArchiveZip.DirToZip(args.First());
                socket.SendArchive(archive);
            }
            catch (SocketException)
            {
                socket.Close();
                socket = null;
            }
        }
    }
}
