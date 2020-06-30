using System;
using System.Threading.Tasks;

namespace Interfaces
{
  public enum DataSet {A, B };
  public enum DataType { row, col };
  public interface IInvestCloudClient
  {
    Task<int> InitMatrix(int size);

    Task<int[]> Numbers(DataSet dataSet, DataType dataType, int idx);

    Task Validate(string md5);
  }
}
