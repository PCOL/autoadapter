namespace AutoAdapterUnitTests
{
    using System;
    using AutoAdapter;
    using global::AutoAdapterUnitTests.Resources;

    public interface IStaticPropertyAdapter
    {
        [AdapterImpl(TargetBinding = AdapterBinding.Static, TargetType = typeof(StaticPropertyAdaptee))]
        string StringProperty { get; set; }

        [AdapterImpl(TargetBinding = AdapterBinding.Static, TargetType = typeof(StaticPropertyAdaptee))]
        short Int16Property { get; set; }

        [AdapterImpl(TargetBinding = AdapterBinding.Static, TargetType = typeof(StaticPropertyAdaptee))]
        int Int32Property { get; set; }

        [AdapterImpl(TargetBinding = AdapterBinding.Static, TargetType = typeof(StaticPropertyAdaptee))]
        long Int64Property { get; set; }

        [AdapterImpl(TargetBinding = AdapterBinding.Static, TargetType = typeof(StaticPropertyAdaptee))]
        float FloatProperty { get; set; }

        [AdapterImpl(TargetBinding = AdapterBinding.Static, TargetType = typeof(StaticPropertyAdaptee))]
        double DoubleProperty { get; set; }

        [AdapterImpl(TargetBinding = AdapterBinding.Static, TargetType = typeof(StaticPropertyAdaptee))]
        bool BooleanProperty { get; set; }

        [AdapterImpl(TargetBinding = AdapterBinding.Static, TargetType = typeof(StaticPropertyAdaptee))]
        DateTime DateTimeProperty { get; set; }

        [AdapterImpl(TargetBinding = AdapterBinding.Static, TargetType = typeof(StaticPropertyAdaptee))]
        Guid GuidProperty { get; set; }
    }

}