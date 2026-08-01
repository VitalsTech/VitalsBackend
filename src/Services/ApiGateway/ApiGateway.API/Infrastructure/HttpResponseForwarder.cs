namespace ApiGateway.API.Infrastructure;

public static class HttpResponseForwarder
{
    public static async Task ForwardAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();

        if (response.Content is null)
            return;

        foreach (var header in response.Content.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();

        context.Response.Headers.Remove("transfer-encoding");

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0)
            return;

        if ((int)response.StatusCode is >= 400 and <= 599)
        {
            var original = System.Text.Encoding.UTF8.GetString(bytes);
            var localized = ErrorMessageLocalizer.TryLocalizeJson(original);
            if (localized is not null)
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(localized, cancellationToken);
                return;
            }
        }

        await context.Response.Body.WriteAsync(bytes, cancellationToken);
    }
}
