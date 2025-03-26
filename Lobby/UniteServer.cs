using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using testTcp;
using testTcp.Play;


public class UniteServer
{
    public static IPAddress ServerIp;
    static int index = 5;
    public static int bufferSize = 2;
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
            mainSock.Listen(5);
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
            AsyncObject asyObj = (AsyncObject)ar.AsyncState;

            byte[] msgLengthBuff = asyObj.Buffer;

            ushort msgLength = EndianChanger.NetToHost(msgLengthBuff);
            byte[] recvBuffer = new byte[msgLength];
            byte[] recvData = new byte[msgLength];
            int recv = 0;
            int recvIdx = 0;
            int rest = msgLength;
            do
            {
                recv = asyObj.WorkingSocket.Receive(recvBuffer);
                Buffer.BlockCopy(recvBuffer, 0, recvData, recvIdx, recv);
                recvIdx += recv;
                rest -= recv;
                recvBuffer = new byte[rest];//퍼올 버퍼 크기 수정
                if (recv == 0)
                {
                    //처음부터 0번 자료가 들어온거면 상대쪽이 연결 끊은거.
                    AddRemoveSokect(asyObj.numbering);
                    return;
                }
            }
            while (rest >= 1);

            Console.WriteLine("요청 받음 " + (ReqLobbyType)recvData[0]);

            HandleRoomMaker(asyObj, recvData);

            if (asyObj.WorkingSocket.Connected)
                asyObj.WorkingSocket.BeginReceive(asyObj.Buffer, 0, asyObj.BufferSize, 0, DataReceived, asyObj);
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
            AddRemoveSokect(_obj.numbering);
        }
        else if (reqType == ReqLobbyType.RoomState)
        {
            ResChangeRoomState(_reqData);
        }
        else if (reqType == ReqLobbyType.RoomUserCount)
        {
            ResChangeRoomUserCount(_reqData);
        }
        else if (reqType == ReqLobbyType.ClientNumber)
        {
            byte[] response = new byte[] { (byte)ReqLobbyType.ClientNumber, (byte)_obj.numbering };
            SendData(_obj, response);
        }
    }

    int roomPortStart = 10000;
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
            RoomData createRoom = new RoomData(roomServerIP, roomPortStart, roomName);
            roomList.Add(roomName, createRoom);
            UnitePlayServer playSever = new UnitePlayServer(createRoom.portNum, this, roomName);
            playSever.Start();
            
        }
        //있으면 방 데이터에서 참가 여부 가져와서 반환하고
        if (roomList[roomName].roomState == RoomState.Play)
        {
            ColorConsole.ConsoleColor("방참가 불가 코드 발송");
            byte[] joinFailCode = new byte[] { (byte)ReqLobbyType.RoomMakeFail};
            SendData(_obj, joinFailCode);
            return;
        }

        //방 데이터 만들어놓고, 방 참가할 수 있도록 요청한 애 한테 정보 전달
        /*
         * [0] 응답 코드
         * [1] 룸데이터 패킷
         */
 
        byte[] roomDataPacket = roomList[roomName].GetRoomDataPacket();
        byte[] finalRes = new byte[1 + roomDataPacket.Length];
        finalRes[0] = (byte)ReqLobbyType.RoomMake;
        Buffer.BlockCopy(roomDataPacket, 0, finalRes, 1, roomDataPacket.Length);

        SendData(_obj, finalRes);
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

    private void ResChangeRoomState(byte[] _reqData)
    {
        //룸상태 변경 
        /*
         * [0] 코드 RoomState
         * [1] 변경될 타입 RoomState.
         * [2] 방이름 길이
         * [3] 여서부터 방이름
         */
        RoomState roomState = (RoomState)_reqData[1];
        int roomNameLength = _reqData[2];
        int roomNameIndex = 3;
        string roomNameStr = Encoding.Unicode.GetString(_reqData, roomNameIndex, roomNameLength);
        if (roomList.ContainsKey(roomNameStr))
        {
            roomList[roomNameStr].ChangeState(roomState);
            ColorConsole.ConsoleColor(roomNameStr + " 방 상태 변경 " + roomList[roomNameStr].roomState);
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

    public void SendData(AsyncObject _obj, byte[] _msg)
    {
        //헤더작업 용량 길이 붙여주기 
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
            send = _obj.WorkingSocket.Send(sendPacket);
            rest -= send;
        } while (rest >= 1);

    }

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
