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
Group for sync: HCeN5SOT
Press Ctrl+C to stop
Received sticky Hub info
connection Id frU7pT5iGLz6iWFhgrOCjg2ded45221
Joined group
```

The secondary client also connects the transport hub ("http://localhost:5000/transportHub"). The first time it connects is unsticky, and it gets the primary client's sticky information, then it reconnects with primary client sticky information.

```
cd client
dotnet run -- secondar -g HCeN5SOT
```

The output is:

```
connection Id 8XJJsnVH7ntSER7DVop9Rwfc4659ff1
Joined group
Received hub information to go to transport
Press Ctrl+C to stop
connection Id n__cOubg9LckMJz9JTiZtQ2ded45221
```

When you see two connections with the same request ID and ASRS instance name, it means the secondary client has connected to transport hub with sticky information.
```
...
client frU7pT5iGLz6iWFhgrOCjg2ded45221 request ID: a3ujAgQLAAA=
client frU7pT5iGLz6iWFhgrOCjg2ded45221 goes to ASRS: d2833b1f-c6c1-4a02-a93b-1e1c48cee542
...
client 8XJJsnVH7ntSER7DVop9Rwfc4659ff1 request ID: eCj8HAQLAAA=
client 8XJJsnVH7ntSER7DVop9Rwfc4659ff1 goes to ASRS: b187b365-5d2a-4869-9079-a0015cc60cbb
client n__cOubg9LckMJz9JTiZtQ2ded45221 request ID: a3ujAgQLAAA=
client n__cOubg9LckMJz9JTiZtQ2ded45221 goes to ASRS: d2833b1f-c6c1-4a02-a93b-1e1c48cee542
Succesfully see two clients are connected to this hub
```

If you stop the secondary client by "ctrl + C", you will see the following information on primary client's console:

```
...
Joined group
connection n__cOubg9LckMJz9JTiZtQ2ded45221 is dropped
```
and retype the command "dotnet run -- secondar -g HCeN5SOT", you will see it successfully reconnected.
