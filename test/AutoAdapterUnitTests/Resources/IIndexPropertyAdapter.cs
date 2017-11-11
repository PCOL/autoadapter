namespace AutoAdapterUnitTests.Resources
{
    public interface IIndexPropertyAdapter
    {
        string this[int index] { get; set; }

        short this[int indexX, int indexY] { get; set; }
    }
}