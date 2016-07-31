using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Manfred
{

/// http://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws/27986405#27986405
public static class HttpResponseMessageExtensions
{
    public static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();

        if (response.Content != null)
            response.Content.Dispose();
        
        throw new SimpleHttpResponseException(response.StatusCode, content);
    }
}

public class SimpleHttpResponseException : Exception
{
    public HttpStatusCode StatusCode { get; private set; }

    public SimpleHttpResponseException(HttpStatusCode statusCode, string content) : base(content)
    {
        StatusCode = statusCode;
    }
}

}