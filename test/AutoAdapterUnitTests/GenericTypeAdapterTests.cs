using AutoAdapterUnitTests.Resources;
using AutoAdapter;
using Xunit;

namespace AutoAdapterUnitTests
{
    public class GenericTypeAdapterTests
    {
        [Fact]
        public void GenericAdapter_CreateAdapter_StringProperty()
        {
            var adaptee = new GenericAdaptee<string>()
            {
                Property = "Test"
            };

            var adapter = adaptee.CreateAdapter<IGenericAdapter<string>>();

            Assert.NotNull(adapter);
            Assert.Equal("Test", adapter.Property);
        }

        [Fact]
        public void GenericAdapter_CreateAdapter_ChildAdapterProperty()
        {
            var adaptee = new GenericAdaptee<ChildAdaptee>()
            {
                Property = new ChildAdaptee()
                {
                    Property = "Test"
                }
            };

            var adapter = adaptee.CreateAdapter<IGenericAdapter<IChildAdapter>>();

            Assert.NotNull(adapter);
            Assert.NotNull(adapter.Property);
            Assert.Equal("Test", adapter.Property.Property);
        }
    }
}