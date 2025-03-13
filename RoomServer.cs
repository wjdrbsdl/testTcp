using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace testTcp
{
    public class RoomServer
    {
        public List<ClaInfo> roomUser = new();
        public Socket linkSocket;
        public int number = 0;
        //방장이 호스트가 되어 해당 방에있던 유저들의 입력을 받고 처리 

        public void Start()
        {
            Console.WriteLine("룸 서버 시작");
            linkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 5001);
            linkSocket.Bind(endPoint);
            linkSocket.Listen(4);

            linkSocket.BeginAccept(AcceptCallBack, null);
        }

        public class ClaInfo
        {
            public byte[] buffer;
            public Socket workingSocket;
            public int number;
            public ClaInfo(int _bufferSize, Socket _claSocket, int _number)
            {
                buffer = new byte[_bufferSize];
                workingSocket = _claSocket;
                number = _number;
            }
          
        }

        private void AcceptCallBack(IAsyncResult _result)
        {
            try
            {
                Console.WriteLine("룸 서버 수락");
                Socket client = linkSocket.EndAccept(_result);
                ClaInfo newCla = new ClaInfo(100, client, number);
                number++;
                roomUser.Add(newCla);
                client.BeginReceive(newCla.buffer, 0, newCla.buffer.Length, 0, DataReceived, newCla);

                linkSocket.BeginAccept(AcceptCallBack, null);
            }
            catch (Exception e)
            {
                Console.WriteLine("");
            }

        }

        void DataReceived(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("룸서버로서 리시브");
                ClaInfo cla = (ClaInfo)ar.AsyncState;
                byte[] recevieBuff = cla.buffer;
                int received = cla.workingSocket.EndReceive(ar);
                byte[] buffer = new byte[received];
                Array.Copy(cla.buffer, 0, buffer, 0, received);
                SendChat(cla.buffer, cla.number);
                ////Send(obj.Buffer);
                //HandleRoomMaker(obj, buffer);

                //obj.ClearBuffer();
                cla.workingSocket.BeginReceive(cla.buffer, 0, cla.buffer.Length, 0, DataReceived, cla);
            }
            catch (Exception e)
            {

            }

        }

        public void SendChat(byte[] msg, int _receiveNumbering)
        {
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].number == _receiveNumbering)
                {
                    msg[0] = (byte)' ';
                }
                else
                {
                    msg[0] = (byte)_receiveNumbering;
                }
                Socket socket = roomUser[i].workingSocket;
                if (socket.Connected)
                {
                    socket.Send(msg);
                }

            }
        }

    }

}
