using System;
using System.IO;
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
