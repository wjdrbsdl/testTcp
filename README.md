# tcp.socket 으로 채팅 만들어 보기

기능 요약
---
서버 
 클라이언트의 연결을 수락하고, 전달받은 메세지를 모든 클라이언트에게 전하는 역할

 클라이언트
  서버를 통해 메시지를 주고 받아 채팅 하는 역할


과정
---
* 코드 이해
> 1. 뼈대 복붙 https://hihaoun.tistory.com/entry/c%EC%9D%84-%EC%9D%B4%EC%9A%A9%ED%95%9C-TCPIP-%EC%84%9C%EB%B2%84-%ED%81%B4%EB%9D%BC%EC%9D%B4%EC%96%B8%ED%8A%B8-%EC%86%8C%EC%BC%93Socket%ED%86%B5%EC%8B%A0
> 2. 구조 공부 : 데이터 파이프 라인 생성 구조, 데이터 송 수신 받는 방법
> 3. 함수 이해 : tcp.beginAccept, AcceptCallBack, tcp.beginConnect 등 기능 함수로 보이는 것들을 공식 문서와 블로그 설명, 디버깅 등으로 이해
* 기능 구현
>1. 반복 수신 기능 추가 : 지속적인 수신을 위해서 DataReceived() 의 끝에 beginReceive에 해당 함수를 콜백으로 넣어 다시 받는 상태로 호출
>2. 채팅 기능 구현 : 서버는 받은 msg를 모든 클라이언트에게 뿌리고, 클라는 받은 msg를 유니코드 string으로 변환하여 출력
>3. 클라이언트 구별 : 서버에서 연결 수락시 각 소켓에 number를 부여하여 메시지 보낸자를 구별, msg.buffer에 담아서 전달
>4. 종료 문제 해결: 접속 중에 창을 끄면 상대쪽에 오류 발생, beginReceive상태에서 null로 인함을 확인 DataReceived에 try, catch 추가 - 서버인 경우엔 해당 소켓을 제거, 클라에선 다시 접속 요청을 구현 (모든 원인은 소켓 종료로 가정)
* 연습
>직접 작성해보기
 
* 추가 수정
 다른곳서 테스트를 위해 현재 Ip 파싱 함수 추가 해서 클라이언트 endPoint 설정
