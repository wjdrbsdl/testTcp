using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



public class ClientLogIn
{
    public static byte[] ServerIp = new byte[] { };
    //클라로 접속을 했으면 여기서 아이피 입력하고 접속 대기 및 접속 ip 바꾸기 진행,
    public void EnterIP()
    {
        bool waitingInputIP = true;
        while (waitingInputIP)
        {
            Console.WriteLine("접속할 서버 IP를 입력하세요 ex) 172.30.1.23");
            string ip = Console.ReadLine();
            //올바른 ip인지 체크 후
            if (IsValidForm(ip) == true)
            {
                Console.Clear();
                LobbyClient client = new LobbyClient(ip, 1);
                client.Connect();
                waitingInputIP = false;
            }

        }

    }

    private bool IsValidForm(string ip)
    {
        //ipv4 가정
        string[] intSplit = ip.Split(".");
        //4구역이 나왔나
        if (intSplit.Length != 4)
        {
            return false;
        }
        //모두 숫자이면서 0이상 255 아래인가
        for (int i = 0; i < intSplit.Length; i++)
        {
            if (int.TryParse(intSplit[i], out int num) == false)
            {
                return false;
            }

            if (num < 0)
            {
                return false;
            }

            if (255 < num)
            {
                return false;
            }
        }
        return true;
    }
}
