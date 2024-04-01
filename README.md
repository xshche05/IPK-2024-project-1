# Ipk24ChatClient
Project from IPK 23/24 course in FIT VUT, simple console chat client app.
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
Whole app is implemented on top of transport protocols such as TCP [RFC9293] and UDP [RFC768] and IPv4 network layer, includes following main modules such as user I/O processing, client FSM, client UPD module and client TCP module. UPD and TCP modules based on some interfaces, to perform polymorphism concept of OOP and to insure compatibility between UPD and TCP clients. To insure easy packet construction is also used builder design pattern, to perform packets' assembly.

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
### Input logic
Input logic is implemented in `InputProcessor.cs` and process all user inputs such as commands and messages. It is simply task which is constantly reads user's inputs and build packets to send to the server or perform some other actions. One of the main thing in user processing is to preform processing block if case of waiting for authentication reply. For block implementation is simply used one semaphore which blocks input processing until any authentication reply received.
### TCP and UPD logic
General client logic such as sending, receiving, processing packets is implemented in `UpdChatClient.cs`, `UpdPacket.cs`  and `TcpChatClient.cs`, `TcpPacket.cs` for UDP and TCP transport protocols respectively. Chat clients are based on `IChatClient` interface, packets are based on `IPacket` interface and packet builders are based on `IPacketBuilder` interface. It uses interfaces to insure same general processing logic for both UDP and TCP protocols. Every described process logic has its own task to perform parallel and asynchronous processing. This allow to separate data sending and receiving how do such network apps require it.
#### Some UPD implementation details
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
In case of not getting confirmation and max retries reaching, client writes `ERR` message, go to `END` state and gracefully terminates an application.