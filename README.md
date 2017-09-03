# GlidingSquirrel

A http (and websockets!) server, implemented in C#.

Originally built for the /r/dailyprogrammer [hard challenge #322](https://www.reddit.com/r/dailyprogrammer/comments/6lti17/20170707_challenge_322_hard_static_http_server/).

GlidingSquirrel is currently in alpha testing! Don't use this in production unless you really know what you're doing :P

## Features
 - HTTP 1.0 / 1.1 implemented so far
 - Uses C&sharp; 7
 - Does not have anything to do with `System.Net.HttpServer` whatsoever at all
 - Easily extendable (it's an abstract class)
 - Supports client requests with bodies (e.g. `POST` and `PUT`, but any http verb with a `content-length` will work)
 - Supports `HEAD` requests
 - Parses and respects the `Accepts` HTTP header
 - Supports keep-alive connections (HTTP 1.1 only, of course)
 - Supports Websockets (Initial implementation, Version 13 only - RFC 6455, needs thorough testing - detailed bug reports welcome :D)

## Todo
 - Trailing headers
 - Make logging much more flexible (it logs to the console only at the moment)

## Getting Started
A tutorial will be coming soon. For now, take a look at the `HttpServer` class and look at the abstract methods and their intellisense comments.

## Useful Links
 - [Tuples in C# 7](https://www.thomaslevesque.com/2016/07/25/tuples-in-c-7/)
 - [Tackling Tuples: Understanding the New C# 7 Value Type](http://our.componentone.com/2017/01/30/tackling-tuples-understanding-the-new-c-7-value-type/)
 - [Writing Websocket Servers on MDN](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers)
 - [Websocket Spec RFC6455](https://tools.ietf.org/html/rfc6455#section-5.5.1)
