using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace testTcp
{
    public enum ReqRoomType
    {
        Ready, Start, RoomOut, Chat, 
        IDRegister, PartyData,
        ShuffleCard, PutDownCard,
        ArrangeTurn,
        StageReady, StageOver, GameOver


    }

    public class PlayServer
    {
        public List<ClaInfo> roomUser = new();
        public Socket linkSocket; //룸유저 받아들이는 소켓
        public Socket linkStateLobby; //서버로비와 소통하는 소켓 -> 방 상태 바뀔때 신호 
        public Socket linkRoomCount; //서버로비와 소통하는 소켓 -> 방 인원 바뀔때 신호 
        public int number = 0;
        public string roomName;
        public int turnId;
        //방장이 호스트가 되어 해당 방에있던 유저들의 입력을 받고 처리 
        #region 연결 및 소켓 세팅
        public void Start()
        {
            ColorConsole.ConsoleColor("룸 서버 시작");
            linkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 5001);
            linkSocket.Bind(endPoint);
            linkSocket.Listen(4);

            linkSocket.BeginAccept(AcceptCallBack, null);
        }

        public class ClaInfo
        {
            public byte[] buffer;
            public Socket workingSocket;
            public int ID = 0; //0이면 아이디를 전달받지 못한 상태
            public int HaveCard = 0;
            public int BadPoint = 0; 

            public ClaInfo(int _bufferSize, Socket _claSocket)
            {
                buffer = new byte[_bufferSize];
                workingSocket = _claSocket;
               
            }

            public void ResetScore()
            {
                HaveCard = 0;
                BadPoint = 0;
            }
          
        }

        private void AcceptCallBack(IAsyncResult _result)
        {
            try
            {
                ColorConsole.ConsoleColor("룸 서버 수락");
                Socket client = linkSocket.EndAccept(_result);
                ClaInfo newCla = new ClaInfo(100, client);
                number++;
                roomUser.Add(newCla);
                client.BeginReceive(newCla.buffer, 0, newCla.buffer.Length, 0, DataReceived, newCla);
                SendRoomCount(); //변경한 인원 전달
                linkSocket.BeginAccept(AcceptCallBack, null); //다른거 받을 준비 
            }
            catch 
            {
                Console.WriteLine("");
            }

        }

        void DataReceived(IAsyncResult ar)
        {
            try
            {
               // ColorConsole.ConsoleColor("룸     서버로서 리시브");
                ClaInfo cla = (ClaInfo)ar.AsyncState;
                byte[] recevieBuff = cla.buffer;
                int received = cla.workingSocket.EndReceive(ar);
                byte[] validData = new byte[received];
                Array.Copy(cla.buffer, 0, validData, 0, received);
                //SendChat(cla.buffer, cla.ID);
                ////Send(obj.Buffer);
                //HandleRoomMaker(obj, buffer);
                HandleReq(cla, validData);


                //obj.ClearBuffer();
                cla.workingSocket.BeginReceive(cla.buffer, 0, cla.buffer.Length, 0, DataReceived, cla);
           
            }
            catch 
            {

            }

        }
        #endregion

        private void HandleReq(ClaInfo _claInfo, byte[] _reqData)
        {
            ReqRoomType reqType = (ReqRoomType)_reqData[0];
            
            if(reqType == ReqRoomType.IDRegister)
            {
                _claInfo.ID = _reqData[1];
                AnnouceParty();
            }
            else if (reqType == ReqRoomType.Chat)
            {
                SendChat(_reqData, _claInfo.ID);
            }
            else if(reqType ==ReqRoomType.RoomOut)
            {
                ExitClient(_reqData);
                SendRoomCount();
            }
            else if(reqType == ReqRoomType.Start)
            {
                //방장으로 부터 게임 시작을 호출 받으면 
                UserScoreReset(); //유저 점수 리셋
                AnnouceCardArrange(); //카드 나눠주고
                AnnounceTurnPlayer(); //누가시작인지 알려줌
            }
            else if(reqType == ReqRoomType.PutDownCard)
            {
                //1. 다른유저들에게도 제출된 카드 공지
                AnnoucePutDownCard(_reqData); 
                //2. 해당 유저가 모든 패를 털었는지 체크
                if(CheckStageOver(_reqData) == false)
                {
                    //해당 판이 안끝났으면 계속 진행
                    AnnounceTurnPlayer(); //다음 차례 지정해줌 - 위의 풋다운에서 다음 차례 찾아놓음
                    return;
                }
                //3. 판이 끝났다면 벌점 계산
                CalBadPoint();
                bool gameOver = CheckGameOver(); //벌점에 따라 게임이 끝났는지 체크
                //4. 게임오버인지 체크
                if(gameOver == false)
                {
                    //아직 게임 오버가 아니라면
                    AnnouceStageOver(); //스테이지 끝났다고 알리고
                    ReadyNexStage(); //다음 스테이지 준비
                    return;
                }
                //5. 점수 정산 게임 대기 상태로 전환
                AnnouceGameOver();
                ReadyGameStart();
            }
            else if(reqType == ReqRoomType.StageReady)
            {
                ResStageReadyPlayer(_reqData);
            }
        }

        private void UserScoreReset()
        {
            for (int i = 0; i < roomUser.Count; i++)
            {
                roomUser[i].ResetScore();
            }
        }

        #region 벌점, 스테이지, 게임 종료 체크
        private void CalBadPoint()
        {
            for (int i = 0; i < roomUser.Count; i++)
            {
                int restCard = roomUser[i].HaveCard;
                if(restCard >= 1)
                {
                    roomUser[i].BadPoint += roomUser[i].HaveCard; //남은 장수 만큼 벌점 진행
                }
                
            }
        }

        int overBadPoint = 13; //
        private bool CheckGameOver()
        {
            //게임 오버 체크 한유저라도 벌점 
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].BadPoint >= overBadPoint)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckStageOver(byte[] _putDownCardData)
        {
            /*
             * [0] 요청 코드 putdownCard
             * [1] 플레이어 id
             * [2] 낸 카드 숫자
             * [3] 카드 구성
           */

            //제출했던 유저 아이디
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].ID == _putDownCardData[1])
                {
                    roomUser[i].HaveCard -= _putDownCardData[2];

                    if (roomUser[i].HaveCard == 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }
        #endregion

        #region 게임 진행
        Queue<int> reqStagePlayer = new();
        List<int> confirmStagePlayer = new();
        int curUserCount;

        private void ShuffleCard()
        {

        }

        private void ReadyNexStage()
        {
            //스테이지가 끝나면 큐를 청소하고,
            //매 프레임마다 큐를 확인해서 준비가 된 id를 확인 
            //중복 id 없이 정원 4명이 모이면 다음 스테이지 시작
            reqStagePlayer.Clear();
            confirmStagePlayer.Clear();
            curUserCount = roomUser.Count; //룸유저 카운트로 ? 
            Task.Run(() =>
            {
                while (confirmStagePlayer.Count < curUserCount)
                {
                    if(reqStagePlayer.TryDequeue(out int id))
                    {
                        if(confirmStagePlayer.IndexOf(id) == -1)
                        {
                            ColorConsole.ConsoleColor(id + "준비 확인");
                            confirmStagePlayer.Add(id);
                        }
                    }
                }
                ColorConsole.ConsoleColor(curUserCount +"명의 준비 확인 다음 스테이지 시작");
                AnnouceCardArrange();
                AnnounceTurnPlayer();
            });


        }

        private void ResStageReadyPlayer(byte[] _reqStageReady)
        {
            //유저가 다음판 할 준비 되었다고 알리기 
            /*
             * [0] 요구코드 stageReady
             * [1] 내 아이디
             */
            //해당 유저가 준비되었다고 요청이 오면, 모두 준비가 끝났는지 확인할 수 있게 큐에 집어넣음.
            //ReadyNextStage에서 계속해서 확인할거임.
            reqStagePlayer.Enqueue(_reqStageReady[1]);
        }

        private void ReadyGameStart()
        {
            //게임 시작 위해 레뒤 같은거 대기하기?
        }
        #endregion

        private void AnnouceGameOver()
        {
            //게임 오버시 전달할것들
            roomUser.Sort((a, b) => a.BadPoint.CompareTo(b.BadPoint)); //벌점 오름순으로 정렬
            /*
             * [0] 종료코드 GameOver
             * [1] 유저수 - 순위대로 정렬
             * [2] 보낼 정보 데이터 길이 일단 2
             * [3] 유저 ID
             * [4] 유저 벌점
             */
            List<byte> overDate = new();
            overDate.Add((byte)ReqRoomType.GameOver);
            overDate.Add((byte)roomUser.Count);
            overDate.Add(2); //보낼 데이터 길이
            for (int i = 0; i < roomUser.Count; i++)
            {
                overDate.Add((byte)roomUser[i].ID);
                overDate.Add((byte)roomUser[i].BadPoint);
            }
            SendMessege(overDate.ToArray());
        }

        #region 통신
        private void SendChat(byte[] msg, int _receiveNumbering)
        {
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].ID == _receiveNumbering)
                {
                    msg[1] = (byte)' ';
                }
                else
                {
                    msg[1] = (byte)_receiveNumbering;
                }
                Socket socket = roomUser[i].workingSocket;
                if (socket.Connected)
                {
                    socket.Send(msg);
                }

            }
        }

        private void ExitClient(byte[] _receiveData)
        {
            ColorConsole.ConsoleColor($"{_receiveData[1]}번 아이디가 나가길 요청 현재인원 :" + roomUser.Count);
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].ID == _receiveData[1])
                {
                    roomUser.RemoveAt(i);
                }
            }
            Console.WriteLine(roomUser.Count);
         
        }

        private void AnnouceCardArrange()
        {
            //카드를 섞어서 각 플레이어에게 나눠주고, 순서를 지정해준다. 
            ColorConsole.ConsoleColor("게임 카드 나눠주기");
            CardData[] cards = new CardData[52];
            int index = 0;
            for (int cardClass = 0; cardClass < 4; cardClass++)
            {
                for (int cardNum = 1; cardNum <= 13; cardNum++)
                {
                    CardData card = new CardData((CardClass)cardClass, cardNum);
                    cards[index] = card;
                    index++;
                }
            }
            //섞었다 치고

            //4 명의 유저에게
            int giveCardIndex = 0;
            for(int userIndex = 0; userIndex < roomUser.Count; userIndex++)
            {
                // (byte)카드클래스, (byte)num 순으로 해당 리스트에 추가
                List<byte> cardList = new();
                cardList.Add((byte)ReqRoomType.Start);//요청 코드
                cardList.Add((byte)13);//줄 숫자
                /*
                 * [0] 요청코드 게임스타트
                 * [1] 카드 숫자
                 */
                //13장씩
                for (int cardCount = 1; cardCount <= 13; cardCount++)
                {
                    CardData selectCard = cards[giveCardIndex];
                    giveCardIndex++;
                    cardList.Add((byte)selectCard.cardClass);
                    cardList.Add((byte)selectCard.num);
                    if (selectCard.Compare(CardData.minClass, CardData.minRealValue) == 0)
                    {
                        turnId = roomUser[userIndex].ID; //서버에서 누가 시작인지 알고 있을것. 
                    }
                }
                byte[] cardByte = cardList.ToArray();
                roomUser[userIndex].HaveCard = 13; //최초 13장으로 세팅.
                roomUser[userIndex].workingSocket.Send(cardByte);
            }

        }

        private void AnnouceParty()
        {
            ColorConsole.ConsoleColor("유저 방 참가를 알려줌");
            /*
             * [0] 응답코드 PartyIDes,
             * [1] ID를 받은 유효한 파티원 수
             * [2] 각 파티원 정보 길이
             * [3] 0번 파티원부터 정보 입력
             */
            List<byte> partyData = new();
            int partyInfoLength = 1; //일단 id값만 넘기기 때문에 임시로 1개
            partyData.Add((byte)ReqRoomType.PartyData);
            partyData.Add((byte)roomUser.Count); //일단 모든 파티원 수 담아놓음
            partyData.Add((byte)partyInfoLength);
            byte valid = 0;
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].ID != 0)
                {
                    valid += 1;
                    partyData.Add((byte)roomUser[i].ID);
                }
            }
            partyData[1] = valid; //아이디가 밝혀진 애들로 수 조절해서 전달
            byte[] partyDataByte = partyData.ToArray();
            SendMessege(partyDataByte);
        }

        private void AnnoucePutDownCard(byte[] _putDownCardData)
        {
            /*
              * [0] 요청 코드 putdownCard
              * [1] 플레이어 id
              * [2] 낸 카드 숫자
              * [3] 카드 구성
            */
            ColorConsole.ConsoleColor("어떤 카드 냈는지 표기");
            //전체 데이터에서 해당 유저가 가진 카드를 빼도 되고
            //다음 차례 역할 지정하기 
            //해당 유저가 보유한 카드에서 감소
            int turnIndex = 0;
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].ID == _putDownCardData[1])
                {
                    //제출한 녀석을 찾아서
                    turnIndex = i; //얘 차례 인덱스 뽑고
                }
              
                //낸 유저를 포함 모든 유저들에게 해당 유저가 어떤걸 냈는지 전달.
                roomUser[i].workingSocket.Send(_putDownCardData);
            }
            //현재 차례였던애 turnIndex;
            turnId = roomUser[(turnIndex + 1) % roomUser.Count].ID;
        }

        private void AnnouceStageOver()
        {
            /*
             * [0] 응답코드 스테이지오버
             * [1] 사람 수 - 4
             * [2] 아이디
             * [3] 보유 카드 수 반복
             */
            ColorConsole.ConsoleColor("해당 판 종료 공지");
            List<byte> stageOver = new();
            stageOver.Add((byte)ReqRoomType.StageOver);
            stageOver.Add((byte)roomUser.Count); //4명
            //사람이 나가서 AI로 해도 roomuser는 4명으로?
            for (int i = 0; i < roomUser.Count; i++)
            {
                stageOver.Add((byte)roomUser[i].ID);
                stageOver.Add((byte)roomUser[i].HaveCard);
            }
            byte[] announceData = stageOver.ToArray();
            SendMessege(announceData);
        }

        private void AnnounceTurnPlayer()
        {
            ColorConsole.ConsoleColor("턴 지정 " + turnId.ToString());
            byte[] turnData = new byte[] { (byte)ReqRoomType.ArrangeTurn, (byte)turnId };
            SendMessege(turnData);
        }

        private void SendMessege(byte[] _messege)
        {
            for (int i = 0; i < roomUser.Count; i++)
            {
                roomUser[i].workingSocket.Send(_messege);
            }
        }

        #region 룸 상태 변경 전달
        public void SendRoomState()
        {
            IPEndPoint endPoint = new IPEndPoint(LobbyServer.ServerIp, 5000);
            linkStateLobby = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            linkStateLobby.BeginConnect(endPoint, LobbyConnectCallBack, linkStateLobby);
        }

        public void LobbyConnectCallBack(IAsyncResult _result)
        {
            try
            {
               
                ReqChageRoomState();    
            }
            catch
            {
                ColorConsole.ConsoleColor("방 서버의 로비서버 접속 실패");
            }
        }

        public void ReqChageRoomState()
        {
            byte[] reqChangeRoomState = new byte[] { (byte)ReqLobbyType.RoomState, (byte)RoomState.Ready };
            linkStateLobby.Send(reqChangeRoomState);
            linkStateLobby.Close();
            linkStateLobby.Dispose();
        }
        #endregion

        #region 룸 인원 변경 전달
        public void SendRoomCount()
        {
            IPEndPoint endPoint = new IPEndPoint(LobbyServer.ServerIp, 5000);
            linkRoomCount = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            linkRoomCount.BeginConnect(endPoint, RoomCountCallBack, linkRoomCount);
        }

        public void RoomCountCallBack(IAsyncResult _result)
        {
            try
            {
               // Console.WriteLine("방 서버의 로비서버에 인원 변경 전달");
                ReqChangeRoomCount();
            }
            catch
            {
                ColorConsole.ConsoleColor("방수 변경 콜백 실패");
            }
        }

        public void ReqChangeRoomCount()
        {

            ColorConsole.ConsoleColor("방 인원 변경 함수 호출");
            /*
             * [0] 요청타입
             * [1] 현재 유저 수
             * [2] 방 이름 길이
             * [3] 부터 방 제목들
             */

            byte[] name = Encoding.Unicode.GetBytes(roomName);
            byte[] roomCode = new byte[] { (byte)ReqLobbyType.RoomUserCount, (byte)roomUser.Count, (byte)name.Length };
            byte[] resRoomCount = roomCode.Concat(name).ToArray();

            linkRoomCount.Send(resRoomCount);
            linkRoomCount.Close();
            linkRoomCount.Dispose();
        }
        #endregion

        #endregion
    }

}
