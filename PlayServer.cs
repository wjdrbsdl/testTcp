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
    public class PlayServer
    {
        public List<ClaInfo> roomUser = new();
        public Socket linkSocket; //룸유저 받아들이는 소켓
        public Socket linkStateLobby; //서버로비와 소통하는 소켓 -> 방 상태 바뀔때 신호 
        public Socket linkRoomCount; //서버로비와 소통하는 소켓 -> 방 인원 바뀔때 신호 
        public int number = 0;
        public string roomName;
        //방장이 호스트가 되어 해당 방에있던 유저들의 입력을 받고 처리 

        public void Start()
        {
            ColorConsole.ConsoleColor("룸 서버 시작");
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
            public int ID;
            public ClaInfo(int _bufferSize, Socket _claSocket)
            {
                buffer = new byte[_bufferSize];
                workingSocket = _claSocket;
               
            }
          
        }

        private void AcceptCallBack(IAsyncResult _result)
        {
            try
            {
                ColorConsole.ConsoleColor("룸 서버 수락");
                Socket client = linkSocket.EndAccept(_result);
                ClaInfo newCla = new ClaInfo(100, client);
                number++;
                roomUser.Add(newCla);
                client.BeginReceive(newCla.buffer, 0, newCla.buffer.Length, 0, DataReceived, newCla);
                SendRoomCount(); //변경한 인원 전달
                linkSocket.BeginAccept(AcceptCallBack, null); //다른거 받을 준비 
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
                ColorConsole.ConsoleColor("룸     서버로서 리시브");
                ClaInfo cla = (ClaInfo)ar.AsyncState;
                byte[] recevieBuff = cla.buffer;
                int received = cla.workingSocket.EndReceive(ar);
                byte[] buffer = new byte[received];
                Array.Copy(cla.buffer, 0, buffer, 0, received);
                //SendChat(cla.buffer, cla.ID);
                ////Send(obj.Buffer);
                //HandleRoomMaker(obj, buffer);
                HandleReq(cla, cla.buffer);


                //obj.ClearBuffer();
                cla.workingSocket.BeginReceive(cla.buffer, 0, cla.buffer.Length, 0, DataReceived, cla);
           
            }
            catch (Exception e)
            {

            }

        }

        private void HandleReq(ClaInfo _claInfo, byte[] _reqData)
        {
            ReqRoomType reqType = (ReqRoomType)_reqData[0];
            
            if(reqType == ReqRoomType.ClientID)
            {
                _claInfo.ID = _reqData[1];
             //  Console.WriteLine("플레이클라이언트 아이디 입력 " + _claInfo.ID);
            }
            else if (reqType == ReqRoomType.Chat)
            {
                SendChat(_reqData, _claInfo.ID);
            }
            else if(reqType ==ReqRoomType.RoomOut)
            {
                ExitClient(_reqData);
                SendRoomCount();
            }
        }

        public void SendChat(byte[] msg, int _receiveNumbering)
        {
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].ID == _receiveNumbering)
                {
                    msg[1] = (byte)' ';
                }
                else
                {
                    msg[1] = (byte)_receiveNumbering;
                }
                Socket socket = roomUser[i].workingSocket;
                if (socket.Connected)
                {
                    socket.Send(msg);
                }

            }
        }

        public void ExitClient(byte[] _receiveData)
        {
            ColorConsole.ConsoleColor($"{_receiveData[1]}번 아이디가 나가길 요청 현재인원 :" + roomUser.Count);
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].ID == _receiveData[1])
                {
                    roomUser.RemoveAt(i);
                }
            }
            Console.WriteLine(roomUser.Count);
         
        }

        #region 룸 상태 변경 전달
        public void SendRoomState()
        {
            IPEndPoint endPoint = new IPEndPoint(LobbyServer.ServerIp, 5000);
            linkStateLobby = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            linkStateLobby.BeginConnect(endPoint, LobbyConnectCallBack, linkStateLobby);
        }

        public void LobbyConnectCallBack(IAsyncResult _result)
        {
            try
            {
               
                ReqChageRoomState();    
            }
            catch
            {
                ColorConsole.ConsoleColor("방 서버의 로비서버 접속 실패");
            }
        }

        public void ReqChageRoomState()
        {
            byte[] reqChangeRoomState = new byte[] { (byte)ReqLobbyType.RoomState, (byte)RoomState.Ready };
            linkStateLobby.Send(reqChangeRoomState);
            linkStateLobby.Close();
            linkStateLobby.Dispose();
        }
        #endregion

        #region 룸 인원 변경 전달
        public void SendRoomCount()
        {
            IPEndPoint endPoint = new IPEndPoint(LobbyServer.ServerIp, 5000);
            linkRoomCount = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            linkRoomCount.BeginConnect(endPoint, RoomCountCallBack, linkRoomCount);
        }

        public void RoomCountCallBack(IAsyncResult _result)
        {
            try
            {
               // Console.WriteLine("방 서버의 로비서버에 인원 변경 전달");
                ReqChangeRoomCount();
            }
            catch
            {
                ColorConsole.ConsoleColor("방수 변경 콜백 실패");
            }
        }

        public void ReqChangeRoomCount()
        {

            ColorConsole.ConsoleColor("방 인원 변경 함수 호출");
            /*
             * [0] 요청타입
             * [1] 현재 유저 수
             * [2] 방 이름 길이
             * [3] 부터 방 제목들
             */

            byte[] name = Encoding.Unicode.GetBytes(roomName);
            byte[] roomCode = new byte[] { (byte)ReqLobbyType.RoomUserCount, (byte)roomUser.Count, (byte)name.Length };
            byte[] resRoomCount = roomCode.Concat(name).ToArray();

            linkRoomCount.Send(resRoomCount);
            linkRoomCount.Close();
            linkRoomCount.Dispose();
        }
        #endregion
    }

}
