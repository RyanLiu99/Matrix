using Autofac;
using Autofac.Extensions.DependencyInjection;
using Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Matrix
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      var services = new ServiceCollection();
      services.AddHttpClient()
              .AddHttpClient<InvestCloudClient>();
      

      var containerBuilder = new ContainerBuilder();
      containerBuilder.Populate(services);
      var container = containerBuilder.Build();

      var factory = container.Resolve<IHttpClientFactory>();
      var client = container.Resolve<InvestCloudClient>();

      var responseInit = await client.InitMatrix(3);
      var getRow = await client.Numbers(DataSet.A, DataType.row, 0);
      
    }

  }
}
