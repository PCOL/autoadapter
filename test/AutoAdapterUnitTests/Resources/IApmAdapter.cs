using System;

namespace AutoAdapterUnitTests.Resources
{
    public interface IApmAdapter
    {
        IAsyncResult BeginOperation(TimeSpan timeout, AsyncCallback callback, object state);

        bool EndOperation(IAsyncResult asyncResult);
    }
}