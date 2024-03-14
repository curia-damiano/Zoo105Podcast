# Zoo105Podcast

A POC project delivering a podcast feed with Azure Functions, CosmosDB and Azure Blob Storage.\
The podcast is about the [Zoo di 105](http://ilsitodellozoo.com/); I don't claim any right and/or responsibility on the radio program and its content.

## Getting Started

The project has been created to demonstrate the power and simplicity of Azure PaaS (Platform as a Service).\
I have written the following blog posts to show parts of this project:
* [Zoo 105 Podcast: introduction and Azure Function setup](https://curia.me/zoo-105-podcast-introduction-and-azure-function-setup)
* [Zoo 105 Podcast: adding CosmosDB](https://curia.me/zoo-105-podcast-adding-cosmosdb)
* [Zoo 105 Podcast: adding Azure Blob storage](https://curia.me/zoo-105-podcast-adding-azure-blob-storage)
* [Zoo 105 Podcast: Migration from .NET Core 3.1 to .NET 6](https://curia.me/zoo-105-podcast-migration-from-net-core-31-to-net-6/)
* [Zoo 105 Podcast: Migrating of Azure Function from .NET 6 to .NET 8 isolated](https://curia.me/zoo-105-podcast-migration-of-azure-function-from-net-6-to-net-8-isolated/)

### Prerequisites

The only prerequisite is an Azure subscription, that is used to deploy the artifacts.\
If you don't already have one, feel free to visit [Microsoft Azure](https://azure.microsoft.com).

### Installing

After downloading or cloning locally, you will need to:
* Create an Azure Function App, where to deploy the code;
* Create a CosmosDB database, that will be used to cache already requested data;
* Create an Azure Blob storage, where the downloaded episodes will be stored.

## Authors

* **Curia Damiano** - *Initial work* - [curia-damiano](https://github.com/curia-damiano)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.