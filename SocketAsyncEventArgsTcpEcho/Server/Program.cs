using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        public static Socket? ServerSocket;

        private static void Main(string[] args)
        {
            ServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Bind(new IPEndPoint(IPAddress.Any, 8080));
            ServerSocket.Listen((int)SocketOptionName.MaxConnections);

            SocketAsyncEventArgs eventArgs = new();
            eventArgs.Completed += CompletedAccept;
            OnAccept(eventArgs);

            while (true) { }
        }

        private static void OnAccept(SocketAsyncEventArgs e)
        {
            if (ServerSocket is null)
            {
                return;
            }

            e.AcceptSocket = null;

            if (!ServerSocket.AcceptAsync(e))
            {
                ProcessAccept(e);
            }
        }

        private static void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (ServerSocket is null)
            {
                e.AcceptSocket?.Shutdown(SocketShutdown.Both);
                return;
            }

            if (e.SocketError == SocketError.Success)
            {
                UserToken token = new UserToken(e.AcceptSocket!);
            }
            else
            {
                Console.WriteLine(e.SocketError.ToString());
            }

            OnAccept(e);
        }

        private static void CompletedAccept(object? sender, SocketAsyncEventArgs e)
        {
            if (ServerSocket is null)
            {
                return;
            }
            ProcessAccept(e);
        }
    }

    public class UserToken
    {
        private Socket? _socket;

        private SocketAsyncEventArgs? _ReceiveEventArgs;
        private SocketAsyncEventArgs? _SendEventArgs;

        public Socket? Socket => _socket;

        public UserToken(Socket socket)
        {
            _socket = socket;

            _ReceiveEventArgs = new SocketAsyncEventArgs();
            _ReceiveEventArgs.SetBuffer(new byte[1024], 0, 1024);
            _ReceiveEventArgs.Completed += CompletedIO;

            _SendEventArgs = new SocketAsyncEventArgs();
            _SendEventArgs.Completed += CompletedIO;

            if (!_socket!.ReceiveAsync(_ReceiveEventArgs))
            {
                ProcessReceive(_ReceiveEventArgs);
            }
        }

        private void CompletedIO(object? sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (!_socket!.Connected || e.BytesTransferred <= 0)
            {
                return;
            }

            byte[] buffer = e.Buffer!;
            string msg = Encoding.UTF8.GetString(buffer).Replace("\0", "").Trim();
            e.SetBuffer(new byte[1024], 0, 1024);

            Received(msg);
            Send(msg);

            if (!_socket!.ReceiveAsync(e))
            {
                ProcessReceive(e);
            }
        }

        private void Received(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Send(string message)
        {
            if (_SendEventArgs is null)
            {
                return;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(message + '\n');
            _SendEventArgs.SetBuffer(buffer, 0, buffer.Length);
            if (!_socket!.SendAsync(_SendEventArgs))
            {
                ProcessSend(_SendEventArgs);
            }
        }

        public void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
            {
                return;
            }
        }
    }
}