using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;


public enum ReqLobbyType
{
    RoomMake, Close, RoomState, RoomUserCount, ClientNumber
}

public enum MeetState
{
    Lobby, Room, Game
}



public class LobbyServer
{
    public static IPAddress ServerIp;
    static int index = 5;
    public static int bufferSize = 4000;
    Socket mainSock;
    List<AsyncObject> connectedClientList = new List<AsyncObject>();
    int m_port = 5000;
    public static byte failCode = 255;

    #region 초기화 및 데이터 리시브
    public void Start()
    {
        try
        {
            Console.WriteLine("서버 연결 시작" + ParseCurIP.GetLocalIP());
            ServerIp = IPAddress.Parse(ParseCurIP.GetLocalIP());
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, m_port);
            mainSock.Bind(serverEP);
            mainSock.Listen(10);
            mainSock.BeginAccept(AcceptCallback, null);
            UpdateRemoveSockect();
        }
        catch 
        {
            Console.WriteLine("연결 실패");
        }
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
            connectedClientList.Add(obj);
            client.BeginReceive(obj.Buffer, 0, bufferSize, 0, DataReceived, obj);
            

            mainSock.BeginAccept(AcceptCallback, index);
        }
        catch 
        {
            Console.WriteLine("받아들이기 실패");
        }
    }

    void DataReceived(IAsyncResult ar)
    {
        try
        {

            AsyncObject obj = (AsyncObject)ar.AsyncState;
            int received = obj.WorkingSocket.EndReceive(ar);
            byte[] buffer = new byte[received];
            Array.Copy(obj.Buffer, 0, buffer, 0, received);
            Console.WriteLine("요청 받음 " + (ReqLobbyType)buffer[0]);
            //  SendChat(obj.Buffer, obj.numbering);
            //Send(obj.Buffer);
            HandleRoomMaker(obj, buffer);

            obj.ClearBuffer();
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
        }
        catch 
        {

            AsyncObject obj = (AsyncObject)ar.AsyncState;
            AddRemoveSokect(obj.numbering);
            Console.WriteLine("서버에서이상 접속한 소켓 수" + connectedClientList.Count);
        }

    }
    #endregion 

    Dictionary<string, RoomData> roomList = new();
    void HandleRoomMaker(AsyncObject _obj, byte[] _reqData)
    {
        //
        ReqLobbyType reqType = (ReqLobbyType)_reqData[0];
        if (reqType == ReqLobbyType.RoomMake)
        {
            ResRoomMake(_obj, _reqData);
        }
        else if (reqType == ReqLobbyType.Close)
        {
            ColorConsole.ConsoleColor("종료 요청 받음");
            AddRemoveSokect(_obj.numbering);
        }
        else if (reqType == ReqLobbyType.RoomState)
        {
           ColorConsole.ConsoleColor((RoomState)_reqData[1] + "로 변경 요청 들어옴");
        }
        else if (reqType == ReqLobbyType.RoomUserCount)
        {
            ResChangeRoomUserCount(_reqData);
        }
        else if (reqType == ReqLobbyType.ClientNumber)
        {
            byte[] response = new byte[] { (byte)ReqLobbyType.ClientNumber, (byte)_obj.numbering };
            _obj.WorkingSocket.Send(response);
        }
    }

    #region 응답 함수
    private void ResRoomMake(AsyncObject _obj, byte[] _reqData)
    {
        string roomName = Encoding.Unicode.GetString(_reqData, 1, _reqData.Length - 1);

        ColorConsole.ConsoleColor("신청한 방이름 : " + roomName);
      
        IPAddress roomServerIP = ((IPEndPoint)_obj.WorkingSocket.RemoteEndPoint).Address;
        /*
         * 방 만들기 리스폰스- 응답타입, 방참가 여부, 생성-참가 되었다면 참가자 수, 참가자 정보
         */
        //최초 생성이면 로비 서버 데이터에 만들고
        if (roomList.ContainsKey(roomName) == false)
        {
            RoomData createRoom = new RoomData();
            roomList.Add(roomName, createRoom);
            createRoom.roomServerIP = roomServerIP;
        }
        //있으면 방 데이터에서 참가 여부 가져와서 반환하고
        if (roomList[roomName].roomState == RoomState.Play)
        {
            ColorConsole.ConsoleColor("방참가 불가 코드 발송");
            byte[] joinFailCode = new byte[] { (byte)ReqLobbyType.RoomMake, failCode };
            _obj.WorkingSocket.Send(joinFailCode);
            return;
        }

        //방 데이터 만들어놓고, 방 참가할 수 있도록 요청한 애 한테 정보 전달
        /*
         * [0] 응답 코드
         * [1] 룸의 현재 인원 - 0이면 자신이 방장
         * [2] 아이피 주소 길이 
         * [3] 방 이름 길이 
         * [4] 4번부터 2번 만큼
         * [4+[2]] 부터 [3] 만큼
         */

        byte[] roomAddress = roomList[roomName].roomServerIP.GetAddressBytes(); //룸 방장 ip
        byte[] roomNameByte = Encoding.Unicode.GetBytes(roomName);
        byte[] roomCode = new byte[] { (byte)ReqLobbyType.RoomMake, (byte)roomList[roomName].curCount, (byte)roomAddress.Length, (byte)roomNameByte.Length };
        byte[] addRoomAddress = roomCode.Concat(roomAddress).ToArray();
        byte[] finalRes = addRoomAddress.Concat(roomNameByte).ToArray();

        _obj.WorkingSocket.Send(finalRes);
    }

    private void ResChangeRoomUserCount(byte[] _reqData)
    {
        Console.WriteLine("룸 인원 변경 요청 들어옴");
        /*
        * [0] 요청타입
        * [1] 현재 유저 수
        * [2] 방 이름 길이
        * [3] 부터 방 제목들
        */
        int roomCount = _reqData[1];
        int roomNameLength = _reqData[2];
        int roomNameIndex = 3;
        string roomNameStr = Encoding.Unicode.GetString(_reqData, roomNameIndex, roomNameLength);
        if (roomList.ContainsKey(roomNameStr))
        {
            roomList[roomNameStr].curCount = roomCount;
            Console.WriteLine(roomNameStr + "방 인원 수 변경 " + roomCount.ToString());
        }
    }

    public void SendChat(byte[] msg, int _receiveNumbering)
    {
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
    #endregion

    public void Send(byte[] msg)
    {
        for (int i = 0; i < connectedClientList.Count; i++)
        {
            Console.WriteLine("먼가 발산");
            Socket socket = connectedClientList[i].WorkingSocket;
            if (socket.Connected)
            {

                socket.Send(msg);
            }

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



    #region 소켓리스트에서 제거
    Queue<int> removeQueue = new Queue<int>();
    private void AddRemoveSokect(int _numbering)
    {
        removeQueue.Enqueue(_numbering);
    }

    private void UpdateRemoveSockect()
    {
        while (true)
        {
            int removeCount = removeQueue.Count;
            for (int i = 0; i < removeCount; i++)
            {
                int numbering = removeQueue.Dequeue();
                for (int x = 0; x < connectedClientList.Count; x++)
                {
                    if (numbering == connectedClientList[x].numbering)
                    {

                        connectedClientList[x].WorkingSocket.Close();
                        connectedClientList[x].WorkingSocket.Dispose();

                        connectedClientList.RemoveAt(x);
                        Console.WriteLine(numbering + "소켓 제거 남은 수 " + connectedClientList.Count);
                        break;
                    }
                }
            }
        }
    }
    #endregion
}


