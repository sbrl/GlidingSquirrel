# GlidingSquirrel Changelog

## v0.6.1-alpha

### Added
 - Started this changelog! About time too, if I'm releasing GlidingSquirrel on NuGet.
 - Added abstract method `bool WebsocketServer.ShouldAcceptConnection(HttpRequest, HttpResponse)` to `WebsocketServer` to check if a connection should be accepted or not. Note that most checks are performed automatically (i.e. those such that we confirm to the spec) - it's only things like checking the origin and credentials etc. that you may need this method for.
 - Added `WebsocketClient.HandshakeRequest`, which holds the HTTP request that the client sent when initiating the WebSockets Handshake.

### Changed
 - `WebsocketServer.HandleHttpRequest()` must now return a `HttpConnectionAction`.
