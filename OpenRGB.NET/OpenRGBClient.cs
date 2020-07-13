﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace OpenRGB.NET
{
    public class OpenRGBClient : IDisposable
    {
        private readonly string _ip;
        private readonly int _port;
        private readonly string _name;
        private readonly Socket _socket;
        private readonly int _timeout;
        private bool disposed;

        public bool Connected => _socket?.Connected ?? false;

        #region Basic init methods
        public OpenRGBClient(string ip = "127.0.0.1", int port = 6742, string name = "OpenRGB.NET", bool autoconnect = true, int timeout = 1000)
        {
            _ip = ip;
            _port = port;
            _name = name;
            _timeout = timeout;
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            if (autoconnect) Connect();
        }

        public void Connect()
        {
            if (Connected)
                return;

            IAsyncResult result = _socket.BeginConnect(_ip, _port, null, null);

            result.AsyncWaitHandle.WaitOne(_timeout);

            if (_socket.Connected)
            {
                _socket.EndConnect(result);
            }
            else
            {
                _socket.Close();
                throw new TimeoutException("Failed to connect to server.");
            }

            //null terminate before sending
            SendMessage(
                CommandId.SetClientName,
                Encoding.ASCII.GetBytes(_name + '\0')
            );
        }
        #endregion

        #region Basic Comms methods
        private void SendMessage(CommandId command, IEnumerable<byte> buffer = null, uint deviceId = 0)
        {
            //we can send the header right away. it contains the command we are sending
            //and the size of the packet that follows
            var packetSize = buffer?.Count() ?? 0;
            var result = _socket.Send(
                new OpenRGBPacketHeader(deviceId, (uint)command, (uint)packetSize).Encode()
            );

            if (result != OpenRGBPacketHeader.Size)
                throw new Exception("Sent incorrect number of bytes when sending header in " + nameof(SendMessage));

            if (packetSize <= 0)
                return;

            result = 0;
            if (buffer is byte[] arr)
                result = _socket.Send(arr);
            else
                result = _socket.Send(buffer.ToArray());

            if (result != packetSize)
                throw new Exception("Sent incorrect number of bytes when sending data in " + nameof(SendMessage));

            return;
        }

        private byte[] ReadMessage()
        {
            //we need a byte buffer to store the header
            var headerBuffer = new byte[OpenRGBPacketHeader.Size];
            //then we read into that buffer
            _socket.Receive(headerBuffer, OpenRGBPacketHeader.Size, SocketFlags.None);
            //and decode it into a header to know how many bytes we will receive next
            var header = OpenRGBPacketHeader.Decode(headerBuffer);
            if (header.DataLength <= 0)
                throw new Exception("Length of header was zero");

            //we then make a buffer that will receive the data
            var dataBuffer = new byte[header.DataLength];
            if (_socket.Receive(dataBuffer, (int)header.DataLength, SocketFlags.None) != header.DataLength)
                throw new Exception("Received wrong amount of bytes in " + nameof(ReadMessage));

            return dataBuffer;
        }
        #endregion

        #region Request Methods
        public int GetControllerCount()
        {
            SendMessage(CommandId.RequestControllerCount);
            return (int)BitConverter.ToUInt32(ReadMessage(), 0);
        }

        public OpenRGBDevice GetControllerData(int id)
        {
            if (id < 0)
                throw new ArgumentException(nameof(id));

            SendMessage(CommandId.RequestControllerData, null, (uint)id);
            return OpenRGBDevice.Decode(ReadMessage());
        }

        public OpenRGBDevice[] GetAllControllerData()
        {
            var count = GetControllerCount();

            var array = new OpenRGBDevice[count];
            for (int i = 0; i < count; i++)
                array[i] = GetControllerData(i);

            return array;
        }
        #endregion

        #region Update Methods
        public void UpdateLeds(int deviceId, OpenRGBColor[] colors)
        {
            if (colors is null)
                throw new ArgumentNullException(nameof(colors));

            if (deviceId < 0)
                throw new ArgumentException(nameof(deviceId));

            //4 bytes of nothing
            //2 bytes for how many colors (sizeof(short))
            //4 bytes for each led
            int GetIndex(int a) => 4 + 2 + (4 * a);

            var ledCount = colors.Length;
            var bytes = new byte[GetIndex(ledCount)];

            bytes[0] = 0;
            bytes[1] = 0;
            bytes[2] = 0;
            bytes[3] = 0;
            BitConverter.GetBytes((ushort)ledCount).CopyTo(bytes, 4);

            for (int i = 0; i < ledCount; i++)
                colors[i].Encode().CopyTo(bytes, GetIndex(i));

            SendMessage(CommandId.UpdateLeds, bytes, (uint)deviceId);
        }

        public void UpdateZone(int deviceId, int zoneId, OpenRGBColor[] colors)
        {
            if (colors is null)
                throw new ArgumentNullException(nameof(colors));

            if (deviceId < 0)
                throw new ArgumentException(nameof(deviceId));

            if (zoneId < 0)
                throw new ArgumentException(nameof(zoneId));

            //4 bytes of nothing
            //4 bytes for zone index (uint)
            //2 bytes for how many colors (ushort)
            //4 bytes per color
            int GetIndex(int a) => 4 + 4 + 2 + (4 * a);

            var ledCount = colors.Length;
            var bytes = new byte[GetIndex(ledCount)];

            bytes[0] = 0;
            bytes[1] = 0;
            bytes[2] = 0;
            bytes[3] = 0;
            BitConverter.GetBytes((uint)zoneId).CopyTo(bytes, 4);
            BitConverter.GetBytes((ushort)ledCount).CopyTo(bytes, 8);

            for (int i = 0; i < ledCount; i++)
                colors[i].Encode().CopyTo(bytes, GetIndex(i));

            SendMessage(CommandId.UpdateZoneLeds, bytes, (uint)deviceId);
        }
        #endregion

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Managed object only
                    if (_socket != null && _socket.Connected)
                    {
                        _socket?.Disconnect(false);
                        _socket?.Dispose();
                    }
                    disposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}