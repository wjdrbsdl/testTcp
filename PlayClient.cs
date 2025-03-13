using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using testTcp;

public enum ReqRoomType
{
    Ready, Start, RoomOut, Chat, ClientID
}

public class PlayClient
{
    public Socket clientSocket;
    public int port;
    public byte[] ip;
    public int id;
    public MeetState meetState = MeetState.Lobby;
    public string state = "";

    public PlayClient(byte[] _ip, int _port, int _id = 0)
    {
        ip = _ip;
        id = _id;
        port = _port;
    }

    public void Connect()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress ipAddress = new IPAddress(ip);
        IPEndPoint endPoint = new IPEndPoint(ipAddress, port);
        clientSocket.BeginConnect(endPoint, CallBackConnect, clientSocket);
        EnterMessege();
        //Update();
    }

    private void CallBackConnect(IAsyncResult _result)
    {
        //연결 되었으면 자료 받을 준비, 상태 준비
        try
        {
            Console.WriteLine("게임 클라 연결 콜백");
            byte[] buff = new byte[100];
            clientSocket.BeginReceive(buff, 0, buff.Length, 0, CallBackReceive, buff);
            ReqClientNumber();
        }
        
        catch(Exception e)
        {
            Console.WriteLine("방 접속 실패 재 접속시도");
           Connect();
        }
    }

    private void CallBackReceive(IAsyncResult _result)
    {
        try
        {
            Console.WriteLine("클라 리십 콜백");
            byte[] receiveBuff = _result.AsyncState as byte[];
            int received = clientSocket.EndReceive(_result);
            ReqRoomType reqType = (ReqRoomType)receiveBuff[0];
            if(reqType == ReqRoomType.Chat)
            {
                ResChat(receiveBuff);
            }
            
            clientSocket.BeginReceive(receiveBuff, 0, receiveBuff.Length, 0, CallBackReceive, receiveBuff);
        }
        catch(Exception e)
        {
            Console.WriteLine("클라 리십 실패");
        }
    }

    public void ReqClientNumber()
    {
        byte[] reqID = new byte[] { (byte)ReqRoomType.ClientID, (byte)id };
        clientSocket.Send(reqID);
    }

    public void ReqRoomOut()
    {
        Console.WriteLine("클라가 나가기 요청");
        byte[] reqRoomOut = new byte[] { (byte)ReqRoomType.RoomOut, (byte)id };
        clientSocket.Send(reqRoomOut);
        LobbyClient client = new LobbyClient();
        client.ReConnect();
    }
    
    public void ResRoomOut()
    {

    }


    bool isChatOpen = false;
    public void EnterMessege()
    {
        //채팅 기능 한번만 오픈되도록
        if (isChatOpen == true)
        {
            return;
        }

        isChatOpen = true;
        Console.WriteLine("플레이어 클라이언트 메시지를 입력하세요. 나가기 q");
        while (true)
        {
           // Console.WriteLine("플클 와일문");
            string messege = Console.ReadLine();
            if (messege == "q")
            {
                ReqRoomOut();
                return;
            }

            string chatMeseege = " " + messege;
 
            ReqChat(chatMeseege);
        }

    }

    public void ReqChat(string msg)
    {
        byte[] chatByte = Encoding.Unicode.GetBytes(msg);
        byte[] chatCode = new byte[] { (byte)ReqRoomType.Chat };
        byte[] reqByte = chatCode.Concat(chatByte).ToArray();
        //  Console.WriteLine("클라 센드" + mainSock.Connected);
        if (clientSocket.Connected == true)
            clientSocket.Send(reqByte);
    }

    public void ResChat(byte[] _receiveData)
    {
        string convertStr = Encoding.Unicode.GetString(_receiveData, 1, _receiveData.Length-1);
        char first = convertStr[0];

        int max = Math.Min(convertStr.Length - 1, convertStr.Length);
        string split = convertStr.Substring(1, max);
        if (first == ' ')
        {
            split = "[당신]:" + split;
        }
        else
        {
            int number = _receiveData[0];
            split = "[" + number.ToString() + "]:" + split;
        }
        Console.WriteLine(split);
        
    }

}

