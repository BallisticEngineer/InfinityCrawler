using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace InfinityCrawler.Tests.TestSite
{
	public class Startup
	{
		private SiteContext Context { get; }

		public Startup(SiteContext context)
		{
			Context = context;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvcCore();
		}

		public void Configure(IApplicationBuilder app)
		{
			var staticFileProvider = new PhysicalFileProvider(
				Path.Combine(Directory.GetCurrentDirectory(), $"Resources/{Context.SiteFolder}"));

			//Some sites expose the same page under both a path and a trailing-slash
			//variant (a "page alias"). When a static file is requested without an
			//extension and no file exists at that exact path, fall back to serving the
			//sibling ".html" file so that alias forms are reachable, mirroring how a
			//PHP-based site serves rewrite-style paths. This runs before the static
			//file pipeline so a directory at the bare path cannot shadow the alias.
			//The charset mirrors a legacy-CMS response so the alias detection must
			//survive code pages that are not in the default .NET encoding provider.
			app.Use(async (context, next) =>
			{
				var path = context.Request.Path.Value ?? string.Empty;
				if (path.Length > 1 && !Path.HasExtension(path))
				{
					var fileInfo = staticFileProvider.GetFileInfo(path.TrimStart('/') + ".html");
					if (fileInfo.Exists)
					{
						context.Response.StatusCode = 200;
						context.Response.ContentType = "text/html; charset=windows-1251";
						using (var stream = fileInfo.CreateReadStream())
						{
							await stream.CopyToAsync(context.Response.Body);
						}
						return;
					}
				}

				await next();
			});

			app.UseDefaultFiles(new DefaultFilesOptions
			{
				FileProvider = staticFileProvider
			});
			app.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = staticFileProvider
			});

#if NET6_0_OR_GREATER
			app.UseRouting();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
#else
			app.UseMvc();
#endif
		}
	}
}
