namespace AutoAdapterUnitTests.Resources
{
    using System;

    public static class StaticPropertyAdaptee
    {
        public static string StringProperty { get; set; }

        public static short Int16Property { get; set; }

        public static int Int32Property { get; set; }

        public static long Int64Property { get; set; }

        public static float FloatProperty { get; set; }

        public static double DoubleProperty { get; set; }

        public static bool BooleanProperty { get; set; }

        public static DateTime DateTimeProperty { get; set; }

        public static Guid GuidProperty { get; set; }
    }
}