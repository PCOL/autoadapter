using System;
using System.Threading.Tasks;
using AutoAdapter;
using AutoAdapter.Extensions;
using AutoAdapterUnitTests.Resources;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoAdapterUnitTests
{
    public class ApmAdapterTests
    {
        [Fact]
        public void CreateIApmAdapter_FromApmAdaptee()
        {
            var adaptee = new ApmAdaptee();

            var adapter = adaptee.CreateAdapter<IApmAdapter>();

            Assert.NotNull(adapter);

            var asyncResult = adapter.BeginOperation(TimeSpan.FromSeconds(2), null, null);

            var result = adapter.EndOperation(asyncResult);

            Assert.True(result);
        }

        [Fact]
        public async Task CreateITaskApmAdapter_FromApmAdaptee()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IAdapterFactoryExtension>(new APMToTaskAdapterExtension());
            var serviceProvider = services.BuildServiceProvider();

            var adaptee = new ApmAdaptee();

            var adapter = adaptee.CreateAdapter<ITaskApmAdapter>(serviceProvider);

            Assert.NotNull(adapter);

            var result = await adapter.OperationAsync(TimeSpan.FromSeconds(2));

            Assert.True(result);
        }
    }
}