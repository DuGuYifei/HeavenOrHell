# README-workflow

1. client send `HelloMessage` with conv id = 0
2. server check in `UDP` by check it as `KCP` msg with conv id
   1. if conv id = 0 means new client
   2. if conv id != 0, check session
3. client check its own conv variable. 
   1. When receive `RoomMessage`, if 0, setup new conv id.