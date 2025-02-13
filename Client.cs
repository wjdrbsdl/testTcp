using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace testTcp
{
    public class Client
    {
        List<string> textList = new List<string>();
        Socket mainSock;
        int m_port = 5000;
        public void Connect()
        {
            Console.WriteLine("클라 컨넥");
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress serverAddr = IPAddress.Parse("192.168.0.41");
            IPEndPoint clientEP = new IPEndPoint(serverAddr, 3333);
            mainSock.BeginConnect(clientEP, new AsyncCallback(ConnectCallback), mainSock);
        }
        public void Close()
        {
            Console.WriteLine("클라에서 끊음");
            if (mainSock != null)
            {
                mainSock.Close();
                mainSock.Dispose();
            }
        }
        public class AsyncObject
        {
            public int numbering = 0;
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
        void ConnectCallback(IAsyncResult ar)
        {
            Console.WriteLine("한번인가 두번인가 클라이언트 연결 콜백");


            try
            {
                Console.WriteLine("클라 연결 콜백");
                Socket client = (Socket)ar.AsyncState;
                // client.EndConnect(ar);
                AsyncObject obj = new AsyncObject(Server.bufferSize); //버퍼 사이즈
                //서버에서 보낼때 사이즈를 256으로 보내고
                //기존 사이즈가 255 라서 계속 1번더 받음

                //서버에서 보낼때는 사이즈가 8 이라서 1번만에 받음. 
                obj.numbering = 100;
                obj.WorkingSocket = mainSock;
                mainSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
            }
            catch (Exception e)
            {
                Console.WriteLine("받을거 없음?");
            }



        }

        void DataReceived(IAsyncResult ar)
        {
            //  Console.WriteLine("클라 데이터 리시브");
            try
            {
                AsyncObject obj = (AsyncObject)ar.AsyncState;

                int received = obj.WorkingSocket.EndReceive(ar);
                if (received == 0)
                {
                    obj.WorkingSocket.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
                    return;
                }
                byte[] buffer = new byte[received];

                Array.Copy(obj.Buffer, 0, buffer, 0, received);
                string convertStr = Encoding.Unicode.GetString(obj.Buffer);
                char first = convertStr[0];

                int max = Math.Min(convertStr.Length - 1, 10);
                string split = convertStr.Substring(1, max);
                if (first == ' ')
                {
                    split = "[당신]:" + split;
                }
                else
                {
                    int number = obj.Buffer[0];
                    split = "["+ number.ToString()+ "]:" + split;
                }
                Console.WriteLine(split + " num" + obj.numbering.ToString() + " " + obj.Buffer.Length);
                textList.Add(split);
                Console.Clear();
                for (int i = 0; i < textList.Count; i++)
                {
                    Console.WriteLine(textList[i]);
                }

                obj.WorkingSocket.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
            }
            catch (Exception e)
            {
                Console.WriteLine("먼가 이상 재접속 시도" + e.HResult);
                Connect();
            }
        }
        public void Send(byte[] msg)
        {
          //  Console.WriteLine("클라 센드" + mainSock.Connected);
            mainSock.Send(msg);
        }
    }
}
