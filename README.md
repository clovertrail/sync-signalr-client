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

The primary client will connect both notification hub and transport hub. It will print the group name to sync with secondary client.

```
cd client
dotnet run -- primary -t "http://localhost:5000/transportHub"  -n "http://localhost:5000/notificationHub"
```

The output is:

```
Received transport Hub info
connection Id zA_SZyOQ0dlL36tHxd9tjwfc4659ff1
Group for sync: eU20pZMn
connection Id Z8dcx62vu4nunOjR4Q4cFwfc4659ff1
Joined group
```

The secondary client only connect notification hub, and it requires the group name for syncing.

```
cd client
dotnet run -- secondar -g eU20pZMn -n "http://localhost:5000/notificationHub"
```

The output is:

```
connection Id VgFxaZS7SgjOxPdwMeZUpA2ded45221
Joined group
Received hub information to go to transport
Press Ctrl+C to stop
connection Id CXPQ1LC7VoHy26WIsoWDXQfc4659ff1
```

When you see two connections, it means the secondary client has connected to transport hub.
