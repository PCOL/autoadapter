using AutoAdapter;

namespace AutoAdapterUnitTests.Resources
{
    [Adapter("AutoAdapterUnitTests.Resources.ChildAdaptee")]
    public interface IChildAdapter
    {
        string Property { get; set; }

        string Method(string paremeter);
    }
}