namespace AutoAdapterUnitTests.Resources
{
    public interface IGenericAdapter<T>
    {
        T Property { get; set; }

        bool GetProperty<U>(out U property);
    }
}