using Autofac;
using Autofac.Extensions.DependencyInjection;
using Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Matrix
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      var services = new ServiceCollection();
      services.AddHttpClient()
              .AddHttpClient<IInvestCloudClient, InvestCloudClient>()
              .SetHandlerLifetime(TimeSpan.FromMinutes(5));      

      var containerBuilder = new ContainerBuilder();
      containerBuilder.Populate(services);
      var container = containerBuilder.Build();

      //var factory = container.Resolve<IHttpClientFactory>();
            
      IInvestCloudClient client = container.Resolve<IInvestCloudClient>();

      await CalculateMatrix(client, 400);      
    }

    static async Task CalculateMatrix(IInvestCloudClient client, int n)
    {
      var responseInit = await client.InitMatrix(n);

      //create an nxn result array, value inside does not matter, will be replaced
      var result = Enumerable.Range(0, n)
      .Select(x => Enumerable.Range(x * n, n).ToArray()).ToArray();

      var start = DateTime.Now;

      ParallelOptions optionOut = new ParallelOptions() { MaxDegreeOfParallelism = 40 };
      ParallelOptions optionIn = new ParallelOptions() { MaxDegreeOfParallelism = 1 };

      Parallel.For(0, n, optionOut, row =>
     {
       Parallel.For(0, n, optionIn, async col =>
      {

        var rowA = await client.Numbers(DataSet.A, DataType.row, row);
        var colB = await client.Numbers(DataSet.B, DataType.col, col);

        result[row][col] = RowXCol(rowA, colB);


        //var rowA = client.Numbers(DataSet.A, DataType.row, row);
        //var colB = client.Numbers(DataSet.B, DataType.col, col);

        //await Task.WhenAll(rowA, colB).ContinueWith(
        //  (data) =>
        //  {
        //    result[row][col] = RowXCol(data.Result[0], data.Result[1]);
        //    Console.WriteLine($"Set {row} : {col}");
        //  }
        //);

      });
     });

      var endCalc = DateTime.Now;
      var timeTake = endCalc - start;
      Console.WriteLine("Time take to calculate: {0}", timeTake);

      string md5 = CalculateMd5(result);

      await client.Validate(md5);
      var end = DateTime.Now;
      Console.WriteLine("Time take to MD5 and valiate: {0}", end - endCalc);
    }

    private static string  CalculateMd5(int[][] array)
    {
      string str = ToString(array);
      var md5 = CreateMD5(str);
      return md5;
    }

    private static string ToString(int[][] array)
    {
      return string.Concat(array.Select(ar => string.Concat(ar.Select(x => x.ToString()))));
    }

    private static string CreateMD5(string input)
    {
      // Use input string to calculate MD5 hash
      using (MD5 md5 = MD5.Create())
      {
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        // Convert the byte array to hexadecimal string
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
          sb.Append(hashBytes[i].ToString("X2"));
        }
        return sb.ToString();
      }
    }

    static int RowXCol(int[] row, int[] col)
    {
      int result = 0;
      for (int i = 0; i < row.Length; i++)
      {
        result += row[i] * col[i];
      }
      return result;
    }


  }
}
