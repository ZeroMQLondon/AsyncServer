Code from our meetup building a fully async scale out client server platform using ZeroMQ and C#

- Start AsyncZeroServer.exe and any number of AsyncZeroClient.exes. In any order.
- To add more clients, start another instance of AsyncZeroClient.exe.
- To add more servers, type in "add" (without quotes) and hit enter (in the AsyncZeroServer.exe window).
- To shut down the server, type "quit" (without quotes) and hit enter.
- Try changing the ip addresses and to inter-machine. 

Note: In a production scenario, we'd deal with timeouts for receives, and error handling. There's only so much we can do in just over an hour :)