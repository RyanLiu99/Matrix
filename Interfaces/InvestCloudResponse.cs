using System;
using System.Collections.Generic;
using System.Text;

namespace Interfaces
{
  public class InvestCloudResponse<T>
  {
    public T Value { get; set; }
    public string Cause { get; set; }

    public bool Success { get; set; }
  }
}
