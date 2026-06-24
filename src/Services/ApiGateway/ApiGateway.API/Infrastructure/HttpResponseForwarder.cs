namespace ApiGateway.API.Infrastructure;

public static class HttpResponseForwarder
{
    public static async Task ForwardAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();

        if (response.Content is not null)
        {
            foreach (var header in response.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            context.Response.Headers.Remove("transfer-encoding");
            await response.Content.CopyToAsync(context.Response.Body, cancellationToken);
        }
    }
}
