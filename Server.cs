using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;


public enum ReqType
{
    RoomMake, RoomStart, RoomOut
}

public enum MeetState
{
    Lobby, Room, Game
}



public class Server
{
    static int index = 5;
    public static int bufferSize = 4000;
    Socket mainSock;
    List<AsyncObject> connectedClientList = new List<AsyncObject>();
    int m_port = 5000;

    public void Start()
    {
        try
        {
            Console.WriteLine("서버 연결 시작" + ParseCurIP.GetLocalIP());
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, m_port);
            mainSock.Bind(serverEP);
            mainSock.Listen(10);
            mainSock.BeginAccept(AcceptCallback, null);
            UpdateRemoveSockect();
            //mainSock.BeginConnect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), Convert.ToInt32(50001)), null, null);
        }
        catch (Exception e)
        {
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
            //  SendChat(obj.Buffer, obj.numbering);
            //Send(obj.Buffer);
            HandleRoomMaker(obj, buffer);

            obj.ClearBuffer();
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
        }
        catch (Exception e)
        {

            AsyncObject obj = (AsyncObject)ar.AsyncState;
            AddRemoveSokect(obj.numbering);
            Console.WriteLine("서버에서이상" + e.HResult);
        }

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
                        break;
                    }
                }
            }
        }
    }
    #endregion



    Dictionary<string, List<AsyncObject>> roomList = new();
    void HandleRoomMaker(AsyncObject _obj, byte[] _reqData)
    {
        //
        ReqType reqType = (ReqType)_reqData[0];
        if (reqType == ReqType.RoomMake)
        {
            string roomName = Encoding.Unicode.GetString(_reqData, 1, _reqData.Length - 1);
            Console.WriteLine("신청한 방이름 " + roomName);

            /*
             * 방 만들기 리스폰스- 응답타입, 방참가 여부, 생성-참가 되었다면 참가자 수, 참가자 정보
             */
            if (roomList.ContainsKey(roomName) == false)
            {
                roomList.Add(roomName, new List<AsyncObject>());
            }
            roomList[roomName].Add(_obj);
            byte[] parti = new byte[roomList[roomName].Count];
            for (int i = 0; i < roomList[roomName].Count; i++)
            {
                parti[i] = (byte)roomList[roomName][i].numbering;
            }
            byte[] roomCode = new byte[] { (byte)ReqType.RoomMake, 1, (byte)roomList[roomName].Count };

            byte[] response = roomCode.Concat(parti).ToArray();

            List<AsyncObject> pati = roomList[roomName];
            for (int i = 0; i < pati.Count; i++)
            {
                pati[i].WorkingSocket.Send(response);
            }
        }

        else if (reqType == ReqType.RoomStart)
        {
            string roomName = Encoding.Unicode.GetString(_reqData, 1, _reqData.Length - 1);
            Console.WriteLine("시작 신청한 방이름 " + roomName);
            List<AsyncObject> pati = roomList[roomName];
            IPEndPoint captain = (IPEndPoint)pati[0].WorkingSocket.RemoteEndPoint;
            byte[] captainAddress = captain.Address.GetAddressBytes();
            byte[] roomCode = new byte[] { (byte)ReqType.RoomStart, 1, (byte)captainAddress.Length };
            byte[] response = roomCode.Concat(captainAddress).ToArray();
            for (int i = 0; i < pati.Count; i++)
            {
                response[1] = (byte)i;
                pati[i].WorkingSocket.Send(response);
            }
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
}


