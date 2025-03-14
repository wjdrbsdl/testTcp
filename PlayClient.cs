using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using testTcp;


public class PlayerData
{
    public int ID;
    public int restCardCount;
    public int badPoint;
}


public class PlayClient
{
    public Socket clientSocket;
    public int port;
    public byte[] ip;
    public int id;
    public MeetState meetState = MeetState.Lobby;
    public string state = "";
    public List<CardData> haveCardList;
    public List<CardData> giveCardList;
    public List<CardData> selecetCardList;
    public bool isMyTurn = false;
    public bool isGameStart = false;
    public PlayClient(byte[] _ip, int _port, int _id = 0)
    {
        ip = _ip;
        id = _id;
        port = _port;
        haveCardList = new();
        giveCardList = new();
        selecetCardList = new();
    }

    #region 연결 수신
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
            ReqRegisterClientID();
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
           // Console.WriteLine("클라 리십 콜백");
            byte[] receiveBuff = _result.AsyncState as byte[];
            int received = clientSocket.EndReceive(_result);
            byte[] validData = new byte[received];
            Array.Copy(receiveBuff, validData, received);
            ReqRoomType reqType = (ReqRoomType)receiveBuff[0];
            HandleReceiveData(reqType, validData);
            
            clientSocket.BeginReceive(receiveBuff, 0, receiveBuff.Length, 0, CallBackReceive, receiveBuff);
        }
        catch(Exception e)
        {
            Console.WriteLine("클라 리십 실패");
        }
    }
    #endregion

    private void HandleReceiveData(ReqRoomType _reqType, byte[] _validData)
    {
        if (_reqType == ReqRoomType.Chat)
        {
            ResChat(_validData);
        }
        else if (_reqType == ReqRoomType.Start)
        {
            ResGameStart(_validData);
        }
        else if (_reqType == ReqRoomType.PutDownCard)
        {
            ResPutDownCard(_validData);
        }
        else if (_reqType == ReqRoomType.ArrangeTurn)
        {
            ResTurnPlayer(_validData);
        }
        else if (_reqType == ReqRoomType.PartyData)
        {
            //idRegister에 반환되는 타입.
            ResRegisterClientIDToPartyID(_validData);
        }
    }

    #region 게임 시작
    private void ReqGameStart()
    {
        byte[] reqStart = { (byte)ReqRoomType.Start };
        clientSocket.Send(reqStart);
    }

    private void ResGameStart(byte[] _resDate)
    {
        /*
         * 셔플된 카드데이터
         * [0] 응답코드
         * [1] 카드 장수
         * [2] 번부터 2개씩 카드가 생성
         */
        isMyTurn = false;
        isGameStart = true;
        for (int i = 2; i < _resDate.Length; i+=2)
        {
            //i번째는 카드 무늬, i+1에는 카드 넘버가 있음
            CardData card = new CardData((CardClass)_resDate[i], _resDate[i + 1]);
            haveCardList.Add(card);
            Console.WriteLine($"받은 카드 {card.cardClass} : {card.num}");
        }
    }

    #endregion

    #region 플레이 서버에 내 아이디 등록
    public void ReqRegisterClientID()
    {
        byte[] reqID = new byte[] { (byte)ReqRoomType.IDRegister, (byte)id };
        clientSocket.Send(reqID);
    }

    public void ResRegisterClientIDToPartyID(byte[] _data)
    {
        //자기 id를 알려주면 다른 모든 참가 아이디를 반환받음
        /*
          * [0] 응답코드 PartyIDes,
          * [1] ID를 받은 유효한 파티원 수
          * [2] 각 파티원 정보 길이 --일단 id만 받음
          * [3] 0번 파티원부터 정보 입력
          */

        for (int i = 3; i < _data.Length; i += _data[2])
        {
            for (int infoIndex = i; infoIndex < _data[2]; infoIndex++)
            {
                Console.WriteLine(_data[infoIndex]+"번 참가");
            }
        }
    }
    #endregion

    #region 방나가기
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
    #endregion


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

            if(isGameStart == true)
            {
                GiveCardCommand();
                break;
            }

            if (messege == "q")
            {
                ReqRoomOut();
                return;
            }
            else if(messege == "s")
            {
                ReqGameStart();
                continue;
            }

            string chatMeseege = " " + messege;
 
            ReqChat(chatMeseege);
        }

    }

    private void GiveCardCommand()
    {
        Console.WriteLine("제출할 카드를 골라 주세요 1,2,3,4");
        while (true)
        {
            string card = Console.ReadLine();
            selecetCardList = new();
            if(isMyTurn == false)
            {
                Console.WriteLine("자기 차례가 아닙니다.");
                continue;
            }
            if(Int32.TryParse(card, out int selectCard) && 0<=selectCard && selectCard<haveCardList.Count)
            {
                Console.WriteLine($"{haveCardList[selectCard].cardClass}:{haveCardList[selectCard].num} 카드 선택");
                selecetCardList.Add(haveCardList[selectCard]);
                ReqPutDownCard(selecetCardList);
            }
            else
            {
                Console.WriteLine("유효 숫자가 아닙니다.");
            }
          
        }
    }

    private void ReqPutDownCard(List<CardData> _cardDataList)
    {
        /*
         * [0] 요청 코드 putdownCard
         * [1] 플레이어 id
         * [2] 낸 카드 숫자
         * [3] 카드 구성
         */
        Console.WriteLine("카드 제출 요청");
        List<byte> reqCardList = new();
        reqCardList.Add((byte)ReqRoomType.PutDownCard);
        reqCardList.Add((byte)id);
        reqCardList.Add((byte)_cardDataList.Count);
        for (int i = 0; i < _cardDataList.Count; i++)
        {
            reqCardList.Add((byte)_cardDataList[i].cardClass);
            reqCardList.Add((byte)_cardDataList[i].num);
        }
        byte[] reqData = reqCardList.ToArray();
        clientSocket.Send(reqData);
    }

    private void ResPutDownCard(byte[] _data)
    {
        //유저가 어떤 카드를 냈는지 전달
        /*
        * [0] 요청 코드 putdownCard
        * [1] 플레이어 id
        * [2] 낸 카드 숫자
        * [3] 카드 구성
        */
        Console.WriteLine(_data[1]+"번 유저가 제출한 카드");
        for (int i = 3; i < _data.Length; i+=2)
        {
            Console.WriteLine($"{(CardClass)_data[i]}:{_data[i+1]}");
        }
    }

    private void ResTurnPlayer(byte[] _data)
    {
        /*
         * [0] 응답코드 ArrangeTurn
         * [1] 차례 ID
         */
        isMyTurn = id == _data[1];
        if (isMyTurn)
        {
            Console.WriteLine("내 차례");
        }
    }

    private void ReqChat(string msg)
    {
        byte[] chatByte = Encoding.Unicode.GetBytes(msg);
        byte[] chatCode = new byte[] { (byte)ReqRoomType.Chat };
        byte[] reqByte = chatCode.Concat(chatByte).ToArray();
        //  Console.WriteLine("클라 센드" + mainSock.Connected);
        if (clientSocket.Connected == true)
            clientSocket.Send(reqByte);
    }

    private void ResChat(byte[] _receiveData)
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

