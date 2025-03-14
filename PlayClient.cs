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
    #region 변수
    public Socket clientSocket;
    public int port;
    public byte[] ip;
    public int id;
    public MeetState meetState = MeetState.Lobby;
    public string state = "";
    public List<CardData> haveCardList; //내가 들고 있는 카드
    public List<CardData> giveCardList; //전에 내가 냈던 카드
    public List<CardData> selecetCardList; //이번턴에 내가 선택한 카드
    public List<CardData> putDownList; //바닥에 깔린 카드
    public bool isMyTurn = false;
    public bool isGameStart = false;
    public int gameTurn = 0; //카드 제출이 진행된 턴 1번부터

    #endregion

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

        catch
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
        catch
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

    #region 로직 파트
    bool isChatOpen = false;
    private void SetNewGame()
    {
        //보유 카드는 통신응답에서 진행
        giveCardList = new();
        selecetCardList = new();
        putDownList = new();
        gameTurn = 0;
        isMyTurn = false;
        isGameStart = true;
    }

    private void EnterMessege()
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

            if (isGameStart == true)
            {
                //TestMixture();
                GiveCardCommand();
                break;
            }

            if (messege == "q")
            {
                ReqRoomOut();
                return;
            }
            else if (messege == "s")
            {
                ReqGameStart();
                continue;
            }

            string chatMeseege = " " + messege;

            ReqChat(chatMeseege);
        }

    }

    private void TestMixture()
    {
        Console.WriteLine("제출할 카드를 골라 주세요 1,2,3,4");
        while (true)
        {
            string card = Console.ReadLine();
            selecetCardList = new();

            card = card.Replace(" ", "");//공백제거
            string[] selectCards = card.Split(","); //콤마로 구별
            int validCount = 0;
            for (int i = 0; i < selectCards.Length; i++)
            {
                char cardClass = selectCards[i][0];
                CardClass selectClass = CardClass.Spade;
                if (cardClass == 'd')
                {
                    selectClass = CardClass.Dia;
                }
                else if (cardClass == 'h')
                {
                    selectClass = CardClass.Heart;
                }
                else if (cardClass == 'c')
                {
                    selectClass = CardClass.Clover;
                }
                string cardNum = selectCards[i].Substring(1);
                if (int.TryParse(cardNum, out int parseCardNum) && 0 <= parseCardNum && parseCardNum <= 13)
                {
                    CardData newCard = new CardData(selectClass, parseCardNum);
                    selecetCardList.Add(newCard);
                }
            }

            CardRule cardRule = new CardRule();
            TMixture mixtureValue = new TMixture();
            if (cardRule.IsVarid(selecetCardList, out mixtureValue) == true)
            {
                CheckSelectCard();
                {
                    putDownList.Clear();
                    for (int i = 0; i < selecetCardList.Count; i++)
                    {
                        putDownList.Add(selecetCardList[i]);
                    }

                }

            }
        }
    }

    private void GiveCardCommand()
    {
        Console.WriteLine("제출할 카드를 골라 주세요 1,2,3,4");
        while (true)
        {
            string card = Console.ReadLine();
            selecetCardList = new();
            if (isMyTurn == false)
            {
                Console.WriteLine("자기 차례가 아닙니다.");
                continue;
            }
            card = card.Replace(" ", "");//공백제거
            string[] selectCards = card.Split(","); //콤마로 구별
            int validCount = 0;
            for (int i = 0; i < selectCards.Length; i++)
            {
                if (Int32.TryParse(selectCards[i], out int selectCard) && (selectCard == 22 || 0 <= selectCard && selectCard < haveCardList.Count))
                {
                    if (selectCard == 22)
                    {
                        Console.WriteLine("패스");
                        validCount++;
                        break; ;
                    }

                    Console.WriteLine($"{haveCardList[selectCard].cardClass}:{haveCardList[selectCard].num} 카드 선택");
                    selecetCardList.Add(haveCardList[selectCard]);
                    validCount++;
                }
                else
                {
                    Console.WriteLine("유효 숫자가 아닙니다.");
                    break;
                }

            }

            if (validCount != selectCards.Length)
            {
                //잘못 입력된게 있어서 패스
                continue;
            }
            if (CheckSelectCard())
            {
                //낼수 있는 카드조합이면 제출
                ReqPutDownCard(selecetCardList);
            }

        }
    }

    private bool CheckSelectCard()
    {
        CardRule cardRule = new CardRule();
        TMixture selectCardValue = new TMixture();
        if (cardRule.IsVarid(selecetCardList, out selectCardValue) == false)
        {
            Console.WriteLine("유효한 조합이 아닙니다.");
            return false;
        }

        //혼자 크기 비교 위해서 아래비교, 내가 제출한건 무조건 전걸로 진행 

        //선택된 카드를 현재 낼 수 있는지 판단해서 bool 반환
        if (gameTurn == 1)
        {
            //첫번째 턴이면 보유한 카드에 스페이드 3 있어야 가능 한걸로 
            foreach (CardData card in selecetCardList)
            {
                if (card.Compare(CardData.minClass, CardData.minRealValue) == 0)
                {
                    return true;
                }
            }
            Console.WriteLine($"첫 시작은 {CardData.minClass}{CardData.minRealValue}을 포함해야함");
            return false;
        }

        //처음이 아니면 내가 낸건지 체크 - 내가 낸거면 자유롭게 내기 가능
        if (CheckAllPass())
        {
            if (selecetCardList.Count == 0)
            {
                Console.WriteLine("올 패스 받은 상태에서 내가 패스는 불가");
                return false;
            }
            return true;
        }


        if (selectCardValue.mixture == EMixtureType.Pass)
        {
            //패스한거면 그냥 통과
            return true;
        }

        //이전것과 비교
        TMixture putDownValue = new TMixture();
        cardRule.CheckValidRule(putDownList, out putDownValue);

        Console.WriteLine($"이전꺼 {putDownValue.mixture}:{putDownValue.mainCardClass}:{putDownValue.mainRealValue}" +
            $"\n제출용 {selectCardValue.mixture}:{selectCardValue.mainCardClass}:{selectCardValue.mainRealValue}:");
        //비교 안되는 타입이면 (앞에 낸것과 다른 유형이면) 실패
        if (cardRule.TryCompare(putDownValue, selectCardValue, out int compareValue) == false)
        {
            Console.WriteLine("이전과 다른 타입이라 잘못된 제출");
            return false;
        }
        //compareValue는 이전꺼에서 현재껄 뺀거 - 즉 양수면 전께 큰거 
        if (compareValue > 0)
        {
            //이전것보다 작아도 실패
            Console.WriteLine("전 보다 작다");
            return false;
        }

        Console.WriteLine("전 보다 크다");
        return true;
    }

    private bool CheckAllPass()
    {
        if (putDownList.Count == 0)
        {
            return false;
        }
     
        Console.WriteLine("올 패스인지 체크");
        giveCardList.Sort();
        putDownList.Sort();

        //정렬해서 냈던 카드가 있으면 올 패스 된거.
        for (int i = 0; i < giveCardList.Count; i++)
        {
            if (giveCardList[0].CompareTo(putDownList[0]) == 0)
            {
                //하나라도 내가 냈던거랑 같으면 내가 냈던거
                Console.WriteLine("올 패스 받았음");
       
                return true;
            }
            return false;
        }

        return false;

    }

    private void ResetPutDownCard()
    {
        putDownList.Clear();
    }

    private void AddPutDownCard(CardData _card)
    {
        putDownList.Add(_card);
    }

    private void CountTurn()
    {
        gameTurn++;
    }
    #endregion


    #region 통신 파트
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
        haveCardList = new();
        for (int i = 2; i < _resDate.Length; i += 2)
        {
            //i번째는 카드 무늬, i+1에는 카드 넘버가 있음
            CardData card = new CardData((CardClass)_resDate[i], _resDate[i + 1]);
            haveCardList.Add(card);
        }
        SetNewGame();
        ConsoleMyCardList();
    }

    private void ConsoleMyCardList()
    {
        for (int i = 0; i < haveCardList.Count; i++)
        {
            //i번째는 카드 무늬, i+1에는 카드 넘버가 있음
            Console.WriteLine($"보유 카드 {haveCardList[i].cardClass} : {haveCardList[i].num}");
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
            for (int infoIndex = i; infoIndex < i + _data[2]; infoIndex++)
            {
                Console.WriteLine(_data[infoIndex] + "번 참가");
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

    #region 카드 제출
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
        //본인의 행위였다면
        if (_data[1] == id)
        {
            //이전에 냈던건 초기화
            giveCardList.Clear(); 
        }
        if (_data[2] == 0)
        {
            //방금 낸 카드가 없으면
            //이전 카드 덮어 쓰지 않고
            //본인의 카드 제거나 이전 카드 기록 안함
            Console.WriteLine("전 사람 패쓰했음");
            return;
        }
        ResetPutDownCard(); //이전 카드 클리어
        Console.WriteLine(_data[1] + " 유저가 제출한 카드");
        for (int i = 3; i < _data.Length; i += 2)
        {
            CardClass cardClass = (CardClass)_data[i];
            int num = _data[i + 1];
            CardData card = new CardData(cardClass, num); //카드 생성
            AddPutDownCard(card);

            //만약 내가냈던 카드면
            if (_data[1] == id)
            {
                for (int myCardIndex = 0; myCardIndex < haveCardList.Count; myCardIndex++)
                {
                    if (haveCardList[myCardIndex].CompareTo(card) == 0)
                    {
                        //내가 보유한 카드에서 제거
                        haveCardList.RemoveAt(myCardIndex);
                        giveCardList.Add(new CardData(cardClass, num));
                        break;
                    }
                }
            }

        }
        if (_data[1] == id)
        {
            ConsoleMyCardList();
        }

    }
    #endregion

    #region 턴 정하기
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
            CheckAllPass();
        }
        CountTurn(); //턴을 지정하는건 새로운 턴이 된거
    }
    #endregion

    #region 채팅 
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
        string convertStr = Encoding.Unicode.GetString(_receiveData, 1, _receiveData.Length - 1);
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
    #endregion
    #endregion
}

