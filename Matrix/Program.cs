using Autofac;
using Autofac.Extensions.DependencyInjection;
using Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Matrix
{
  class Program
  {
    const int batchSize = 50;
    const int arraySize = 1000;
    public static async Task Main(string[] args)
    {
      
      var services = new ServiceCollection();
      services.AddHttpClient()
              .AddHttpClient<IInvestCloudClient, InvestCloudClient>()
              .SetHandlerLifetime(TimeSpan.FromMinutes(5));

      var containerBuilder = new ContainerBuilder();
      containerBuilder.Populate(services);
      var container = containerBuilder.Build();

      using (IInvestCloudClient client = container.Resolve<IInvestCloudClient>())
      {
        await CalculateMatrix(client, arraySize);
      }
    }

    private static async Task<int[][]> GetDataFromServer(IInvestCloudClient client, int n, DataSet dataSet, DataType dataType)
    {
      //var tasks = Partitioner.Create(0, n, batchSize).GetDynamicPartitions().AsParallel()
      //  .SelectMany(x => Enumerable.Range(x.Item1, x.Item2 - x.Item1 + 1)
      // .Select(index => client.Numbers(dataSet, dataType, index)
      // ));

      //int[][] result = null;
      //await Task.WhenAll(tasks).ContinueWith(lines => result = lines.Result);
      //return result;

      var result = (int[][])Array.CreateInstance(typeof(int[]), n); 

      var ranges = Partitioner.Create(0, n-1, batchSize).GetDynamicPartitions(); 


      foreach (var range in ranges)
      {
        var lines = Task.WhenAll(Enumerable.Range(range.Item1, range.Item2 - range.Item1 + 1)
      .Select(index => client.Numbers(dataSet, dataType, index))).Result;

        Array.Copy(lines, 0, result, range.Item1, range.Item2 - range.Item1 + 1);
      }

      return result;
    }


    private static async Task<int[][]> GetARows(IInvestCloudClient client, int n)
    {
      return await GetDataFromServer(client, n, DataSet.A, DataType.row);
      //int[][] result = null;
      //var tasks = Enumerable.Range(0, n).Select(rowIndex => client.Numbers(DataSet.A, DataType.row, rowIndex));
      //await Task.WhenAll(tasks).ContinueWith(rows => result = rows.Result);
      //return result;
    }

    private static async Task<int[][]> GetBCols(IInvestCloudClient client, int n)
    {
      return await GetDataFromServer(client, n, DataSet.B, DataType.col);
      //int[][] result = null;
      //var tasks = Enumerable.Range(0, n).Select(colIndex => client.Numbers(DataSet.B, DataType.col, colIndex));
      //await Task.WhenAll(tasks).ContinueWith(cols => result = cols.Result);
      //return result;
    }


    static async Task CalculateMatrix(IInvestCloudClient client, int n)
    {

      var responseInit = await client.InitMatrix(n);

      var start = DateTime.Now;

      var aRows = await GetARows(client, n);
      var bCols = await GetBCols(client, n);

      var result = Init2DArray(n);

     

      ParallelOptions optionOut = new ParallelOptions() { MaxDegreeOfParallelism = 40 };
      ParallelOptions optionIn = new ParallelOptions() { MaxDegreeOfParallelism = 40 };

      Parallel.For(0, n, optionOut, row =>
      {
        Parallel.For(0, n, optionIn, col =>
        {
          result[row][col] = RowXCol(aRows[row], bCols[col]);
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


    private static string CalculateMd5(int[][] array)
    {
      string str = ToString(array);
      var md5 = CreateMD5(str);
      return md5;
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

    //create n X n array
    private static int[][] Init2DArray(int n)
    {
      //create an nxn result array, value inside does not matter, will be replaced
      var result = Enumerable.Range(0, n)
      .Select(x => Enumerable.Range(x * n, n).ToArray()).ToArray();
      return result;
    }

    private static string ToString(int[][] array)
    {
      return string.Concat(array.Select(ar => string.Concat(ar.Select(x => x.ToString()))));
    }
  }
}
