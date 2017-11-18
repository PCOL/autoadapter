using AutoAdapter;

namespace AutoAdapterUnitTests.Resources
{
    public interface IAdapterWithChildAdapter
    {
        IChildAdapter Child { get; }

        IChildAdapter[] Children { get; }
    }
}