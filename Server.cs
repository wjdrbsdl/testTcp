using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace testTcp
{
    public class Server
    {
        int start = 5;
        static int index = 5;
        public static int bufferSize = 255;
        Socket mainSock;
        List<Socket> connectedClients = new List<Socket>();
        List<AsyncObject> connectedClientList = new List<AsyncObject>();
        int m_port = 5000;
        public void Start()
        {
            try
            {
                Console.WriteLine("서버 연결 시작");
                mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, m_port);
                mainSock.Bind(serverEP);
                mainSock.Listen(10);
                mainSock.BeginAccept(AcceptCallback, null);
                //mainSock.BeginConnect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), Convert.ToInt32(50001)), null, null);
            }
            catch (Exception e)
            {
            }
        }

        public void Close()
        {
           
            if (mainSock != null)
            {
                mainSock.Close();
                mainSock.Dispose();
            }

            foreach (AsyncObject socket in connectedClientList)
            {
                socket.WorkingSocket.Close();
                socket.WorkingSocket.Dispose();
            }
            connectedClientList.Clear();

            //mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }

        public class AsyncObject
        {
            public int numbering;
            public byte[] Buffer;
            public Socket WorkingSocket;
            public readonly int BufferSize;
            public AsyncObject(int bufferSize)
            {
                BufferSize = bufferSize;
                Buffer = new byte[(long)BufferSize];
            }

            public void ClearBuffer()
            {
                Array.Clear(Buffer, 0, BufferSize);
            }
        }

        void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("서버 수락 콜백 - 클라로부터 받음");
                Socket client = mainSock.EndAccept(ar);
                AsyncObject obj = new AsyncObject(bufferSize);
                obj.numbering = index;
                index++;
                obj.WorkingSocket = client;
                connectedClients.Add(client);
                connectedClientList.Add(obj);
                client.BeginReceive(obj.Buffer, 0, bufferSize, 0, DataReceived, obj);

                mainSock.BeginAccept(AcceptCallback, null);
            }
            catch (Exception e)
            { }
        }

        void DataReceived(IAsyncResult ar)
        {
            try
            {
                AsyncObject obj = (AsyncObject)ar.AsyncState;
                int received = obj.WorkingSocket.EndReceive(ar);
                byte[] buffer = new byte[received];
                Array.Copy(obj.Buffer, 0, buffer, 0, received);
                Console.WriteLine(obj.numbering + "클라로 부터 데이터 받음");

                SendChat(obj.Buffer, obj.numbering);
                //Send(obj.Buffer);

                obj.ClearBuffer();
                obj.WorkingSocket.BeginReceive(obj.Buffer, 0, bufferSize, 0, DataReceived, obj);
            }
            catch (Exception e)
            {

                AsyncObject obj = (AsyncObject)ar.AsyncState;
                for (int i = 0; i < connectedClientList.Count; i++)
                {
                    if(obj.numbering == connectedClientList[i].numbering)
                    {

                        connectedClientList[i].WorkingSocket.Close();
                        connectedClientList[i].WorkingSocket.Dispose();
                        Console.WriteLine(obj.numbering +" 소켓 제거");
                        connectedClientList.RemoveAt(i);
                        break;
                    }
                }
                Console.WriteLine("서버에서이상"+  e.HResult);
            }
           
        }

        public void SendChat(byte[] msg, int _receiveNumbering)
        {
            //for (int i = 0; i < connectedClients.Count; i++)
            //{
            //    if ((_index - start) == i)
            //    {
            //        msg[0] = 5;
            //    }
            //    else
            //    {
            //        msg[0] = (byte)' ';
            //    }
            //    Socket socket = connectedClients[i];
            //    if (socket.Connected)
            //    {
            //        socket.Send(msg);
            //    }

            //}

            for (int i = 0; i < connectedClientList.Count; i++)
            {
                if (connectedClientList[i].numbering == _receiveNumbering)
                {
                    msg[0] = (byte)' ';
                }
                else
                {
                    msg[0] = (byte)_receiveNumbering;
                }
                Socket socket = connectedClientList[i].WorkingSocket;
                if (socket.Connected)
                {
                    socket.Send(msg);
                }

            }
        }

        public void Send(byte[] msg)
        {
            for (int i = 0; i < connectedClients.Count; i++)
            {
                Console.WriteLine("먼가 발산");
                Socket socket = connectedClients[i];
                if (socket.Connected)
                {

                    socket.Send(msg);
                }

            }
        }

    }

}
