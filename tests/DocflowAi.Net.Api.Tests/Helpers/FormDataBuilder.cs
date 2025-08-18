using System.Net.Http.Headers;
using System.Text;

namespace DocflowAi.Net.Api.Tests.Helpers;

public static class FormDataBuilder
{
    public static MultipartFormDataContent BuildMultipart(
        byte[] fileBytes,
        string fileName,
        string contentType,
        string model,
        string templateToken)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(model, Encoding.UTF8, "text/plain"), "model");
        content.Add(new StringContent(templateToken, Encoding.UTF8, "text/plain"), "templateToken");
        return content;
    }
}
