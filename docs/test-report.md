# Test Report

Generated on Thu Aug 14 11:42:53 UTC 2025.

## Key File Results
- DatasetTests.DatasetFilesExist verifica la presenza di `sample_invoice.pdf` e `sample_invoice.png`.
- DatasetMarkdownNetTests.SamplePdf_ConversionMatchesReference estrae "ACME" e 36 box dal PDF.
- DatasetMarkdownNetTests.SamplePng_ContainsExpectedWords conferma che l'OCR del PNG contiene le parole attese.

## Build
```
  Determining projects to restore...
  Restored /workspace/docflow-ai-net/tests/DocflowAi.Net.BBoxResolver.Tests/DocflowAi.Net.BBoxResolver.Tests.csproj (in 3.85 sec).
  Failed to download package 'Serilog.Sinks.File.7.0.0' from 'https://api.nuget.org/v3-flatcontainer/serilog.sinks.file/7.0.0/serilog.sinks.file.7.0.0.nupkg'.
  Response status code does not indicate success: 503 (Service Unavailable).
  Failed to download package 'Serilog.Extensions.Logging.9.0.2' from 'https://api.nuget.org/v3-flatcontainer/serilog.extensions.logging/9.0.2/serilog.extensions.logging.9.0.2.nupkg'.
  Response status code does not indicate success: 503 (Service Unavailable).
  Restored /workspace/docflow-ai-net/src/DocflowAi.Net.Infrastructure/DocflowAi.Net.Infrastructure.csproj (in 17.35 sec).
  Restored /workspace/docflow-ai-net/src/DocflowAi.Net.Domain/DocflowAi.Net.Domain.csproj (in 12 ms).
  Restored /workspace/docflow-ai-net/src/DocflowAi.Net.BBoxResolver/DocflowAi.Net.BBoxResolver.csproj (in 17 ms).
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w [/workspace/docflow-ai-net/docflow-ai-net.sln]
  Restored /workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj (in 1.74 sec).
  Restored /workspace/docflow-ai-net/tools/BBoxEvalRunner/BBoxEvalRunner.csproj (in 2 ms).
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1603: DocflowAi.Net.Tests.Integration depends on Verify.Xunit (>= 24.7.2) but Verify.Xunit 24.7.2 was not found. Verify.Xunit 25.0.0 was resolved instead. [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
  Restored /workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj (in 23.42 sec).
  Restored /workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj (in 23.42 sec).
  Restored /workspace/docflow-ai-net/src/DocflowAi.Net.Application/DocflowAi.Net.Application.csproj (in 3 ms).
  Restored /workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj (in 23.42 sec).
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'Newtonsoft.Json' 11.0.1 has a known high severity vulnerability, https://github.com/advisories/GHSA-5crp-9r3c-p9vr [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
  Restored /workspace/docflow-ai-net/markitdownnet/src/MarkItDownNet/MarkItDownNet.csproj (in 8 ms).
  Restored /workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj (in 23 ms).
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w [/workspace/docflow-ai-net/docflow-ai-net.sln]
  Restored /workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj (in 1.3 sec).
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1603: DocflowAi.Net.Tests.Integration depends on Verify.Xunit (>= 24.7.2) but Verify.Xunit 24.7.2 was not found. Verify.Xunit 25.0.0 was resolved instead.
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'Newtonsoft.Json' 11.0.1 has a known high severity vulnerability, https://github.com/advisories/GHSA-5crp-9r3c-p9vr
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
  DocflowAi.Net.BBoxResolver -> /workspace/docflow-ai-net/src/DocflowAi.Net.BBoxResolver/bin/Release/net9.0/DocflowAi.Net.BBoxResolver.dll
  XFundEvalRunner -> /workspace/docflow-ai-net/tools/XFundEvalRunner/bin/Release/net9.0/XFundEvalRunner.dll
/workspace/docflow-ai-net/markitdownnet/src/MarkItDownNet/MarkItDownConverter.cs(94,32): warning CA1416: This call site is reachable on all platforms. 'Conversion.ToImages(Stream, bool, string?, RenderOptions)' is only supported on: 'Android' 31.0 and later, 'iOS' 13.6 and later, 'Linux', 'maccatalyst' 13.5 and later, 'macOS/OSX', 'Windows'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1416) [/workspace/docflow-ai-net/markitdownnet/src/MarkItDownNet/MarkItDownNet.csproj]
  BBoxEvalRunner -> /workspace/docflow-ai-net/tools/BBoxEvalRunner/bin/Release/net9.0/BBoxEvalRunner.dll
  MarkItDownNet -> /workspace/docflow-ai-net/markitdownnet/src/MarkItDownNet/bin/Release/net9.0/MarkItDownNet.dll
  DocflowAi.Net.Domain -> /workspace/docflow-ai-net/src/DocflowAi.Net.Domain/bin/Release/net9.0/DocflowAi.Net.Domain.dll
  DocflowAi.Net.Application -> /workspace/docflow-ai-net/src/DocflowAi.Net.Application/bin/Release/net9.0/DocflowAi.Net.Application.dll
  DocflowAi.Net.BBoxResolver.Tests -> /workspace/docflow-ai-net/tests/DocflowAi.Net.BBoxResolver.Tests/bin/Release/net9.0/DocflowAi.Net.BBoxResolver.Tests.dll
  XFundEvalRunner.Tests -> /workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/bin/Release/net9.0/XFundEvalRunner.Tests.dll
  DocflowAi.Net.Infrastructure -> /workspace/docflow-ai-net/src/DocflowAi.Net.Infrastructure/bin/Release/net9.0/DocflowAi.Net.Infrastructure.dll
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/Security/ApiKeyAuthenticationHandler.cs(5,137): warning CS0618: 'ISystemClock' is obsolete: 'Use TimeProvider instead.' [/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/Security/ApiKeyAuthenticationHandler.cs(5,190): warning CS0618: 'AuthenticationHandler<AuthenticationSchemeOptions>.AuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions>, ILoggerFactory, UrlEncoder, ISystemClock)' is obsolete: 'ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.' [/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/Program.cs(177,29): warning CS8604: Possible null reference argument for parameter 'value' in 'void IDiagnosticContext.Set(string propertyName, object value, bool destructureObjects = false)'. [/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj]
  DocflowAi.Net.Api -> /workspace/docflow-ai-net/src/DocflowAi.Net.Api/bin/Release/net9.0/DocflowAi.Net.Api.dll
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/Fakes/FakeProcessService.cs(39,53): warning CS8625: Cannot convert null literal to non-nullable reference type. [/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(211,65): warning CS0618: 'SKPaint.TextSize' is obsolete: 'Use SKFont.Size instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(212,9): warning CS0618: 'SKCanvas.DrawText(string, float, float, SKPaint)' is obsolete: 'Use DrawText(string text, float x, float y, SKTextAlign textAlign, SKFont font, SKPaint paint) instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(223,65): warning CS0618: 'SKPaint.TextSize' is obsolete: 'Use SKFont.Size instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(224,9): warning CS0618: 'SKCanvas.DrawText(string, float, float, SKPaint)' is obsolete: 'Use DrawText(string text, float x, float y, SKTextAlign textAlign, SKFont font, SKPaint paint) instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(236,69): warning CS0618: 'SKPaint.TextSize' is obsolete: 'Use SKFont.Size instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(237,13): warning CS0618: 'SKCanvas.DrawText(string, float, float, SKPaint)' is obsolete: 'Use DrawText(string text, float x, float y, SKTextAlign textAlign, SKFont font, SKPaint paint) instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(260,65): warning CS0618: 'SKPaint.TextSize' is obsolete: 'Use SKFont.Size instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(261,9): warning CS0618: 'SKCanvas.DrawText(string, float, float, SKPaint)' is obsolete: 'Use DrawText(string text, float x, float y, SKTextAlign textAlign, SKFont font, SKPaint paint) instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
  DocflowAi.Net.Api.Tests -> /workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/bin/Release/net9.0/DocflowAi.Net.Api.Tests.dll
  DocflowAi.Net.Tests.Integration -> /workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/bin/Release/net9.0/DocflowAi.Net.Tests.Integration.dll
  DocflowAi.Net.Tests.Unit -> /workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/bin/Release/net9.0/DocflowAi.Net.Tests.Unit.dll

Build succeeded.

/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1603: DocflowAi.Net.Tests.Integration depends on Verify.Xunit (>= 24.7.2) but Verify.Xunit 24.7.2 was not found. Verify.Xunit 25.0.0 was resolved instead. [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'Newtonsoft.Json' 11.0.1 has a known high severity vulnerability, https://github.com/advisories/GHSA-5crp-9r3c-p9vr [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1603: DocflowAi.Net.Tests.Integration depends on Verify.Xunit (>= 24.7.2) but Verify.Xunit 24.7.2 was not found. Verify.Xunit 25.0.0 was resolved instead.
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'Newtonsoft.Json' 11.0.1 has a known high severity vulnerability, https://github.com/advisories/GHSA-5crp-9r3c-p9vr
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
/workspace/docflow-ai-net/markitdownnet/src/MarkItDownNet/MarkItDownConverter.cs(94,32): warning CA1416: This call site is reachable on all platforms. 'Conversion.ToImages(Stream, bool, string?, RenderOptions)' is only supported on: 'Android' 31.0 and later, 'iOS' 13.6 and later, 'Linux', 'maccatalyst' 13.5 and later, 'macOS/OSX', 'Windows'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1416) [/workspace/docflow-ai-net/markitdownnet/src/MarkItDownNet/MarkItDownNet.csproj]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/Security/ApiKeyAuthenticationHandler.cs(5,137): warning CS0618: 'ISystemClock' is obsolete: 'Use TimeProvider instead.' [/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/Security/ApiKeyAuthenticationHandler.cs(5,190): warning CS0618: 'AuthenticationHandler<AuthenticationSchemeOptions>.AuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions>, ILoggerFactory, UrlEncoder, ISystemClock)' is obsolete: 'ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.' [/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/Program.cs(177,29): warning CS8604: Possible null reference argument for parameter 'value' in 'void IDiagnosticContext.Set(string propertyName, object value, bool destructureObjects = false)'. [/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/Fakes/FakeProcessService.cs(39,53): warning CS8625: Cannot convert null literal to non-nullable reference type. [/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(211,65): warning CS0618: 'SKPaint.TextSize' is obsolete: 'Use SKFont.Size instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(212,9): warning CS0618: 'SKCanvas.DrawText(string, float, float, SKPaint)' is obsolete: 'Use DrawText(string text, float x, float y, SKTextAlign textAlign, SKFont font, SKPaint paint) instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(223,65): warning CS0618: 'SKPaint.TextSize' is obsolete: 'Use SKFont.Size instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(224,9): warning CS0618: 'SKCanvas.DrawText(string, float, float, SKPaint)' is obsolete: 'Use DrawText(string text, float x, float y, SKTextAlign textAlign, SKFont font, SKPaint paint) instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(236,69): warning CS0618: 'SKPaint.TextSize' is obsolete: 'Use SKFont.Size instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(237,13): warning CS0618: 'SKCanvas.DrawText(string, float, float, SKPaint)' is obsolete: 'Use DrawText(string text, float x, float y, SKTextAlign textAlign, SKFont font, SKPaint paint) instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(260,65): warning CS0618: 'SKPaint.TextSize' is obsolete: 'Use SKFont.Size instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/MarkdownNetConverterTests.cs(261,9): warning CS0618: 'SKCanvas.DrawText(string, float, float, SKPaint)' is obsolete: 'Use DrawText(string text, float x, float y, SKTextAlign textAlign, SKFont font, SKPaint paint) instead.' [/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj]
    41 Warning(s)
    0 Error(s)

Time Elapsed 00:00:54.85
```

## Test
```
  Determining projects to restore...
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1603: DocflowAi.Net.Tests.Integration depends on Verify.Xunit (>= 24.7.2) but Verify.Xunit 24.7.2 was not found. Verify.Xunit 25.0.0 was resolved instead. [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'Newtonsoft.Json' 11.0.1 has a known high severity vulnerability, https://github.com/advisories/GHSA-5crp-9r3c-p9vr [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57 [/workspace/docflow-ai-net/docflow-ai-net.sln]
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj [/workspace/docflow-ai-net/docflow-ai-net.sln]
  All projects are up-to-date for restore.
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1603: DocflowAi.Net.Tests.Integration depends on Verify.Xunit (>= 24.7.2) but Verify.Xunit 24.7.2 was not found. Verify.Xunit 25.0.0 was resolved instead.
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/DocflowAi.Net.Tests.Integration.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/DocflowAi.Net.Api.Tests.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/DocflowAi.Net.Tests.Unit.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4
/workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/XFundEvalRunner.Tests.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-8g4q-xg66-9fp4
/workspace/docflow-ai-net/tools/XFundEvalRunner/XFundEvalRunner.csproj : warning NU1903: Package 'System.Text.Json' 8.0.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'Newtonsoft.Json' 11.0.1 has a known high severity vulnerability, https://github.com/advisories/GHSA-5crp-9r3c-p9vr
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Net.Http' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-7jgj-8wvc-jh57
/workspace/docflow-ai-net/src/DocflowAi.Net.Api/DocflowAi.Net.Api.csproj : warning NU1903: Package 'System.Text.RegularExpressions' 4.3.0 has a known high severity vulnerability, https://github.com/advisories/GHSA-cmhx-cq75-c4mj
  MarkItDownNet -> /workspace/docflow-ai-net/markitdownnet/src/MarkItDownNet/bin/Release/net9.0/MarkItDownNet.dll
  DocflowAi.Net.BBoxResolver -> /workspace/docflow-ai-net/src/DocflowAi.Net.BBoxResolver/bin/Release/net9.0/DocflowAi.Net.BBoxResolver.dll
  XFundEvalRunner -> /workspace/docflow-ai-net/tools/XFundEvalRunner/bin/Release/net9.0/XFundEvalRunner.dll
  XFundEvalRunner.Tests -> /workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/bin/Release/net9.0/XFundEvalRunner.Tests.dll
Test run for /workspace/docflow-ai-net/tests/XFundEvalRunner.Tests/bin/Release/net9.0/XFundEvalRunner.Tests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.12.0 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 126 ms - XFundEvalRunner.Tests.dll (net9.0)
  DocflowAi.Net.Domain -> /workspace/docflow-ai-net/src/DocflowAi.Net.Domain/bin/Release/net9.0/DocflowAi.Net.Domain.dll
  BBoxEvalRunner -> /workspace/docflow-ai-net/tools/BBoxEvalRunner/bin/Release/net9.0/BBoxEvalRunner.dll
  DocflowAi.Net.Application -> /workspace/docflow-ai-net/src/DocflowAi.Net.Application/bin/Release/net9.0/DocflowAi.Net.Application.dll
  DocflowAi.Net.Infrastructure -> /workspace/docflow-ai-net/src/DocflowAi.Net.Infrastructure/bin/Release/net9.0/DocflowAi.Net.Infrastructure.dll
  DocflowAi.Net.BBoxResolver.Tests -> /workspace/docflow-ai-net/tests/DocflowAi.Net.BBoxResolver.Tests/bin/Release/net9.0/DocflowAi.Net.BBoxResolver.Tests.dll
Test run for /workspace/docflow-ai-net/tests/DocflowAi.Net.BBoxResolver.Tests/bin/Release/net9.0/DocflowAi.Net.BBoxResolver.Tests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.12.0 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    19, Skipped:     0, Total:    19, Duration: 726 ms - DocflowAi.Net.BBoxResolver.Tests.dll (net9.0)
  DocflowAi.Net.Api -> /workspace/docflow-ai-net/src/DocflowAi.Net.Api/bin/Release/net9.0/DocflowAi.Net.Api.dll
  DocflowAi.Net.Tests.Integration -> /workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/bin/Release/net9.0/DocflowAi.Net.Tests.Integration.dll
  DocflowAi.Net.Tests.Unit -> /workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/bin/Release/net9.0/DocflowAi.Net.Tests.Unit.dll
Test run for /workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Unit/bin/Release/net9.0/DocflowAi.Net.Tests.Unit.dll (.NETCoreApp,Version=v9.0)
Test run for /workspace/docflow-ai-net/tests/DocflowAi.Net.Tests.Integration/bin/Release/net9.0/DocflowAi.Net.Tests.Integration.dll (.NETCoreApp,Version=v9.0)
  DocflowAi.Net.Api.Tests -> /workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/bin/Release/net9.0/DocflowAi.Net.Api.Tests.dll
VSTest version 17.12.0 (x64)
Test run for /workspace/docflow-ai-net/tests/DocflowAi.Net.Api.Tests/bin/Release/net9.0/DocflowAi.Net.Api.Tests.dll (.NETCoreApp,Version=v9.0)

VSTest version 17.12.0 (x64)

VSTest version 17.12.0 (x64)

Starting test execution, please wait...
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10, Duration: 729 ms - DocflowAi.Net.Tests.Unit.dll (net9.0)

Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17, Duration: 2 s - DocflowAi.Net.Tests.Integration.dll (net9.0)

Passed!  - Failed:     0, Passed:    48, Skipped:     0, Total:    48, Duration: 21 s - DocflowAi.Net.Api.Tests.dll (net9.0)
```
