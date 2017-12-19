# ![](https://github.com/sbrl/GlidingSquirrel/blob/master/logo.png?raw=true) GlidingSquirrel

A http (and Websockets!) server, implemented in C#.

Originally built for the /r/dailyprogrammer [hard challenge #322](https://www.reddit.com/r/dailyprogrammer/comments/6lti17/20170707_challenge_322_hard_static_http_server/).

GlidingSquirrel is currently in alpha testing! Don't use this in production unless you really know what you're doing :P

The logo is temporary!

## Features
 - HTTP 1.0 / 1.1 (RFC 1945 / RFC 1616) supported (mostly - bug reports & pull requests welcome :D)
 - Uses C&sharp; 7
 - Does not have anything to do with `System.Net.HttpServer` whatsoever at all
 - Easily extendable (it's an abstract class)
 - Supports client requests with bodies (e.g. `POST` and `PUT`, but any http verb with a `content-length` will work)
 - Supports `HEAD` requests
 - Parses and respects the `Accepts` HTTP header
 - Supports keep-alive connections (HTTP 1.1 only, of course)
 - Supports Websockets (RFC 6455, Initial implementation, version 13 only, needs thorough testing - detailed bug reports welcome :D)
 - Global configurable logging level

## Todo
 - Trailing headers
 - Give implementors of `WebsocketServer` a cleaner way to decide whether they want to accept a connection or not

## Getting Started
Start by making sure your project is using the .NET framework 4.6.2 or higher, and then install the `GlidingSquirrel` (pre-release) [NuGet package](https://www.nuget.org/packages/GlidingSquirrel/). Here's an overview of the important classes you'll probably come into contact with:

 - HTTP
	 - `SBRL.GlidingSquirrel.Http.HttpServer` - The main HTTP server class. Inherit from this to create a HTTP server.
	 - `SBRL.GlidingSquirrel.Http.HttpRequest` - Represents HTTP requests incoming from clients.
	 - `SBRL.GlidingSquirrel.Http.HttpResponse` - Represents the HTTP response   that will be sent by to the client.
	 - `SBRL.GlidingSquirrel.Http.HttpMessage` - The base class that `HttpRequest` and `HttpResponse` inherit from.
	 - `SBRL.GlidingSquirrel.Http.HttpResponseCode` - Represents a HTTP response code that is returned to the client.
 - Websockets
	 - `SBRL.GlidingSquirrel.Websocket.WebsocketServer` - The main Websockets server class. Inherit from this to create a websockets-capable server!
	 - `SBRL.GlidingSquirrel.Websocket.WebsocketClient` - Represents a single Websocket client.
	 - `SBRL.GlidingSquirrel.Websocket.WebsocketFrame` - Represents a single frame received from / about to be sent to a client.
 - `SBRL.GlidingSquirrel.Log` - The global logging class that all log messages flow through. Can be tuned to increase / decrease the verbosity of the logging messages.
 - `SBRL.GlidingSquirrel.LogLevel` - The enum that contains the different logging levels.

The best way I'm currently aware of to get an idea as to how to utilise this library for yourself is to take a look at the [demo server modes](https://github.com/sbrl/GlidingSquirrel/tree/master/GlidingSquirrelCLI/Modes) built into the CLI project that's part of the main GlidingSquirrel solution.

### Things to watch out for
 - If you don't set the response body (either with `response.SetBody()` or `response.Body = StreamReader`), then no response will be sent to the browser and clients will sit there waiting for a response indefinitely!
 - If you set the response body directly via `response.Body = StreamReader`, some clients may require the `ContentLength` property to be specified also (`response.SetBody()` does this automatically) - especially if a client is using a persistent connection.
 - As the GlidingSquirrel supports HTTP/1.1 persistent connection, you can tell it what (not?) to do with a connection either before or after sending a response via the `HttpConnectionAction` enum that you return a value from in `HandleRequest` (or `HandleHttpRequest` for websockets servers).

## Useful Links
 - [Tuples in C# 7](https://www.thomaslevesque.com/2016/07/25/tuples-in-c-7/)
 - [Tackling Tuples: Understanding the New C# 7 Value Type](http://our.componentone.com/2017/01/30/tackling-tuples-understanding-the-new-c-7-value-type/)
 - [Writing Websocket Servers on MDN](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers)
 - [Websocket Spec RFC6455](https://tools.ietf.org/html/rfc6455#section-5.5.1)

