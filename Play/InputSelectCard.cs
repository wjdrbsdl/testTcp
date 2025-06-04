using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class InputSelectCard
{
    public PlayClient pClient;
    public bool isPlaying = true;
    private List<CardData> selectCardList;
    private List<CardData> haveCardList;

    public InputSelectCard(PlayClient _pClient, List<CardData> _haveList)
    {
        pClient = _pClient;
        haveCardList = _haveList;
        selectCardList = new();
    }

    public void Update()
    {
        Task.Run(() =>
        {
            while (isPlaying)
            {
                string inputStr = Console.ReadLine();
                if(inputStr == "gameover")
                {
                    SendGameOver();
                    break;
                }

                selectCardList.Clear();
                if (CheckValidInput(inputStr) == false)
                {

                    continue;
                }
                SendSelectCard();
            }
            Console.WriteLine("게임 종료 입력 나감");
        });
    }

    private bool CheckValidInput(string cardStr)
    {
        //잘 골랐는지 체크
        cardStr = cardStr.Replace(" ", "");//공백제거
        string[] selectCards = cardStr.Split(","); //콤마로 구별
        int validCount = 0;
        for (int i = 0; i < selectCards.Length; i++)
        {
            if (Int32.TryParse(selectCards[i], out int selectCard) && (selectCard == 22 || 0 <= selectCard && selectCard < haveCardList.Count))
            {
                if (selectCard == 22)
                {
                    ColorConsole.Default("패스");
                    validCount++;
                    break; ;
                }

                ColorConsole.Default($"{haveCardList[selectCard].cardClass}:{haveCardList[selectCard].num} 카드 선택");
                selectCardList.Add(haveCardList[selectCard]);
                validCount++;
            }
            else
            {
                ColorConsole.Default("유효 숫자가 아닙니다.");
                break;
            }

        }

        if (validCount != selectCards.Length)
        {
            //잘못 입력된게 있으면 실패
            return false;
        }
        return true;
    }


    public void SendSelectCard()
    {
        //입력 유효 체크

        pClient.PutDownCards(selectCardList);
    }

    public void SendGameOver()
    {
        pClient.TestGameOver();
    }

}
