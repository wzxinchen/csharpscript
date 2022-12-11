using Microsoft.AspNetCore.StaticFiles;

namespace CSharpScript.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorPages();
            var app = builder.Build();
            app.UseScriptMiddleware();
            var provider = new FileExtensionContentTypeProvider();
            // Add new mappings
            provider.Mappings[".dll"] = "application/x-msdownload";
            provider.Mappings[".wasm"] = "application/wasm";
            provider.Mappings[".pdb"] = "application/octet-stream";
            provider.Mappings[".symbols"] = "application/octet-stream";
            provider.Mappings[".bin"] = "application/octet-stream";
            provider.Mappings[".dat"] = "application/octet-stream";
            provider.Mappings[".blat"] = "application/octet-stream";
            app.UseStaticFiles(new StaticFileOptions()
            {
                ContentTypeProvider = provider
            });
            app.UseRouting();
            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}