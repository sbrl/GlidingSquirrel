using System;

namespace SBRL.GlidingSquirrel.Http
{
	/// <summary>
	/// Represents an HTTP response code and its associated descriptive message, such 
	/// as "200 OK", and "404 Not Found".
	/// </summary>
	public class HttpResponseCode
	{
		/// <summary>
		/// The numeric status code.
		/// </summary>
		public readonly int Code;
		/// <summary>
		/// The associated descriptive message, such as "OK", or "Not Found".
		/// </summary>
		public readonly string Message;

		/// <summary>
		/// Creates a new <see cref="HttpResponseCode" /> instance.
		/// </summary>
		/// <param name="inCode">The numberic status code.</param>
		/// <param name="inMessage">The associated descriptive message.</param>
		public HttpResponseCode(int inCode, string inMessage)
		{
			Code = inCode;
			Message = inMessage;
		}

		/// <summary>
		/// Returns the string representation of this HTTP status code instance.
		/// May be directly included in an outgoing HTTP response.
		/// </summary>
		/// <returns>A <see cref="string"/> that represents the current <see cref="HttpResponseCode"/> instance.</returns>
		public override string ToString()
		{
			return $"{Code} {Message}";
		}

		// todo Fill more of these out
		/// <summary>
		/// 100 Continue
		/// </summary>
		public static HttpResponseCode Continue = new HttpResponseCode(100, "Continue");
		/// <summary>
		/// 101 Switching Protocols
		/// </summary>
		public static HttpResponseCode SwitchingProtocols = new HttpResponseCode(101, "Switching Protocols");
		/// <summary>
		/// 101 Web Socket Procotol Handshake
		/// </summary>
		public static HttpResponseCode SwitchingProtocotolsWebsocket = new HttpResponseCode(101, "Web Socket Protocol Handshake");

		/// <summary>
		/// 200 OK
		/// </summary>
		public static HttpResponseCode Ok = new HttpResponseCode(200, "OK");

		/// <summary>
		/// 304 Not Modified
		/// </summary>
		public static HttpResponseCode NotModified = new HttpResponseCode(304, "Not Modified");
		/// <summary>
		/// 307 Temporary Redirect
		/// </summary>
		public static HttpResponseCode TemporaryRedirect = new HttpResponseCode(307, "Temporary Redirect");
		/// <summary>
		/// 308 Permanent Redirect
		/// </summary>
		public static HttpResponseCode PermanentRedirect = new HttpResponseCode(308, "Premanent Redirect");

		/// <summary>
		/// 400 Bad Request
		/// </summary>
		public static HttpResponseCode BadRequest = new HttpResponseCode(400, "Bad Request");
		/// <summary>
		/// 401 Unauthorised
		/// </summary>
		public static HttpResponseCode Unauthorised = new HttpResponseCode(401, "Unauthorised");
		/// <summary>
		/// 403 Forbidden
		/// </summary>
		public static HttpResponseCode Forbidden = new HttpResponseCode(403, "Forbidden");
		/// <summary>
		/// 404 Not Found
		/// </summary>
		public static HttpResponseCode NotFound = new HttpResponseCode(404, "Not Found");
		/// <summary>
		/// 405 Method Not Allowed
		/// </summary>
		public static HttpResponseCode MethodNotAllowed = new HttpResponseCode(405, "MethodNotAllowed");
		/// <summary>
		/// 406 Not Acceptable
		/// </summary>
		public static HttpResponseCode NotAcceptable = new HttpResponseCode(406, "Not Acceptable");
		/// <summary>
		/// 411 Length Required
		/// </summary>
		public static HttpResponseCode LengthRequired = new HttpResponseCode(411, "Length Required");
		/// <summary>
		/// 414 Request-URI Too Long
		/// </summary>
		public static HttpResponseCode RequestUrlTooLong = new HttpResponseCode(414, "Request-URI Too Long");
		/// <summary>
		/// 401 I'm a teapot
		/// </summary>
		public static HttpResponseCode ImATeapot = new HttpResponseCode(418, "I'm a teapot");

		/// <summary>
		/// 500 Internal Server Error
		/// </summary>
		public static HttpResponseCode InternalServerError = new HttpResponseCode(500, "Internal Server Error");
		/// <summary>
		/// 501 Not Implemented
		/// </summary>
		public static HttpResponseCode NotImplemented = new HttpResponseCode(501, "Not Implemented");
		/// <summary>
		/// 502 Bad Gateway
		/// </summary>
		public static HttpResponseCode BadGateway = new HttpResponseCode(502, "Bad Gateway");
		/// <summary>
		/// 503 Service Temporarily Unavailable
		/// </summary>
		public static HttpResponseCode ServiceTemporarilyUnavailable = new HttpResponseCode(503, "Service Temporarily Unavailable");
		/// <summary>
		/// 505 HTTP Version Not Supported
		/// </summary>
		public static HttpResponseCode HttpVersionNotSupported = new HttpResponseCode(505, "HTTP Version Not Supported");
	}
}
