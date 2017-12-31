namespace AutoAdapterUnitTests
{
    using System;

    public interface IPropertyAdapter
    {
        string StringProperty { get; set; }

        short Int16Property { get; set; }

        int Int32Property { get; set; }

        long Int64Property { get; set; }

        float FloatProperty { get; set; }

        double DoubleProperty { get; set; }

        bool BooleanProperty { get; set; }

        DateTime DateTimeProperty { get; set; }

        Guid GuidProperty { get; set; }
    }

}