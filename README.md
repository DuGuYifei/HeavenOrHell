# HeavenOrHell Server

A game server implementation using KCP protocol for reliable UDP communication.

## Building the Project

1. Generate C++ code from Protocol Buffer definitions:
   ```bash
   chmod +x proto2cpp.sh
   ./proto2cpp.sh
   ```

2. Build the project:
   ```bash
   chmod +x make.sh
   ./make.sh
   ```

## Components

- **Server**: The main KCP server implementation that handles game logic and client connections
- **KCP Client**: A test client implementation for sending messages and receiving responses from the server. Useful for testing server functionality and message handling.
- The **conv** variable in client will decide how to connect to server.

## Notes

- Run `proto2cpp.sh` before building if you modify any `.proto` files
- The generated Protocol Buffer code is in `src/message/gen/` and is ignored by git 
- In the submodule of `src/message`, put all enums will be used here. Then everyone can know what happened