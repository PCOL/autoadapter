using System;
using System.Threading.Tasks;

namespace AutoAdapterUnitTests.Resources
{
    public interface ITaskApmAdapter
    {
        Task<bool> OperationAsync(TimeSpan timeout);
    }
}