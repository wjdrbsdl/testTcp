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
        ArrangeTurn

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
            public ClaInfo(int _bufferSize, Socket _claSocket)
            {
                buffer = new byte[_bufferSize];
                workingSocket = _claSocket;
               
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
                GameStartShuffleCard(); //카드 나눠주고
                AnnounceTurnPlayer(); //누가시작인지 알려줌
            }
            else if(reqType == ReqRoomType.PutDownCard)
            {
                AnnoucePutDownCard(_reqData); //카드제출 요청 받으면, 다른유저들에게 어떤 카드가 나왔는지 알려줌
                AnnounceTurnPlayer(); //다음 차례 지정해줌 - 위의 풋다운에서 다음 차례 찾아놓음
            }
        }

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

        private void GameStartShuffleCard()
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
                    if (selectCard.Compare(CardClass.Spade, 3) == 0)
                    {
                        turnId = roomUser[userIndex].ID; //서버에서 누가 시작인지 알고 있을것. 
                    }
                }
                byte[] cardByte = cardList.ToArray();
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
            for (int i = 0; i < roomUser.Count; i++)
            {
                roomUser[i].workingSocket.Send(partyDataByte);
            }
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
            //주 역할은 다음 차례 역할 지정하기 
            int turnIndex = 0;
            for (int i = 0; i < roomUser.Count; i++)
            {
                if (roomUser[i].ID == _putDownCardData[1])
                {
                    turnIndex = i;
                }
              
                //낸 유저를 포함 모든 유저들에게 해당 유저가 어떤걸 냈는지 전달.
                roomUser[i].workingSocket.Send(_putDownCardData);
            }
            //현재 차례였던애 turnIndex;
            turnId = roomUser[(turnIndex + 1) % roomUser.Count].ID;
        }

        private void AnnounceTurnPlayer()
        {
            ColorConsole.ConsoleColor("턴 지정 " + turnId.ToString());
            byte[] turnData = new byte[] { (byte)ReqRoomType.ArrangeTurn, (byte)turnId };
            for (int i = 0; i < roomUser.Count; i++)
            {
                roomUser[i].workingSocket.Send(turnData);
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
    }

}
