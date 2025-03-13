using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static study.Program;
using testTcp;

namespace study
{
    class Program
    {
        static bool isServer = true;
        static void Main(string[] args)
        {
            byte[] msg = new byte[8];
            LobbyServer server = new LobbyServer();
            Console.WriteLine("서버 생성은 s 클라 생성은 c 를 입력하세요");
            string select = "";
            while (true)
            {
               // Console.WriteLine("메인 와일문");
                string input = Console.ReadLine();
                if (input == "s")
                {
                    Console.WriteLine("s 받음");
                    select = input;
                    break;
                }
                else if (input == "c")
                {
                    Console.WriteLine("c 받음");
                    select = input;
                   
                    break;
                }
            }
            Console.WriteLine("메인함수 빠져나감");
            if(select == "s")
            {
                server.Start();
            }
            else
            {
                isServer = false;
                ClientLogIn claLogIn = new ClientLogIn();
                claLogIn.EnterIP();
            }

            //연결이 완료됐다면 전송하는 코드
            while (true)
            {
              
            
              
                if (isServer)
                {
                  //  Console.WriteLine("프로그램 와일문");
                    string exit = Console.ReadLine();
                    string a = " " + exit;
                    byte[] ubytes = System.Text.Encoding.Unicode.GetBytes(a);
                    if (exit == "q")
                    {
                        if (isServer)
                        {
                            server.Close();
                        }
                    }

                    server.Send(ubytes);
                }
               
            }
            
            //client.Send(msg);

        }
    }

}