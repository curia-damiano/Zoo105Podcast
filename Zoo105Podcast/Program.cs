using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zoo105Podcast.AzureBlob;
using Zoo105Podcast.AzureQueue;

namespace Zoo105Podcast;

public static class Program
{
	public static async Task Main()
	{
		var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

		var host = new HostBuilder()
			.ConfigureFunctionsWorkerDefaults()
			.ConfigureServices((hostContext, services) =>
			{
				// https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
				services.AddApplicationInsightsTelemetryWorkerService();
				services.ConfigureFunctionsApplicationInsights();

				// Services to be instantiated by the Dependency Injection Host
				services.AddSingleton<AzureBlobHelper, AzureBlobHelper>();
				services.AddSingleton<AzureQueueHelper, AzureQueueHelper>();

				// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/microsoft.extensions.azure-readme
				var configuration = hostContext.Configuration;
				services.AddAzureClients(clientBuilder =>
				{
					clientBuilder.AddBlobServiceClient(configuration["AzureWebJobsStorage"]);
					clientBuilder.AddQueueServiceClient(configuration["AzureWebJobsStorage"]);
				});
			})
			.ConfigureAppConfiguration(builder =>
			{
				builder
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
					.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
					.AddEnvironmentVariables();
				var configuration = builder.Build();
			})
			.Build();

		await host.RunAsync().ConfigureAwait(false);
	}
}