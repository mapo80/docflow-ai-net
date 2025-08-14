using System.Net.Http.Headers;
using System.Text;

namespace DocflowAi.Net.Api.Tests.Helpers;

public static class FormDataBuilder
{
    public static MultipartFormDataContent BuildMultipart(
        byte[] fileBytes,
        string fileName,
        string contentType,
        string? promptTextOrJson = null,
        string? fieldsJson = null)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        if (promptTextOrJson != null)
            content.Add(new StringContent(promptTextOrJson, Encoding.UTF8, "text/plain"), "prompt");
        if (fieldsJson != null)
            content.Add(new StringContent(fieldsJson, Encoding.UTF8, "application/json"), "fields");
        return content;
    }
}
