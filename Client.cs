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
        private string tryIP = "";

        bool isChatOpen = false;
        bool isCancle = false; //플레이어가 취소 요청 보냈으면 이후 승낙되어도 얘는 파기
        public void Connect(string _ip)
        {
            Console.WriteLine(_ip+"서버에 컨넥 시도");
            tryIP = _ip;
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string parseIP = _ip;
            IPAddress serverAddr = IPAddress.Parse(parseIP);
            IPEndPoint clientEP = new IPEndPoint(serverAddr, m_port);
           
            mainSock.BeginConnect(clientEP, new AsyncCallback(ConnectCallback), mainSock);
            EnterMessege(); //메시지 입력 호출
        }

        public void Close()
        {
            Console.WriteLine("클라에서 끊음");
            isCancle = true;
            if (mainSock != null)
            {
                mainSock.Close();
                mainSock.Dispose();
                mainSock = null;
            }
            
            ClientLogIn login = new ClientLogIn();
            login.EnterIP();
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
            Console.WriteLine("연결 응답 확인");
            if (mainSock == null)
                return;

            try
            {
                Console.WriteLine("연결 성공");
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
                if(isCancle == true)
                {
                    return;
                }
                Console.WriteLine($"연결 실패 {tryIP} 재 연결 시도");
                Connect(tryIP);
                
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
                Console.WriteLine("메시지를 입력하세요. 나가기 q");

                obj.WorkingSocket.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
            }
            catch (Exception e)
            {
                if (isCancle == true)
                    return;
                Console.WriteLine("먼가 이상 재접속 시도" + e.HResult);
                Connect(tryIP);
            }
        }

        public void EnterMessege()
        {
            //채팅 기능 한번만 오픈되도록
            if(isChatOpen == true)
            {
                return;
            }

            isChatOpen = true;
            Console.WriteLine("메시지를 입력하세요. 나가기 q");
            while (true)
            {
                string messege = Console.ReadLine();
                if(messege == "q")
                {
                    Close();
                    return;
                }
                
                string chatMeseege = " " + messege;
                byte[] ubytes = System.Text.Encoding.Unicode.GetBytes(chatMeseege);
                Send(ubytes);
            }
            
        }

        public void Send(byte[] msg)
        {
          //  Console.WriteLine("클라 센드" + mainSock.Connected);
           if(mainSock.Connected == true)
            mainSock.Send(msg);
        }
    }
}
