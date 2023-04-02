using System;
using System.Net.Sockets;

namespace OpenRGB.NET.Utils
{

    internal static class SocketExtensions
    {
        internal static int ReceiveFull(this Socket socket, byte[] buffer)
        {
            var size = buffer.Length;
            var total = 0;

            while (total < size)
            {
                var recv = socket.Receive(buffer, total, size - total, SocketFlags.None);
                if (recv == 0) break;
                total += recv;
            }

            return total;
        }

        internal static int SendFull(this Socket socket, ReadOnlySpan<byte> buffer)
        {
            var size = buffer.Length;
            var total = 0;

            while (total < size)
            {
                var recv = socket.Send(buffer.Slice(total).ToArray());
                if (recv == 0) break;
                total += recv;
            }

            return total;
        }

        internal static void Connect(this Socket socket, string ip, int port, TimeSpan timeout)
        {
            var result = socket.BeginConnect(ip, port, null, null);

            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                socket.EndConnect(result);
            }
            else
            {
                socket.Close();
                throw new SocketException(10060); // Connection timed out.
            }
        }
    }
}