namespace AdapterTestApp
{
    using AutoAdapter;

    [Adapter("AdapterTestApp.AdaptedChildObject")]
    public interface IAdaptedChild1
    {
        string Test { get; set; }
    }
}
