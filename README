# Sample to demonstrate about how to make two clients connect to the same webapp server

## Run

Start the server by providing ASRS connection string:

```
cd server
dotnet run -- "Endpoint=xxxx"
```

It listens on localhost:5000

```
...
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
...
```

Start the client:
```
cd client
dotnet run -- "http://localhost:5000/transportHub" "http://localhost:5000/notificationHub"
```

The output is:

```
Received transport Hub info
connection Id svzNGkH_f7CovDbs54NW1Af27570021
connection Id mHThBUWRsb_Yg1IzlL7DgA2ded45221
Joined group
connection Id 8MvSTUGLhVijQmOoPaJZIQ2ded45221
Joined group
Received hub information to go to transport
Successful
Received hub information to go to transport
connection Id AHZ4vX-j0nA2P_Q8aLRCvgf27570021
stop the connection
stop the connection
stop the connection
```


