using HtmlAgilityPack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;

namespace CSharpScript.Host
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ScriptMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ScriptMiddleware(RequestDelegate next, IWebHostEnvironment webHostEnvironment)
        {
            _next = next;
            this.webHostEnvironment = webHostEnvironment;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.ToString();
            if (path.StartsWith("/api"))
            {
                await _next(httpContext);
                return;
            }
            if (path.EndsWith(".html"))
            {
                var htmlCode = await File.ReadAllTextAsync(MapPath(path));
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlCode);
                var scriptNodes = htmlDoc.DocumentNode.SelectNodes("//script");
                foreach (var scriptNode in scriptNodes)
                {
                    var src = scriptNode.GetAttributeValue("src", "");
                    if (src.EndsWith(".cs"))
                    {
                        var csharpPath = MapPath(src).ToLower();
                        var csharpCode = await File.ReadAllTextAsync(csharpPath);
                        var tree = SyntaxFactory.ParseSyntaxTree(csharpCode.Trim());
                        var references = new[]
                        {
                            MetadataReference.CreateFromFile(typeof(JSExportAttribute).Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(Object).Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(void).Assembly.Location),
                            MetadataReference.CreateFromFile(@"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\7.0.0\System.Runtime.dll")
                        };
                        var compilation = CSharpCompilation.Create(Path.GetFileName(csharpPath))
                            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                optimizationLevel: OptimizationLevel.Release))
                            .AddSyntaxTrees(tree)
                            .WithReferences(references);
                        var dllPath = MapPath("managed/" + Path.GetFileNameWithoutExtension(csharpPath)) + ".dll";
                        File.Delete(dllPath);
                        var emitResult = compilation.Emit(dllPath);
                        if (!emitResult.Success)
                        {
                            throw new Exception(string.Join(Environment.NewLine, emitResult.Diagnostics.Select(x => x.ToString())));
                        }
                        scriptNode.SetAttributeValue("src", "dllloader.js?name=" + Path.GetFileName(dllPath));
                        scriptNode.SetAttributeValue("type", "module");
                    }
                }
                httpContext.Response.ContentType = "text/html";
                using var ms = new MemoryStream();
                htmlDoc.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                await httpContext.Response.WriteAsync(reader.ReadToEnd());
                return;
            }
            await _next(httpContext);
        }

        string MapPath(string path)
        {
            return Path.Combine(webHostEnvironment.WebRootPath, path.TrimStart('/'));
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ScriptMiddlewareExtensions
    {
        public static IApplicationBuilder UseScriptMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ScriptMiddleware>();
        }
    }
}
