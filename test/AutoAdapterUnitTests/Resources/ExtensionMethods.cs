namespace AutoAdapterUnitTests.Resources
{
    public static class ExtensionMethods
    {
        public static bool TryGetChild(this ExtensionMethodAdaptee adaptee, out ChildAdaptee child)
        {
            child = adaptee.Child;
            return true;
        }
    }
}