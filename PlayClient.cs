using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using testTcp;

public enum ReqRoomCode
{
    Ready, Start, RoomOut
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
        Console.WriteLine("플레이 클라이언트 연결");
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
            HandleChat(receiveBuff);
            clientSocket.BeginReceive(receiveBuff, 0, receiveBuff.Length, 0, CallBackReceive, receiveBuff);
        }
        catch(Exception e)
        {
            Console.WriteLine("클라 리십 실패");
        }
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
            string messege = Console.ReadLine();
            if (messege == "q")
            {
                //Close();
                //return;

                return;
            }

            string chatMeseege = " " + messege;
            byte[] ubytes = System.Text.Encoding.Unicode.GetBytes(chatMeseege);
            ReqChat(ubytes);
        }

    }

    public void ReqRoomOut()
    {

    }

    public void ReqChat(byte[] msg)
    {
        //  Console.WriteLine("클라 센드" + mainSock.Connected);
        if (clientSocket.Connected == true)
            clientSocket.Send(msg);
    }

    void HandleChat(byte[] _chatData)
    {
        string convertStr = Encoding.Unicode.GetString(_chatData);
        char first = convertStr[0];

        int max = Math.Min(convertStr.Length - 1, convertStr.Length);
        string split = convertStr.Substring(1, max);
        if (first == ' ')
        {
            split = "[당신]:" + split;
        }
        else
        {
            int number = _chatData[0];
            split = "[" + number.ToString() + "]:" + split;
        }
        Console.WriteLine(split);
        
    }

    #region 게임 시작
    private void ReqRoomStart()
    {
  
    }

    private void ResRoomStart(byte[] _receiveData)
    {
      
    }
    #endregion
}

