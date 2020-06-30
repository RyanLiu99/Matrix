using Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Matrix
{
  public class InvestCloudClient : IInvestCloudClient
  {
    private readonly HttpClient client;

    public InvestCloudClient(HttpClient client)
    {
      this.client = client;            
      //client.Timeout = TimeSpan.FromSeconds(35);
      client.BaseAddress = new Uri("https://recruitment-test.investcloud.com/");        
    }


    public async Task<int> InitMatrix(int size)
    {
      var response = await this.client.GetAsync($"/api/numbers/init/{size}");

      response.EnsureSuccessStatusCode();

      using var responseStream = await response.Content.ReadAsStreamAsync();
      var result = await JsonSerializer.DeserializeAsync
          <InvestCloudResponse<int>>(responseStream);

      EnsureSuccess(result);
      return result.Value;
    }
   
    public async Task<int[]> Numbers(DataSet dataSet, DataType dataType, int idx)
    {
      var response = await this.client.GetAsync($"/api/numbers/{dataSet}/{dataType}/{idx}");

      response.EnsureSuccessStatusCode();

      using var responseStream = await response.Content.ReadAsStreamAsync();
      var result = await JsonSerializer.DeserializeAsync
          <InvestCloudResponse<int[]>>(responseStream);

      EnsureSuccess(result, idx);      
      return result.Value;
    }

    public async Task Validate(string md5)
    {
      var request = new HttpRequestMessage(HttpMethod.Post,
          "https://recruitment-test.investcloud.com/api/numbers/validate");     
      
      request.Content = new StringContent(md5, Encoding.ASCII, "application/json");

      var response = await this.client.SendAsync(request);

      response.EnsureSuccessStatusCode();

      using var responseStream = await response.Content.ReadAsStreamAsync();
      var result = await JsonSerializer.DeserializeAsync
          <InvestCloudResponse<string>>(responseStream);

      Console.WriteLine("Validate result is {0}", result.Success);
      this.EnsureSuccess(result);
    }

    private void EnsureSuccess<T>(InvestCloudResponse<T> response, int? idx = null)
    {
      if (!response.Success)
      {
        Console.WriteLine($"Falied get {idx}: {response.Cause}");
        throw new Exception(response.Cause);
      }
      else
      {
        //Console.WriteLine($"Acquired {idx}" );
      }
    }

    public void Dispose()
    {
      this.client?.Dispose();
    }
  }

}
