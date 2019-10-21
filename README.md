# Sample to demonstrate about how to make two clients connect to the same webapp server

## Run

Start the server by providing ASRS connection string:

```
cd server
dotnet user-secrets set Azure:SignalR:ConnectionString "Endpoint=XXXX"
dotnet run
```

It listens on localhost:5000

```
...
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
...
```

Start the two clients: primary and secondary

The primary client will connect transport hub ("http://localhost:5000/transportHub"). It will print the group name to sync with secondary client.

```
cd client
dotnet run -- primary
```

The output is:

```
Group for sync: i6E+J1HJ
Press Ctrl+C to stop
Received sticky Hub info
connection Id e2ZF4c0hhRkFj26zSigK0Afc4659ff1
Joined group
```

The secondary client also connects the transport hub ("http://localhost:5000/transportHub"). The first time it connects is unsticky, and it gets the primary client's sticky information, then it reconnects with primary client sticky information.

```
cd client
dotnet run -- secondar -g i6E+J1HJ
```

The output is:

```
connection Id qeJXcLBNwl8OupbqPeSrNw2ded45221
Joined group
Received hub information to go to transport
Press Ctrl+C to stop
connection Id LNwfUmGAKWFLJuNjRtqnwwfc4659ff1
stop the connection
```

When you see two connections with the same request ID and ASRS instance name, it means the secondary client has connected to transport hub with sticky information.
```
...
client e2ZF4c0hhRkFj26zSigK0Afc4659ff1 request ID: /0T5zBwJAAA=
client e2ZF4c0hhRkFj26zSigK0Afc4659ff1 goes to ASRS: b187b365-5d2a-4869-9079-a0015cc60cbb
...
client qeJXcLBNwl8OupbqPeSrNw2ded45221 request ID: m6NU1RwJAAA=
client qeJXcLBNwl8OupbqPeSrNw2ded45221 goes to ASRS: d2833b1f-c6c1-4a02-a93b-1e1c48cee542
client LNwfUmGAKWFLJuNjRtqnwwfc4659ff1 request ID: /0T5zBwJAAA=
client LNwfUmGAKWFLJuNjRtqnwwfc4659ff1 goes to ASRS: b187b365-5d2a-4869-9079-a0015cc60cbb
Succesfully see two clients are connected to this hub
```

If you stop the secondary client by "ctrl + C", and retype the command "dotnet run -- secondar -g i6E+J1HJ", you will see it successfully reconnected.
