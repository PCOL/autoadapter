using System;
using System.Dynamic;
using AutoAdapter;
using AutoAdapter.Extensions;
using AutoAdapterUnitTests.Resources;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoAdapterUnitTests
{
    public class DynamicObjectAdapterTests
    {
        [Fact]
        public void DynamicObject_PropertyAdapter_Test()
        {
            var testDateTime = DateTime.Now;
            var testGuid = Guid.NewGuid();

            var services = new ServiceCollection();
            services.AddSingleton<IAdapterFactoryExtension>(new DynamicObjectAdapterFactoryExtension());
            var serviceProvider = services.BuildServiceProvider();

            var dynamicObj = new TestDynamicObject();

            var dynamicObjAdapter = dynamicObj.CreateAdapter<IPropertyTestAdapter>(serviceProvider);
            dynamicObjAdapter.StringProperty = "Test";
            dynamicObjAdapter.Int16Property = 20000;
            dynamicObjAdapter.Int32Property = 2000000;
            dynamicObjAdapter.Int64Property = 200000000;
            dynamicObjAdapter.FloatProperty = 3.14F;
            dynamicObjAdapter.DoubleProperty = 3.14;
            dynamicObjAdapter.BooleanProperty = true;
            dynamicObjAdapter.DateTimeProperty = testDateTime;
            dynamicObjAdapter.GuidProperty = testGuid;

            Assert.NotNull(dynamicObjAdapter);

            dynamic obj = dynamicObj;
            Assert.Equal("Test", obj.StringProperty);
            Assert.Equal(20000, obj.Int16Property);
            Assert.Equal(2000000, obj.Int32Property);
            Assert.Equal(200000000, obj.Int64Property);
            Assert.Equal(3.14F, obj.FloatProperty);
            Assert.Equal(3.14, obj.DoubleProperty);
            Assert.Equal(true, obj.BooleanProperty);
            Assert.Equal(testDateTime, obj.DateTimeProperty);
            Assert.Equal(testGuid, obj.GuidProperty);
        }

        [Fact]
        public void DynamicObject_IndexProperty_Success()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IAdapterFactoryExtension>(new DynamicObjectAdapterFactoryExtension());
            var serviceProvider = services.BuildServiceProvider();

            var dynamicObj = new TestDynamicObject();

            var dynamicObjAdapter = dynamicObj.CreateAdapter<IIndexPropertyAdapter>(serviceProvider);
            dynamicObjAdapter[0] = "Test";
            dynamicObjAdapter[0, 2] = 2000;
            Assert.NotNull(dynamicObjAdapter);
            Assert.True(dynamicObjAdapter.IsAdaptedObject());

            dynamic obj = dynamicObj;
            Assert.Equal("Test", obj[0]);
            Assert.Equal(2000, obj[0, 2]);
        }
    }
}