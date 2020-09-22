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
    const int batchSize = 200; 
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

    
    //It get two matrix first
    //then multiple by rows x cols

    //It get tow martirxes one by one, sequencially 
    //For the first matrix, it gets rows line by line; For 2nd matric, it gets cols line by line.
    
    //When it get line by line, it get 150 out 1000 lines at a time in paralla by using tasks
    
    //WHen it multiple by rows x cols, it uses ParallelOptions to do 40 calulations at a time.

    static async Task CalculateMatrix(IInvestCloudClient client, int n)
    {

      var responseInit = await client.InitMatrix(n);

      var start = DateTime.Now;

      int[][] aRows = await GetARows(client, n);
      int[][] bCols = await GetBCols(client, n);

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
      Console.WriteLine(" >>> Time take to calculate: {0} seconds", timeTake.TotalSeconds);

      string md5 = CalculateMd5(result);

      await client.Validate(md5);
      var end = DateTime.Now;
      Console.WriteLine("Time take to MD5 and valiate: {0}", end - endCalc);
    }

    // n- size of array n x n 
    private static async Task<int[][]> GetDataFromServer(IInvestCloudClient client, int n, DataSet dataSet, DataType dataType)
    {

      var result = (int[][])Array.CreateInstance(typeof(int[]), n);
      var ranges = Partitioner.Create(0, n - 1, batchSize).GetDynamicPartitions();

      //do in batch mode avoid network error
      //System.Net.Http.HttpRequestException: A connection attempt failed because the connected party did not properly respond after a period of time

      // use Enumerable.Range to run paralla
      //Use await.WhenAll to limit parallism
      foreach (var range in ranges)
      {
        int[][] lines = await Task.WhenAll(Enumerable.Range(range.Item1, range.Item2 - range.Item1 + 1)
      .Select(index => client.Numbers(dataSet, dataType, index)));

        Array.Copy(lines, 0, result, range.Item1, range.Item2 - range.Item1 + 1);
      }

      return result;
    }

    private static async Task<int[][]> GetARows(IInvestCloudClient client, int n)
    {
      return await GetDataFromServer(client, n, DataSet.A, DataType.row);
    }

    private static async Task<int[][]> GetBCols(IInvestCloudClient client, int n)
    {
      return await GetDataFromServer(client, n, DataSet.B, DataType.col);
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
      //create an nXn result array, value inside does not matter, will be replaced
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
