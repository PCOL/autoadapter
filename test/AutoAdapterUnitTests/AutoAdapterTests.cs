using System;
using AutoAdapter;
using AutoAdapter.Reflection;
using AutoAdapterUnitTests.Resources;
using Xunit;

namespace AutoAdapterUnitTests
{
    public class AutoAdapterUnitTests
    {
        [Fact]
        public void CreateIPropertyAdapter_FromPropertyAdaptee()
        {
            var testDateTime = DateTime.Now;
            var testGuid = Guid.NewGuid();

            var testObject = new PropertyAdaptee()
            {
                StringProperty = "Test",
                Int16Property = 20000,
                Int32Property = 2000000,
                Int64Property = 200000000,
                FloatProperty = 3.14F,
                DoubleProperty = 3.14,
                BooleanProperty = true,
                DateTimeProperty = testDateTime,
                GuidProperty = testGuid
            };

            var testAdapter = testObject.CreateAdapter<IPropertyAdapter>();

            Assert.NotNull(testAdapter);
            Assert.Equal("Test", testAdapter.StringProperty);
            Assert.Equal(20000, testAdapter.Int16Property);
            Assert.Equal(2000000, testAdapter.Int32Property);
            Assert.Equal(200000000, testAdapter.Int64Property);
            Assert.Equal(3.14F, testAdapter.FloatProperty);
            Assert.Equal(3.14, testAdapter.DoubleProperty);
            Assert.Equal(true, testAdapter.BooleanProperty);
            Assert.Equal(testDateTime, testAdapter.DateTimeProperty);
            Assert.Equal(testGuid, testAdapter.GuidProperty);
        }

        [Fact]
        public void CreateIPropertyAdapter_FromStaticPropertyAdaptee()
        {
            var testDateTime = DateTime.Now;
            var testGuid = Guid.NewGuid();

            StaticPropertyAdaptee.StringProperty = "Test";
            StaticPropertyAdaptee.Int16Property = 20000;
            StaticPropertyAdaptee.Int32Property = 2000000;
            StaticPropertyAdaptee.Int64Property = 200000000;
            StaticPropertyAdaptee.FloatProperty = 3.14F;
            StaticPropertyAdaptee.DoubleProperty = 3.14;
            StaticPropertyAdaptee.BooleanProperty = true;
            StaticPropertyAdaptee.DateTimeProperty = testDateTime;
            StaticPropertyAdaptee.GuidProperty = testGuid;

            var testAdapterType = typeof(StaticPropertyAdaptee).CreateAdapterType<IStaticPropertyAdapter>();
            var testAdapter = Activator.CreateInstance(testAdapterType, new object[] { null, null }) as IStaticPropertyAdapter;

            Assert.NotNull(testAdapter);

            Assert.Equal("Test", testAdapter.StringProperty);
            Assert.Equal(20000, testAdapter.Int16Property);
            Assert.Equal(2000000, testAdapter.Int32Property);
            Assert.Equal(200000000, testAdapter.Int64Property);
            Assert.Equal(3.14F, testAdapter.FloatProperty);
            Assert.Equal(3.14, testAdapter.DoubleProperty);
            Assert.Equal(true, testAdapter.BooleanProperty);
            Assert.Equal(testDateTime, testAdapter.DateTimeProperty);
            Assert.Equal(testGuid, testAdapter.GuidProperty);
        }

        [Fact]
        public void CreateIIndexPropertyAdapter_FromIndexPropertyAdaptee()
        {
            var testDateTime = DateTime.Now;
            var testGuid = Guid.NewGuid();

            var testIndexObject = new IndexPropertyAdaptee(5);

            testIndexObject[1] = "Test";
            testIndexObject[1, 100] = 200;

            var testIndexAdapter = testIndexObject.CreateAdapter<IIndexPropertyAdapter>();

            Assert.NotNull(testIndexAdapter);
            Assert.Equal("Test", testIndexAdapter[1]);
            Assert.Equal(200, testIndexAdapter[1, 100]);
        }
    }
}