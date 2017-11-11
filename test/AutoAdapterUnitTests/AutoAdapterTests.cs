using System;
using AutoAdapter;
using AutoAdapterUnitTests.Resources;
using Xunit;

namespace AutoAdapterUnitTests
{
    public class AutoAdapterUnitTests
    {
        [Fact]
        public void CreateITestAdapter_FromTestType_GetPropertyTest()
        {
            var testDateTime = DateTime.Now;
            var testGuid = Guid.NewGuid();

            var testObject = new PropertyTest()
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

            var testAdapter = testObject.CreateAdapter<IPropertyTestAdapter>();

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
        public void CreateITestAdapter_FromTestType_SetPropertyTest()
        {
            var testDateTime = DateTime.Now;
            var testGuid = Guid.NewGuid();

            var testObject = new PropertyTest();

            var testAdapter = testObject.CreateAdapter<IPropertyTestAdapter>();

            Assert.NotNull(testAdapter);

            testAdapter.StringProperty = "Test";
            testAdapter.Int16Property = 20000;
            testAdapter.Int32Property = 2000000;
            testAdapter.Int64Property = 200000000;
            testAdapter.FloatProperty = 3.14F;
            testAdapter.DoubleProperty = 3.14;
            testAdapter.BooleanProperty = true;
            testAdapter.DateTimeProperty = testDateTime;
            testAdapter.GuidProperty = testGuid;

            Assert.Equal("Test", testObject.StringProperty);
            Assert.Equal(20000, testObject.Int16Property);
            Assert.Equal(2000000, testObject.Int32Property);
            Assert.Equal(200000000, testObject.Int64Property);
            Assert.Equal(3.14F, testObject.FloatProperty);
            Assert.Equal(3.14, testObject.DoubleProperty);
            Assert.Equal(true, testObject.BooleanProperty);
            Assert.Equal(testDateTime, testObject.DateTimeProperty);
            Assert.Equal(testGuid, testObject.GuidProperty);
        }

        [Fact]
        public void CreateIIndexPropertyAdapter_FromTestType_Success()
        {
            var testDateTime = DateTime.Now;
            var testGuid = Guid.NewGuid();

            var testIndexObject = new IndexProperty(5);

            testIndexObject[1] = "Test";
            testIndexObject[1, 100] = 200;

            var testIndexAdapter = testIndexObject.CreateAdapter<IIndexPropertyAdapter>();

            Assert.NotNull(testIndexAdapter);
            Assert.Equal("Test", testIndexAdapter[1]);
            Assert.Equal(200, testIndexAdapter[1, 100]);
        }
    }
}