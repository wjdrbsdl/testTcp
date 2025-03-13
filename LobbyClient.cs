using System.Net;
using System.Net.Sockets;
using System.Text;
using testTcp;


public class LobbyClient
{
    public Socket clientSocket;
    public int port = 5000;
    public string ip;
    public int id;
    public MeetState meetState = MeetState.Lobby;
    public string RoomName = "";

    public LobbyClient()
    {

    }

    ~LobbyClient()
    {
        Console.WriteLine( "로비 클라 소멸");
    }

    public LobbyClient(string _ip, int _id, string _preRoomName = "")
    {
        ip = _ip;
        id = _id;
        RoomName = _preRoomName;
    }

    public void Connect()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress ipAddress = IPAddress.Parse(ip);
        LobbyServer.ServerIp = ipAddress; //들어갔던 서버 기록
        ClientLogIn.ServerIp = ipAddress.GetAddressBytes(); //게임 서버 ip 저장. 
        IPEndPoint endPoint = new IPEndPoint(ipAddress, port);
        byte[] buff = new byte[100];
        clientSocket.BeginConnect(endPoint, CallBackConnect, buff);
        Update();
    }

    public void ReConnect()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint endPoint = new IPEndPoint(LobbyServer.ServerIp, port);
        byte[] buff = new byte[100];
        clientSocket.BeginConnect(endPoint, CallBackConnect, buff);
        Update();
    }

    private void CallBackConnect(IAsyncResult _result)
    {
        try
        {
            Console.WriteLine("클라 연결 콜백" );
            byte[] buff = new byte[100];
            clientSocket.BeginReceive(buff, 0, buff.Length, 0, CallBackReceive, buff);
            //접속했으면 접속한 넘버링 요구
            ReqClientNumber();
        }
        catch
        {
            Console.WriteLine("방에서 재 연결 시도");
            Connect();
        }
    }

    private void CallBackReceive(IAsyncResult _result)
    {
        try
        {
            Console.WriteLine("클라 리십 콜백");
            byte[] receiveBuff = _result.AsyncState as byte[];

            ReqLobbyType reqType = (ReqLobbyType)receiveBuff[0];
            if (reqType == ReqLobbyType.RoomMake)
            {
                ResRoomJoin(receiveBuff);
            }
            else if(reqType == ReqLobbyType.ClientNumber)
            {
                ResClientNumber(receiveBuff);
            }

            if(clientSocket.Connected)
                clientSocket.BeginReceive(receiveBuff, 0, receiveBuff.Length, 0, CallBackReceive, receiveBuff);
        }
        catch
        {
            Connect();
        }
    }

    #region 방 생성 진입
    private void ReqRoomJoin(string _roomName = "테스트 방 이름")
    {
        Console.WriteLine("참가 신청");
        string roomName = _roomName;
        byte[] roomByte = Encoding.Unicode.GetBytes(roomName);
        byte[] reqRoom = new byte[roomByte.Length + 1];
        Array.Copy(roomByte, 0, reqRoom, 1, roomByte.Length); //룸 네임 전체를 요청 바이트 1번째부터 복사시작
        reqRoom[0] = (byte)ReqLobbyType.RoomMake;
        clientSocket.Send(reqRoom);
    }

    private void ResRoomJoin(byte[] _receiveData)
    {
        Console.WriteLine("r방에 대한 정보를 받음" + _receiveData[1]);
        //룸메이크 요청에 대한 대답이라면
        /*
              * [0] 응답 코드
              * [1] 룸의 현재 인원 - 0이면 자신이 방장
              * [2] 아이피 주소 길이 
              * [3] 방 이름 길이 
              * [4] 4번부터 2번 만큼
              * [4+[2]] 부터 [3] 만큼
              */

        if (_receiveData[1] == LobbyServer.failCode)
        {
            //현재 인원수 쪽에 패일 코드를 넣어서 불가 체크
            Console.WriteLine("r방에 참가 못했음");
            return;
        }

        //아이피 주소 파싱
        int ipLengthIdx = 2;
        int ipLength = _receiveData[ipLengthIdx];
        byte[] ip = new byte[ipLength];
        int ipIdx = 4;
        for (int i = 0; i < ipLength; i++)
        {
            ip[i] = _receiveData[i + ipIdx];
            Console.WriteLine(ip[i].ToString());
        }
        //아이피 주소 생성
        IPAddress address = new IPAddress(ip);
        int portNum = 5001;

        //방이름 파싱
        int roomNameLength = _receiveData[3];
        string roomName = Encoding.Unicode.GetString(_receiveData, ipIdx + ipLength, _receiveData[3]);

        Console.WriteLine(roomName + "r방에서 순서 " + _receiveData[1].ToString());

        isLobby = false; //게임으로 이동
        int roomPersonCount = _receiveData[1];
        if (roomPersonCount == 0)
        {
            //현재 방 인원이 0 이라면 방장으로서 호스트도 생성
            Console.WriteLine("방장으로서 호스트 진행");
            PlayServer roomServer = new PlayServer();
            roomServer.roomName = roomName;
            roomServer.Start();
        }
        Console.WriteLine("로비 클라 디스컨넥");
        ReqDisConnect();
        clientSocket.Close();//기존 소켓은 끊고 해당 클래스는 지움 
        clientSocket.Dispose();

        Console.WriteLine("플레이어 참가 클라이언트 생성");
        PlayClient playerClient = new PlayClient(ip, 5001, id);
        playerClient.Connect();

        meetState = MeetState.Room;
    }
    #endregion

    private void ReqDisConnect()
    {
        Console.WriteLine("종료 요청");
        byte[] reqClaDisconnect = new byte[] { (byte)ReqLobbyType.Close };
        clientSocket.Send(reqClaDisconnect);
    }

    private void ReqClientNumber()
    {
        byte[] reqClientNumber = new byte[] { (byte)ReqLobbyType.ClientNumber };
        clientSocket.Send(reqClientNumber);
    }

    private void ResClientNumber(byte[] receiveBuff)
    {
        /*
         * [0] 요청타입
         * [1] 넘버링
         */

        id = receiveBuff[1];
        Console.WriteLine("클라 넘버 " + id);
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
