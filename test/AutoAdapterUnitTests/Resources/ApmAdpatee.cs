using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoAdapterUnitTests.Resources
{
    public class ApmAdaptee
    {
        private class AsyncOperation
            : IAsyncResult
        {
            private AsyncCallback callback;

            private bool result;

            private Exception exception;

            public AsyncOperation(TimeSpan timeout, AsyncCallback callback, object state)
            {
                this.callback = callback;
                this.AsyncState = state;
                this.AsyncWaitHandle = new ManualResetEvent(false);

                Task
                    .Delay(timeout)
                    .ContinueWith(
                        (t, obj) =>
                        {
                            if (t.IsFaulted == true)
                            {
                                this.exception = t.Exception;
                            }

                            this.result = true;
                            this.IsCompleted = true;
                            ((ManualResetEvent)this.AsyncWaitHandle).Set();

                            this.callback?.Invoke(this);
                        },
                        null);
            }

            public bool EndInvoke()
            {
                this.AsyncWaitHandle.WaitOne();

                if (this.exception != null)
                {
                    throw this.exception;
                }

                return this.result;
            }

            public object AsyncState { get; }

            public WaitHandle AsyncWaitHandle { get; }

            public bool CompletedSynchronously => false;

            public bool IsCompleted { get; private set; }
        }

        public IAsyncResult BeginOperation(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new AsyncOperation(timeout, callback, state);
        }

        public bool EndOperation(IAsyncResult asyncResult)
        {
            return ((AsyncOperation)asyncResult).EndInvoke();
        }
    }
}