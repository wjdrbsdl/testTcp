using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace testTcp.Lobby
{
    public class UniteLobClient
    {
        public Socket clientSocket;
        public int port = 5000;
        public string ip;
        public int id;
        public MeetState meetState = MeetState.Lobby;
        public string RoomName = "";

        public UniteLobClient(string _ip, int _id, string _preRoomName = "")
        {
            ip = _ip;
            id = _id;
            RoomName = _preRoomName;
        }

        public void Connect()
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse(ip);
            UniteServer.ServerIp = ipAddress; //들어갔던 서버 기록
            ClientLogIn.ServerIp = ipAddress.GetAddressBytes(); //게임 서버 ip 저장. 
            IPEndPoint endPoint = new IPEndPoint(ipAddress, port);
            byte[] buff = new byte[100];
            clientSocket.BeginConnect(endPoint, CallBackConnect, buff);
            Update();
        }

        public void ReConnect()
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(UniteServer.ServerIp, port);
            byte[] buff = new byte[100];
            clientSocket.BeginConnect(endPoint, CallBackConnect, buff);
            Update();
        }

        private void CallBackConnect(IAsyncResult _result)
        {
            try
            {
                ColorConsole.Default("로비 클라 연결 콜백");
                byte[] buff = new byte[2];
                clientSocket.BeginReceive(buff, 0, buff.Length, 0, CallBackReceive, buff);
                //접속했으면 접속한 넘버링 요구
                ReqClientNumber();
            }
            catch
            {
                ColorConsole.Default("로비에서 재 연결 시도");
                Connect();
            }
        }

        private void CallBackReceive(IAsyncResult _result)
        {
            try
            {
                ColorConsole.Default("로비 클라 리십 콜백");
                byte[] msgLengthBuff = _result.AsyncState as byte[];

                ushort msgLength = EndianChanger.NetToHost(msgLengthBuff);

                byte[] recvBuffer = new byte[msgLength];
                byte[] recvData = new byte[msgLength];
                int recv = 0;
                int recvIdx = 0;
                int rest = msgLength;
                do
                {
                    recv = clientSocket.Receive(recvBuffer);
                    Buffer.BlockCopy(recvBuffer, 0, recvData, recvIdx, recv);
                    recvIdx += recv;
                    rest -= recv;
                    recvBuffer = new byte[rest];//퍼올 버퍼 크기 수정
                    if (recv == 0)
                    {
                        //만약 남은게있으면 어떡함?
                        break;
                    }
                } while (rest >= 1);


                ReqLobbyType reqType = (ReqLobbyType)recvData[0];
                if (reqType == ReqLobbyType.RoomMake)
                {
                    ResRoomJoin(recvData);
                }
                else if (reqType == ReqLobbyType.ClientNumber)
                {
                    ResClientNumber(recvData);
                }

                if (clientSocket.Connected)
                    clientSocket.BeginReceive(msgLengthBuff, 0, msgLengthBuff.Length, 0, CallBackReceive, msgLengthBuff);
            }
            catch
            {
                Connect();
            }
        }

        #region 방 생성 진입
        private void ReqRoomJoin(string _roomName = "테스트 방 이름")
        {
            ColorConsole.Default("로비에서 방 참가 신청");
            string roomName = _roomName;
            byte[] roomByte = Encoding.Unicode.GetBytes(roomName);
            byte[] reqRoom = new byte[roomByte.Length + 1];
            Array.Copy(roomByte, 0, reqRoom, 1, roomByte.Length); //룸 네임 전체를 요청 바이트 1번째부터 복사시작
            reqRoom[0] = (byte)ReqLobbyType.RoomMake;
            SendMessege(reqRoom);
        }

        private void ResRoomJoin(byte[] _receiveData)
        {
            ColorConsole.Default("로비에서 방에 대한 정보를 받음. 방 인원 : " + _receiveData[1]);
            //룸메이크 요청에 대한 대답이라면
            /*
                  * [0] 응답 코드
                  * [1] 룸의 현재 인원 - 0이면 자신이 방장
                  * [2] 2 , 룸포트 번호
                  * [3] 방 이름 길이 
                  * [4] 4번부터 2번 만큼
                  * [4+[2]] 부터 [3] 만큼
                  */

            if (_receiveData[1] == LobbyServer.failCode)
            {
                //현재 인원수 쪽에 패일 코드를 넣어서 불가 체크
                ColorConsole.Default("방에 참가 못했음");
                Update(); //참가신청하는 순간 입력 while문이 꺼져서 다시 켜주기
                return;
            }

            //아이피 주소 파싱
            int portLengthIdx = 2;
            int portLength = _receiveData[portLengthIdx];
            byte[] portByte = new byte[portLength];
            int portIdx = 4;
            for (int i = 0; i < portLength; i++)
            {
                portByte[i] = _receiveData[i + portIdx];
            }
            //포트 번호 생성
            ushort portNum = (ushort)BitConverter.ToUInt16(portByte);

            //방이름 파싱
            int roomNameLength = _receiveData[3];
            string roomName = Encoding.Unicode.GetString(_receiveData, portIdx + portLength, _receiveData[3]);

            ColorConsole.Default(roomName + "방에서 내 순서 :" + _receiveData[1].ToString());

            isLobby = false; //게임으로 이동
       
            ColorConsole.Default("로비 클라 디스컨넥");
            ReqDisConnect();
            clientSocket.Close();//기존 소켓은 끊고 해당 클래스는 지움 
            clientSocket.Dispose();

            ColorConsole.Default("로비에서 플레이어 참가 클라이언트 생성 포트 번호 : " + portNum);
            PlayClient playerClient = new PlayClient(UniteServer.ServerIp.GetAddressBytes() , portNum, id);
            playerClient.Connect();

            meetState = MeetState.Room;
        }
        #endregion

        private void ReqDisConnect()
        {
            byte[] reqClaDisconnect = new byte[] { (byte)ReqLobbyType.Close };
            SendMessege(reqClaDisconnect);
        }

        private void ReqClientNumber()
        {
            byte[] reqClientNumber = new byte[] { (byte)ReqLobbyType.ClientNumber };
            SendMessege(reqClientNumber);
        }

        private void ResClientNumber(byte[] receiveBuff)
        {
            /*
             * [0] 요청타입
             * [1] 넘버링
             */

            id = receiveBuff[1];
            ColorConsole.Default("클라 넘버 " + id);
        }

        private void SendMessege(byte[] _msg)
        {
            ushort msgLength = (ushort)_msg.Length;
            byte[] msgLengthBuff = EndianChanger.HostToNet(msgLength);

            byte[] originPacket = new byte[msgLengthBuff.Length + msgLength];
            Buffer.BlockCopy(msgLengthBuff, 0, originPacket, 0, msgLengthBuff.Length); //패킷 0부터 메시지 길이 버퍼 만큼 복사
            Buffer.BlockCopy(_msg, 0, originPacket, msgLengthBuff.Length, msgLength); //패킷 메시지길이 버퍼 길이 부터, 메시지 복사

            int rest = (msgLength + msgLengthBuff.Length);
            int send = 0;
            do
            {
                byte[] sendPacket = new byte[rest];
                Buffer.BlockCopy(originPacket, originPacket.Length - rest, sendPacket, 0, rest);
                send = clientSocket.Send(sendPacket);
                rest -= send;
            } while (rest >= 1);
        }

        bool isLobby = true;
        private void Update()
        {
            while (isLobby)
            {
                if (meetState == MeetState.Lobby)
                {
                    // Console.WriteLine("로클 와일문");
                    string command = Console.ReadLine();
                    if (command == "j")
                    {
                        ReqRoomJoin();
                        return;
                    }

                }

            }
        }
    }
}
