using AutoAdapter;

namespace AutoAdapterUnitTests.Resources
{
    public interface IExtensionMethodAdapter
    {
        [AdapterImpl(TargetType = typeof(ExtensionMethods), TargetBinding = AdapterBinding.Static)]
        bool TryGetChild(out IChildAdapter child);
    }
}