using System;

namespace SBRL.GlidingSquirrel.Http
{
	/// <summary>
	/// Represents the method of an HTTP request.
	/// </summary>
	public enum HttpMethod
	{
		/// <summary>
		/// The GET request type.
		/// </summary>
		GET,
		/// <summary>
		/// The POST request type. May have a body.
		/// </summary>
		POST,
		/// <summary>
		/// The HEAD request type. May *not* have a body - that is to say that the any body provided in a
		/// response to a HEAD request will be ignored.
		/// </summary>
		HEAD,
		/// <summary>
		/// The PUT request type. Probably will have a body.
		/// </summary>
		PUT,
		/// <summary>
		/// The DELETE request type.
		/// </summary>
		DELETE,
		/// <summary>
		/// The OPTIONS request type.
		/// </summary>
		OPTIONS,
		/// <summary>
		/// The PATCH request type.
		/// </summary>
		PATCH,
		/// <summary>
		/// The TRACE request type.
		/// </summary>
		TRACE

		// future Add WebDav methods here
	}
}
