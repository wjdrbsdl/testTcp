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
            Server server = new Server();
            Client client = new Client();
            while (true)
            {
                string input = Console.ReadLine();
                if (input == "s")
                {
                    Console.WriteLine("s 받음");

                    server.Start();
                    break;
                }
                else if (input == "c")
                {
                    Console.WriteLine("c 받음");
                    isServer = false;
                    client.Connect();
                    break;
                }
            }
     

            //연결이 완료됐다면 전송하는 코드
            while (true)
            {
                string exit = Console.ReadLine();

                if(exit == "q")
                {
                    if (isServer)
                    {
                        server.Close();
                    }
                    else
                    {

                        client.Close();
                    }
                }

                string a = " "+exit;
                byte[] ubytes = System.Text.Encoding.Unicode.GetBytes(a);
       //     출처: https://timeboxstory.tistory.com/120 [매 시간 담기는 이야기 상자:티스토리]
            
                if (isServer)
                {
                    server.Send(ubytes);
                }
                else
                {
                
                    client.Send(ubytes);
                }
                    
            }
            
            //client.Send(msg);

        }
    }

}