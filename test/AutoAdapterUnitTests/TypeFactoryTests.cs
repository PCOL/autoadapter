using System;
using AutoAdapter.Reflection;
using Xunit;

namespace AutoAdapterUnitTests
{
    public class TypeFactoryTests
    {
        [Fact]
        public void NewTypeFactory_WithNullAssemblyName_Throws()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(
                () =>
                {
                    new TypeFactory(null, "testModule");
                });

            Assert.Equal("Value cannot be null.\nParameter name: assemblyName", ex.Message);
        }

        [Fact]
        public void NewTypeFactory_WithEmptyAssemblyName_Throws()
        {
            Exception ex = Assert.Throws<ArgumentException>(
                () =>
                {
                    new TypeFactory(string.Empty, "testModule");
                });

            Assert.Equal("Value cannot be empty.\nParameter name: assemblyName", ex.Message);
        }

        [Fact]
        public void NewTypeFactory_WithNullModuleName_Throws()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(
                () =>
                {
                    new TypeFactory("assemblyName", null);
                });

            Assert.Equal("Value cannot be null.\nParameter name: moduleName", ex.Message);
        }

        [Fact]
        public void NewTypeFactory_WithEmptyModuleName_Throws()
        {
            Exception ex = Assert.Throws<ArgumentException>(
                () =>
                {
                    new TypeFactory("assemblyName", string.Empty);
                });

            Assert.Equal("Value cannot be empty.\nParameter name: moduleName", ex.Message);
        }

        [Fact]
        public void NewTypeFactory_WithAssemblyAndModuleName_CreatesATypeFactory()
        {
            var typeFactory = new TypeFactory("TestAssembly", "TestModule");

            Assert.NotNull(typeFactory.ModuleBuilder);
            Assert.NotNull(typeFactory.ModuleBuilder.Assembly);
            Assert.Equal("TestAssembly", typeFactory.ModuleBuilder.Assembly.GetName().Name);
            Assert.Equal("<In Memory Module>", typeFactory.ModuleBuilder.Name);
        }

        [Fact]
        public void TypeFactory_DefineType_CreatesType()
        {
            var typeFactory = new TypeFactory("TestAssembly", "TestModule");
            var testTypeBuilder = typeFactory
                .ModuleBuilder
                .DefineType("TestType");

            Assert.NotNull(testTypeBuilder);
            Assert.Equal("TestType", testTypeBuilder.Name);

            var testType = testTypeBuilder.CreateType();

            Assert.NotNull(testType);
            Assert.Equal("TestType", testType.Name);
        }
    }
}
