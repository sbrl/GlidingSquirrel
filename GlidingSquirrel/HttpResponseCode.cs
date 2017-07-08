using System;
namespace SBRL.GlidingSquirrel
{
	public class HttpResponseCode
	{
		public readonly int Code;
		public readonly string Message;

		public HttpResponseCode(int inCode, string inMessage)
		{
			Code = inCode;
			Message = inMessage;
		}

		// todo Fill more of these out
		public static HttpResponseCode Ok = new HttpResponseCode(200, "OK");

		public static HttpResponseCode NotModified = new HttpResponseCode(304, "Not Modified");
		public static HttpResponseCode TemporaryRedirect = new HttpResponseCode(307, "Temporary Redirect");
		public static HttpResponseCode PremanentRedirect = new HttpResponseCode(308, "Premanent Redirect");

		public static HttpResponseCode Unauthorised = new HttpResponseCode(401, "Unauthorised");
		public static HttpResponseCode Forbidden = new HttpResponseCode(403, "Forbidden");
		public static HttpResponseCode NotFound = new HttpResponseCode(404, "Not Found");
		public static HttpResponseCode ImATeapot = new HttpResponseCode(418, "I'm a teapot");

		public static HttpResponseCode InternalServerError = new HttpResponseCode(500, "Internal Server Error");
		public static HttpResponseCode NotImplemented = new HttpResponseCode(501, "Not Implemented");
		public static HttpResponseCode BadGateway = new HttpResponseCode(502, "Bad Gateway");
		public static HttpResponseCode ServiceTemporarilyUnavailable = new HttpResponseCode(503, "Service Temporarily Unavailable");
	}
}
