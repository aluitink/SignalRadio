using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SignalRadio.LiquidBridge
{
    public class UdpSocket: IDisposable
    {
        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public void Server(string address, int port, Action<string> onMessageReceived = null)
        {
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
            Receive(onMessageReceived);       
        }

        public void Client(string address, int port, Action<string> onMessageReceived = null)
        {
            _socket.Connect(IPAddress.Parse(address), port);
            Receive(onMessageReceived);            
        }

        public void Send(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndSend(ar);
            }, state);
        }

        private void Receive(Action<string> onMessageReceived = null)
        {            
            _socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                _socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                var message = Encoding.UTF8.GetString(so.buffer, 0, bytes);
                if(onMessageReceived != null)
                    onMessageReceived(message);
            }, state);
        }

        public void Dispose()
        {
            if(_socket != null)
                _socket.Dispose();
        }
    }
}
