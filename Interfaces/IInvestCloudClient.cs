using System;
using System.Threading.Tasks;

namespace Interfaces
{

  public enum DataSet {A, B };
  public enum DataType { row, col };
  public interface IInvestCloudClient
  {
    Task<InvestCloudResponse<int>> InitMatrix(int size);

    Task<InvestCloudResponse<int[]>> Numbers(DataSet dataSet, DataType dataType, int idx);

  }
}
