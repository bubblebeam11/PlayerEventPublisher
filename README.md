# Player Event Publisher

## Overview 
Player event publisher reads player's data from an xml file, then parses it to json events of different types and sends it to RabbitMQ Server.
When player data for an individual data gets fully parsed, it's yielded to the event publisher, which establishes a connection and channel and sends it to RabbitMQ.
The rabbitMQ message has a header, with some values potentially useful for tracking events, and a body. 
The body can be optionally encrypted by setting the EncryptMessages flag in appsettings.json. 
There are unit tests for Parser.cs class, and I tested sending events to rabbit manually.
## Setup
I used .net 8.0 and visual studio 2022.

After cloning the repository, you can setup your rabbit connection parameters in appsettings.json, or use the default.

RabbitMQ server must be running.

Default channel name is player_events. 

To run the program, select PlayerEventPublisher as startup project and run it (Visual Studio). Can also do dotnet run.

After changing appsettings.json, rebuilding the solution is recomended.
