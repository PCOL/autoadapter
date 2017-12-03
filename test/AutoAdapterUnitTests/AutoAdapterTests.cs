using System;
using AutoAdapter;
using AutoAdapter.Reflection;
using AutoAdapterUnitTests.Resources;
using Xunit;

namespace AutoAdapterUnitTests
{
    public class AutoAdapterTests
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

        [Fact]
        public void CreateIAdapterWithChildAdapter_FromAdapteeWithChildAdaptee_TestChild()
        {
            var adaptee = new AdapteeWithChildAdaptee()
            {
                Child = new ChildAdaptee()
                {
                    Property = "TEST"
                }
            };

            var adapter = adaptee.CreateAdapter<IAdapterWithChildAdapter>();

            Assert.NotNull(adapter);
            Assert.NotNull(adapter.Child);
            Assert.Equal("TEST", adapter.Child.Property);
            Assert.Equal("Test", adapter.Child.Method("Test"));
        }

        [Fact]
        public void CreateIAdapterWithChildAdapter_FromAdapteeWithChildAdaptee_TestChildren()
        {
            var adaptee = new AdapteeWithChildAdaptee()
            {
                Children = new[]
                {
                    new ChildAdaptee()
                    {
                        Property = "One"
                    },
                    new ChildAdaptee()
                    {
                        Property = "Two"
                    },
                    new ChildAdaptee()
                    {
                        Property = "Three"
                    }
                }
            };

            var adapter = adaptee.CreateAdapter<IAdapterWithChildAdapter>();

            Assert.NotNull(adapter);
            Assert.NotNull(adapter.Children);
            Assert.Equal(adaptee.Children.Length, adapter.Children.Length);
            for (int i = 0; i < adaptee.Children.Length; i++)
            {
                Assert.Equal(adaptee.Children[i].Property, adapter.Children[i].Property);
            }
        }

        [Fact]
        public void CreateIAdapterWithChildAdapter_FromAdapteeWithChildAdaptee_TestTryGetChild()
        {
            var adaptee = new AdapteeWithChildAdaptee()
            {
                Child = new ChildAdaptee()
                {
                    Property = "Test"
                }
            };

            var adapter = adaptee.CreateAdapter<IAdapterWithChildAdapter>();

            Assert.NotNull(adapter);

            var result = adapter.TryGetChild(out IChildAdapter child);

            Assert.True(result);
            Assert.NotNull(child);
            Assert.Equal("Test", child.Property);
        }

        [Fact]
        public void CreateIAdapterWithChildAdapter_FromAdapteeWithChildAdaptee_TestTryGetChildren()
        {
            var adaptee = new AdapteeWithChildAdaptee()
            {
                Children = new[]
                {
                    new ChildAdaptee()
                    {
                        Property = "One"
                    },
                    new ChildAdaptee()
                    {
                        Property = "Two"
                    },
                    new ChildAdaptee()
                    {
                        Property = "Three"
                    },
                }
            };

            var adapter = adaptee.CreateAdapter<IAdapterWithChildAdapter>();

            Assert.NotNull(adapter);

            var result = adapter.TryGetChildren(out IChildAdapter[] children);

            Assert.True(result);
            Assert.NotNull(children);
            for (int i = 0; i < adaptee.Children.Length; i++)
            {
                Assert.Equal(adaptee.Children[i].Property, children[i].Property);
            }
        }

        [Fact]
        public void CreateIAdapterWithChildAdapter_FromAdapteeWithChildAdaptee_TestActionParameter()
        {
            var adaptee = new AdapteeWithChildAdaptee()
            {
                Child = new ChildAdaptee()
                {
                    Property = "Test"
                }
            };

            var adapter = adaptee.CreateAdapter<IAdapterWithChildAdapter>();

            Assert.NotNull(adapter);

            IChildAdapter calledChild = null;
            adapter.ActionParameter(
                (child) =>
                {
                    calledChild = child;
                });

            Assert.NotNull(calledChild);
            Assert.Equal("Test", calledChild.Property);
        }

        [Fact]
        public void CreateIAdapterWithChildAdapter_FromAdapteeWithChildAdaptee_TestFuncParameter()
        {
            var adaptee = new AdapteeWithChildAdaptee();

            var adapter = adaptee.CreateAdapter<IAdapterWithChildAdapter>();

            Assert.NotNull(adapter);

            adapter.FuncParameter(
                () =>
                {
                    return new ChildAdaptee()
                    {
                        Property = "Test"
                    }.CreateAdapter<IChildAdapter>();
                });

            Assert.NotNull(adaptee.Child);
            Assert.Equal("Test", adaptee.Child.Property);
        }

        [Fact]
        public void CreateIArrayAdapter_FromArrayAdaptee()
        {
            var adaptee = new ArrayAdaptee()
            {
                ArrayProperty = new[]
                {
                    "One",
                    "Two",
                    "Three"
                }
            };

            var adapter = adaptee.CreateAdapter<IArrayAdapter>();
            Assert.NotNull(adapter);
            Assert.NotNull(adapter.ArrayProperty);
            Assert.Equal(adaptee.ArrayProperty.Length, adapter.ArrayProperty.Length);
            for (int i = 0; i < adaptee.ArrayProperty.Length; i++)
            {
                Assert.Equal(adaptee.ArrayProperty[i], adapter.ArrayProperty[i]);
            }
        }

        [Fact]
        public void CreateIExtensionMethodAdapter_FromExtensionMethodAdaptee()
        {
            var adaptee = new ExtensionMethodAdaptee()
            {
                Child = new ChildAdaptee()
                {
                    Property = "Test"
                }
            };

            var adapter = adaptee.CreateAdapter<IExtensionMethodAdapter>();

            Assert.NotNull(adapter);
            Assert.True(adapter.TryGetChild(out IChildAdapter child));
            Assert.NotNull(child);
            Assert.Equal("Test", child.Property);
        }
    }
}