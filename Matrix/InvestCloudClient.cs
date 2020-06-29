using Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
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
      client.BaseAddress = new Uri("https://recruitment-test.investcloud.com/");
      client.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<InvestCloudResponse<int>> InitMatrix(int size)
    {
      var response = await this.client.GetAsync($"/api/numbers/init/{size}");

      response.EnsureSuccessStatusCode();

      using var responseStream = await response.Content.ReadAsStreamAsync();
      var result = await JsonSerializer.DeserializeAsync
          <InvestCloudResponse<int>>(responseStream);
      return result;
    }

    
    public async Task<InvestCloudResponse<int[]>> Numbers(DataSet dataSet, DataType dataType, int idx)
    {
      var response = await this.client.GetAsync($"/api/numbers/{dataSet}/{dataType}/{idx}");

      response.EnsureSuccessStatusCode();

      using var responseStream = await response.Content.ReadAsStreamAsync();
      var result = await JsonSerializer.DeserializeAsync
          <InvestCloudResponse<int[]>>(responseStream);
      return result;
    }
  }

}
