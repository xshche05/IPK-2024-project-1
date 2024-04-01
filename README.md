# Ipk24ChatClient
Project from IPK 23/24 course in FIT VUT, simple console chat client app.
## Table of Contents
- [Ipk24ChatClient](#ipk24chatclient)
    - [Introducing](#introducing)
    - [Getting Started](#getting-started)
    - [Architecture Overview](#architecture-overview)
    - [Features & Functionalities](#features--functionalities)
        - [Basic functionality](#basic-functionality)
        - [Additional functionality and features](#additional-functionality-and-features)
    - [Code Overview](#code-overview)
        - [FSM logic](#fsm-logic)
        - [Input logic](#input-logic)
        - [TCP and UDP logic](#tcp-and-udp-logic)
            - [Some UDP implementation details](#some-udp-implementation-details)
    - [Testing & Validation](#testing--validation)
        - [Tools](#tools)
        - [Input testing](#input-testing)
        - [TCP testing](#tcp-testing)
        - [UDP testing](#udp-testing)
        - [General testing for both protocols](#general-testing-for-both-protocols)
        - [Other tests](#other-tests)
## Introducing
This document describes the chat client that uses `IPK24-CHAT` protocol for communication. Application support communication with server via TCP and UDP. Chat client is using console as an user interface, all commands and message inputs are put to `stdin`, all application outputs are printed to `stdin` in case of messages and success replies from server, outputs such as user warnings, app errors, server errors and failure replies are printed to `stderr`.

## Getting Started
App uses `.NET 8.0` as main development platform and all source codes are written in `C#`. The whole solution is using only base SDK libraries. App is consist from several task to process different things in the same time. To run an application use the minimum following command:
```shell
./ipk24chat-client -t <udp|tcp> -s <hostname|IP>
``` 
Mandatory options:
- `-t <protocol>` - communication protocol `udp` or `tcp` (case insensitive)
- `-s <host>` - server address, IPv4 or domain name

Other supported optional command line options:
- `-p <port>` - server port (`uint16`) specification, default port is `4567`
- `-d <timeout>` - UDP connection timeout (`uint16`) in *ms*, default value is `250 ms`
- `-r <retries>` - UDP connection retries (retransmit) number (`uint8`), default value is `3`
- `--ts` - enable timestamps for output messages in following format `HH:mm:ss`
- `-c` - enable application output coloring
- `-h` - prints application guidance and exits

## Architecture Overview
Whole app is implemented on top of transport protocols such as TCP [RFC9293] and UDP [RFC768] and IPv4 network layer, includes following main modules such as user I/O processing, client FSM, client UDP module and client TCP module. UDP and TCP modules based on some interfaces, to perform polymorphism concept of OOP and to insure compatibility between UDP and TCP clients. To insure easy packet construction is also used builder design pattern, to perform packets' assembly.

## Features & Functionalities
### Basic functionality
As basic functionality described app provides simple interface to perform client <-> server communication using the `IPK24-CHAT` protocol above TCP or UDP communication protocols. User can perform some predefined commands and send / receive messages. All user commands starts with `/` symbol and can accept zero o several arguments. In case of unsupported command or invalid command syntax user gets an warning with problem descriptions. Other inputs which doesn't starts with slash symbol are treated as user input messages. Application termination possible by performing `Ctrl+C` or `Ctrl+D` (sending `EOF` as input), in this case app clean all resources sends `BYE`, gracefully disconnects and exits with `0` code. App also will terminate by reaching `END` state. In case of reaching `END` through `ERROR` state or UDP max retries reached all resources are cleaned and process exits with code `1`.

Basic available commands:
- `/help` - prints input guidance to `stdout`
- `/auth <username> <secret> <display_name>` - user authentication command, after authentication user public name is set to `<display_name>`
- `/join <channel>` - join (create if doesn't exists) to specified channel
- `/rename <new_display_name>` - changing the display name

### Additional functionality and features
Additionally application supports a few user friend features such as colored output according to message type (`-c` option), message timestamps (`--ts` option) and following additional command:
- `/currentname` - which tells you your current display name

How message color is connected to message type describes the following table:

| Color    | Message meaning                                        |
|:----------:|:--------------------------------------------------------:|
| `White`  | User input messages and commands                       |
| `Green`  | Received messages from server                          |
| `Red`    | Fatal app and server errors, failure replies           |
| `Yellow` | User warnings, such as invalid command                 |
| `Cyan`   | Success replies, help and current name commands output |

## Code Overview
### FSM logic
General FSM logic is implemented in `ClientFsm.cs` and have several functions such as state changing, state changing according to got packet from server, and user input validator according to the current state. Client termination logic is implemented inside state changing function, in case of changing state to `END`, client send `BYE` packet and release termination semaphore to stat termination process. During the termination process client waits for last packet send task and wait for packet printer task termination to insure every got packet successful output.
### Input logic
Input logic is implemented in `InputProcessor.cs` and process all user inputs such as commands and messages. It is simply task which is constantly reads user's inputs and build packets to send to the server or perform some other actions. One of the main thing in user processing is to preform processing block if case of waiting for authentication reply. For block implementation is simply used one semaphore which blocks input processing until any authentication reply received.
### TCP and UDP logic
General client logic such as sending, receiving, processing packets is implemented in `UdpChatClient.cs`, `UdpPacket.cs`  and `TcpChatClient.cs`, `TcpPacket.cs` for UDP and TCP transport protocols respectively. Chat clients are based on `IChatClient` interface, packets are based on `IPacket` interface and packet builders are based on `IPacketBuilder` interface. It uses interfaces to insure same general processing logic for both UDP and TCP protocols. Every described process logic has its own task to perform parallel and asynchronous processing. This allow to separate data sending and receiving how do such network apps require it. Sending logic is based on queuing packets to the send queue and sending each packet after successful sending of previous packet.
#### Some UDP implementation details
In case of UDP client we need to insure getting the confirmations for every send packet, sender task is simply gets packet from send queue, and starts trying to send required packet with provided timeout and number of retries. Timeout waiting is simply implemented by using two parallel tasks and `blockingCollection<int>` to block `checkTask` from return until some confirm would be received .
```c#
int controlId = -1;  
Task checkTask = Task.Run(() => { controlId = _confirmedMessages.Take(); }); // Confirmation waiter  
Task delayTask = Task.Delay(SysArgParser.Config.Timeout); // Timeout timer  
Task firstCompleted = await Task.WhenAny(checkTask, delayTask); // Wait for confirm message or timeout  
if (firstCompleted == checkTask && checkTask.IsCompletedSuccessfully && controlId == id)  
{  
  // Message confirmed  
  break; // Break
}
```
In case of not getting confirmation and max retries reaching, client writes `ERR` message, go to `END` state and gracefully terminates an application with `1` exit code.

## Testing & Validation
### Tools
In this topic would be described application testing and validation. During the testing process was used such tools as `Wireshark` application to handle and analyze transmitted packets, additional Lua script to detect `IPK24-CHAT` packets, `netcat` application to perform simple server <-> client communication, reference IPK server `anton5.fit.vutbr.cz:4567`.
### Input testing
Firsts of all was tested user input validation, such as commands and messages. Following test cases was tested:
- User command syntax validation
- Handling invalid user command which is not part of command set
- Handling unacceptable commands (messages) in current client state
- Client input processing blocking in case of waiting for authentication reply
- Message content validation to insure that all messages contains only allowed characters and no other symbols became a part of message packet
### TCP testing
During the TCP testing following cases were tested:
- Case insensitivity in case of provided ABNF [RFC5234] grammar
- Correct packet separation in case of byte stream read return more than one packet
- Handling unterminated packet
- ABNF message grammar structure validation
- Server unavailable reaction
- Gracefully TCP socket disconnection
### UDP testing
UDP testing consists of following cases:
- Minimal packet length validation
- Packet type byte validation, type byte is in defined set
- Timeout reaction
- Max retries reach reaction
- Packet delay reaction
- Packet loss reaction
- Correct server end point port changing after authentication
### General testing for both protocols
Both protocols testing contains following cases:
- Server error handling
- Server bye handling
- Invalid packets
- Invalid content of particular packet item, such as display name, message content
### Other tests