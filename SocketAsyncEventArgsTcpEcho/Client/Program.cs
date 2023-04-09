using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Program
    {
        public static Socket? ClientSocket;

        public static SocketAsyncEventArgs? ConnectEventArgs;
        public static SocketAsyncEventArgs? ReceiveEventArgs;
        public static SocketAsyncEventArgs? SendEventArgs;

        public static bool isConnected = false;
        public static string buff = "";

        static void Main(string[] args)
        {
            ClientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            ConnectEventArgs = new SocketAsyncEventArgs();
            ConnectEventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);
            ConnectEventArgs.Completed += CompletedConnect;

            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.SetBuffer(new byte[1024], 0, 1024);
            ReceiveEventArgs.Completed += CompletedIO;

            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.Completed += CompletedIO;

            if (!ClientSocket.ConnectAsync(ConnectEventArgs))
            {
                CompletedConnect(null, ConnectEventArgs);
            }

            while (!isConnected) ;

            while(isConnected)
            {
                var msg = Console.ReadLine();
                if(msg is null)
                {
                    continue;
                }
                else if(msg.Equals("exit"))
                {
                    break;
                }

                msg += '\n';
                Send(msg);
            }

            CloseSocket();
        }

        private static void CompletedConnect(object? sender, SocketAsyncEventArgs e)
        {
            isConnected = true;

            if (e.SocketError == SocketError.Success)
            {
                ClientSocket = e.ConnectSocket;
                if(!ClientSocket!.ReceiveAsync(ReceiveEventArgs!))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                Console.WriteLine(e.SocketError.ToString());
            }
        }

        private static void CompletedIO(object? sender, SocketAsyncEventArgs e)
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

        private static void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                string msg = Encoding.UTF8.GetString(e.Buffer!);
                e.SetBuffer(new byte[1024], 0, 1024);
                Console.WriteLine(msg);
            }
            else
            {
                CloseSocket();
                return;
            }

            if (!ClientSocket!.ReceiveAsync(e))
            {
                ProcessReceive(e);
            }
        }

        public static void Send(string message)
        {
            if(SendEventArgs is null)
            {
                return;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(message);
            SendEventArgs.SetBuffer(buffer, 0, buffer.Length);

            if (!ClientSocket!.SendAsync(SendEventArgs))
            {
                ProcessSend(SendEventArgs);
            }
        }

        private static void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                
            }
            else
            {
                CloseSocket();
            }
        }

        public static void CloseSocket()
        {
            if(!isConnected)
            {
                return;
            }

            isConnected = false;
            try
            {
                ClientSocket!.Shutdown(SocketShutdown.Receive);
            }
            catch (Exception) { }
            ClientSocket!.Close();
        }
    }
}