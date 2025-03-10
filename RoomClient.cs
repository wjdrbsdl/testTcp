﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


public class RoomClient
{
    public Socket clientSocket;
    public int port = 5000;
    public string ip;
    public int id;
    public MeetState meetState = MeetState.Lobby;

    public RoomClient(string _ip, int _id)
    {
        ip = _ip;
        id = _id;
    }

    public void Connect()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress ipAddress = IPAddress.Parse(ip);
        IPEndPoint endPoint = new IPEndPoint(ipAddress, port);
        clientSocket.BeginConnect(endPoint, CallBackConnect, clientSocket);
        Update();
    }

    private void CallBackConnect(IAsyncResult _result)
    {
        //연결 되었으면 자료 받을 준비, 상태 준비
        Console.WriteLine("클라 연결 콜백");
        byte[] buff = new byte[100];
        clientSocket.BeginReceive(buff, 0, buff.Length, 0, CallBackReceive, buff);
    }

    private void CallBackReceive(IAsyncResult _result)
    {
        Console.WriteLine("클라 리십 콜백");
        byte[] receiveBuff = _result.AsyncState as byte[];

        ReqType reqType = (ReqType)receiveBuff[0];
        if (reqType == ReqType.RoomMake)
        {
            ResRoomJoin(receiveBuff);
        }

        clientSocket.BeginReceive(receiveBuff, 0, receiveBuff.Length, 0, CallBackReceive, receiveBuff);
    }

    private void ReqRoomJoin()
    {
        Console.WriteLine("참가 신청");
        string roomName = "테스트로 아무거나 써보기";
        byte[] roomByte = Encoding.Unicode.GetBytes(roomName);
        byte[] reqRoom = new byte[roomByte.Length + 1];
        Array.Copy(roomByte, 0, reqRoom, 1, roomByte.Length); //룸 네임 전체를 요청 바이트 1번째부터 복사시작
        reqRoom[0] = (byte)ReqType.RoomMake;
        clientSocket.Send(reqRoom);
    }

    private void ResRoomJoin(byte[] _receiveData)
    {
        Console.WriteLine("방에 대한 정보를 받음" + _receiveData[1]);
        ReqType resType = (ReqType)_receiveData[0];
        if (resType == ReqType.RoomMake)
        {
            //룸메이크 요청에 대한 대답이라면
            //[1] 이 응답
            //[2] 가 참가인원
            //[3] 부터 참가 인원 넘버링 
            string parti = "참가 번호 : ";
            for (int i = 0; i < _receiveData[2]; i++)
            {
                parti += _receiveData[i + 3].ToString() + " ";
            }
            Console.WriteLine(parti);
        }
    }

    private void Update()
    {
        while (true)
        {
            if (meetState == MeetState.Lobby)
            {
                string command = Console.ReadLine();
                if (command == "j")
                {
                    ReqRoomJoin();
                }

            }
        }
    }

}
