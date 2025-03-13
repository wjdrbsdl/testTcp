using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using testTcp;


public class RoomClient
{
    public Socket clientSocket;
    public int port = 5000;
    public string ip;
    public int id;
    public MeetState meetState = MeetState.Lobby;
    public string RoomName = "";

    public RoomClient(string _ip, int _id, string _preRoomName = "")
    {
        ip = _ip;
        id = _id;
        RoomName = _preRoomName;
    }

    public void Connect()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress ipAddress = IPAddress.Parse(ip);
        Server.ServerIp = ipAddress; //들어갔던 서버 기록
        ClientLogIn.ServerIp = ipAddress.GetAddressBytes(); //게임 서버 ip 저장. 
        IPEndPoint endPoint = new IPEndPoint(ipAddress, port);
        clientSocket.BeginConnect(endPoint, CallBackConnect, clientSocket);
        Update();
    }

    public void ReConnect()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress ipAddress = Server.ServerIp;
        ClientLogIn.ServerIp = ipAddress.GetAddressBytes(); //게임 서버 ip 저장. 
        IPEndPoint endPoint = new IPEndPoint(ipAddress, port);
        clientSocket.BeginConnect(endPoint, CallBackReConnect, clientSocket);
        Update();
    }

    private void CallBackConnect(IAsyncResult _result)
    {
        try
        {
            //연결 되었으면 자료 받을 준비, 상태 준비
            Console.WriteLine("클라 연결 콜백");
            byte[] buff = new byte[100];
            clientSocket.BeginReceive(buff, 0, buff.Length, 0, CallBackReceive, buff);
        }
        catch
        {
            Console.WriteLine("방에서 재 연결 시도");
            Connect();
        }
    }

    private void CallBackReConnect(IAsyncResult _result)
    {
        try
        {
            //연결 되었으면 자료 받을 준비, 상태 준비
            Console.WriteLine("클라 재연결");
            byte[] buff = new byte[100];
            clientSocket.BeginReceive(buff, 0, buff.Length, 0, CallBackReceive, buff);
            ReqRoomReJoin(RoomName);
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

            ReqType reqType = (ReqType)receiveBuff[0];
            if (reqType == ReqType.RoomMake)
            {
                ResRoomJoin(receiveBuff);
            }
            else if (reqType == ReqType.RoomStart)
            {
                ResRoomStart(receiveBuff);
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
    private void ReqRoomJoin(string _string)
    {
        Console.WriteLine("참가 신청");
        string roomName = _string;
        byte[] roomByte = Encoding.Unicode.GetBytes(roomName);
        byte[] reqRoom = new byte[roomByte.Length + 1];
        Array.Copy(roomByte, 0, reqRoom, 1, roomByte.Length); //룸 네임 전체를 요청 바이트 1번째부터 복사시작
        reqRoom[0] = (byte)ReqType.RoomMake;
        clientSocket.Send(reqRoom);
    }

    private void ReqRoomJoin()
    {
        string roomName = "테스트로 아무거나 써보기";
        ReqRoomJoin(RoomName);
    }

    private void ResRoomJoin(byte[] _receiveData)
    {
        Console.WriteLine("방에 대한 정보를 받음" + _receiveData[1]);
        //룸메이크 요청에 대한 대답이라면
        //[1] 이 응답
        //[2] 가 참가인원
        //[3] 부터 참가 인원 넘버링 

        if (_receiveData[1] == Server.failCode)
        {
            Console.WriteLine("방에 참가 못했음");
            return;
        }

        string parti = "참가 번호 : ";
        for (int i = 0; i < _receiveData[2]; i++)
        {
            parti += _receiveData[i + 3].ToString() + " ";
        }
        Console.WriteLine(parti);
        meetState = MeetState.Room;

    }
    #endregion

    private void ReqRoomReJoin(string _string)
    {
        Console.WriteLine("재참가 신청");
        string roomName = _string;
        byte[] roomByte = Encoding.Unicode.GetBytes(roomName);
        byte[] reqReJoin = new byte[roomByte.Length + 1];
        Array.Copy(roomByte, 0, reqReJoin, 1, roomByte.Length); //룸 네임 전체를 요청 바이트 1번째부터 복사시작
        reqReJoin[0] = (byte)ReqType.RoomReJoin;
        clientSocket.Send(reqReJoin);
    }

    #region 게임 시작
    private void ReqRoomStart()
    {
        Console.WriteLine("시작 요청");
        string roomName = "테스트로 아무거나 써보기"; //있던 방이름
        byte[] roomByte = Encoding.Unicode.GetBytes(roomName);
        byte[] reqRoomStart = new byte[roomByte.Length + 1];
        Array.Copy(roomByte, 0, reqRoomStart, 1, roomByte.Length); //룸 네임 전체를 요청 바이트 1번째부터 복사시작
        reqRoomStart[0] = (byte)ReqType.RoomStart;
        clientSocket.Send(reqRoomStart);
    }

    private void ResRoomStart(byte[] _receiveData)
    {
        Console.WriteLine("시작 요청에 대한 응답");
        byte[] ip = new byte[4];
        for (int i = 0; i < _receiveData[2]; i++)
        {
            ip[i] = _receiveData[i + 3];
            Console.WriteLine(ip[i].ToString());
        }
        IPAddress address = new IPAddress(ip);
        int portNum = 5001;
        Console.WriteLine("방에서 순서 "+_receiveData[1].ToString());
    
        isLobby = false; //게임으로 이동
        if (_receiveData[1] == 0)
        {
            Console.WriteLine("방장으로서 호스트 진행");
            RoomServer roomServer = new RoomServer();
            roomServer.Start();
        }
        Console.WriteLine("디스컨넥");
        ReqDisConnect();
        clientSocket.Close();//기존 소켓은 끊고 해당 클래스는 지움 
        clientSocket.Dispose();

        Console.WriteLine("플레이어 참가 클라이언트 생성");
        PlayerClient playerClient = new PlayerClient(ip, 5001);
        playerClient.Connect();

    
        //clientSocket.Close();//기존 소켓은 끊고 해당 클래스는 지움 
        //clientSocket.Dispose();
        //meetState = MeetState.Game;
    }
    #endregion

    private void ReqDisConnect()
    {
        Console.WriteLine("종료 요청");
        byte[] reqClaDisconnect = new byte[] { (byte)ReqType.Close };
        clientSocket.Send(reqClaDisconnect);
    }
    bool isLobby = true;
    private void Update()
    {
        while (isLobby)
        {
            if (meetState == MeetState.Lobby)
            {
                string command = Console.ReadLine();
                if (command == "j")
                {
                    ReqRoomJoin();
                }

            }
            else if (meetState == MeetState.Room)
            {
                string command = Console.ReadLine();
                if (command == "s")
                {
                    ReqRoomStart();
                   
                }

            }
        }
    }

}
