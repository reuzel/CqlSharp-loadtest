CqlSharp-loadtest
=================

I've run into strange resource race with CqlSharp lib running under Owin self host.

OwinHost
----

is an Owin middleware web application, that can be hosted under IIS.

SimpleOwinHost
----

is a self host Owin runner of the middleware from OwinHost.

LoadTester
----

is a console app, that runs multi-threaded stress test. I can work in two modes:

  1. Using Provider class directly.
  2. Using HttpClient over rest api.


Problem:
=================

Self-hosted Owin server somehow limits number of running tasks so 'sender' tasks fill the queue and 'readers' wait too much time before really getting any response.
Simply start self host `SimpleOwinHost.exe` and it will bind to 
  `http://localhost/v2/negr/{item}/{relation}`

Then loadtester can be run with this command:
  `LoadTester.exe --threads 5 --url http://localhost/v2/`
  
then log (in /logs/timers*.log) will look something like this:

First long result is warmup one, when lib connects.
```
2014-07-21 12:47:30.9077 provider new request arrived
2014-07-21 12:47:31.8698 provider Time in provider: 950ms
```
Then five requests arrive simultaneously (we have 5 threads in loadtester, remember?):
```
2014-07-21 12:47:33.8410 provider new request arrived
2014-07-21 12:47:33.8410 provider new request arrived
2014-07-21 12:47:33.8410 provider new request arrived
2014-07-21 12:47:33.8410 provider new request arrived
2014-07-21 12:47:33.8410 provider new request arrived
2014-07-21 12:47:34.3791 provider Time in provider: 537ms
2014-07-21 12:47:34.3791 provider Time in provider: 536ms
2014-07-21 12:47:34.3791 provider Time in provider: 538ms
2014-07-21 12:47:34.3791 provider Time in provider: 536ms
2014-07-21 12:47:34.3791 provider Time in provider: 537ms
```

They block and reads but then
```
2014-07-21 12:47:34.4031 provider new request arrived
2014-07-21 12:47:34.4031 provider new request arrived
2014-07-21 12:47:34.4031 provider new request arrived
2014-07-21 12:47:34.4031 provider new request arrived
2014-07-21 12:47:34.4031 provider new request arrived
2014-07-21 12:47:34.4031 provider Time in provider: 4ms
2014-07-21 12:47:34.4031 provider new request arrived
2014-07-21 12:47:34.4031 provider Time in provider: 7ms
2014-07-21 12:47:34.4031 provider Time in provider: 5ms
2014-07-21 12:47:34.4031 provider Time in provider: 7ms
2014-07-21 12:47:34.4031 provider Time in provider: 7ms
...
```

This runs normally without any problems. But if you lift threads count (for example to 20) you will see something like this in log:
```
2014-07-21 14:35:34.7831 provider new request arrived
2014-07-21 14:35:34.7831 provider new request arrived
2014-07-21 14:35:34.7831 provider Time in provider: 5ms
2014-07-21 14:35:34.7921 provider Time in provider: 3ms
2014-07-21 14:35:34.7921 provider new request arrived
2014-07-21 14:35:34.7921 provider new request arrived
2014-07-21 14:35:34.7921 provider new request arrived
2014-07-21 14:35:34.7921 provider new request arrived
2014-07-21 14:35:34.7921 provider new request arrived
2014-07-21 14:35:35.2511 provider new request arrived
2014-07-21 14:35:35.7512 provider new request arrived
2014-07-21 14:35:36.2512 provider new request arrived
2014-07-21 14:35:36.7513 provider new request arrived
2014-07-21 14:35:37.2513 provider new request arrived
2014-07-21 14:35:37.7514 provider new request arrived
2014-07-21 14:35:38.2514 provider new request arrived
2014-07-21 14:35:38.7515 provider new request arrived
2014-07-21 14:35:39.2515 provider new request arrived
2014-07-21 14:35:39.7516 provider new request arrived
2014-07-21 14:35:40.2516 provider Time in provider: 5456ms
2014-07-21 14:35:40.2516 provider new request arrived
2014-07-21 14:35:40.2516 provider new request arrived
2014-07-21 14:35:40.7517 provider new request arrived
2014-07-21 14:35:41.2517 provider new request arrived
2014-07-21 14:35:41.7518 provider new request arrived
2014-07-21 14:35:42.2508 provider new request arrived
2014-07-21 14:35:42.7509 provider new request arrived
2014-07-21 14:35:43.2509 provider new request arrived
2014-07-21 14:35:43.7510 provider new request arrived
2014-07-21 14:35:44.2510 provider new request arrived
2014-07-21 14:35:44.7511 provider new request arrived
2014-07-21 14:35:45.2511 provider new request arrived
2014-07-21 14:35:45.7512 provider new request arrived
2014-07-21 14:35:46.2512 provider new request arrived
2014-07-21 14:35:46.7553 provider new request arrived
2014-07-21 14:35:47.2523 provider Time in provider: 12454ms
...
```

Heavy locks on command send, increasing wait times for reads. 
Despite of `MaxConnectionsPerNode` set to 20 CqlSharp only opened 2 connections to each node. Both being heavily locked with writes.

