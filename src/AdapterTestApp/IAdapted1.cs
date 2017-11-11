namespace AdapterTestApp
{
    using AutoAdapter;

    [Adapter(typeof(AdaptedTestObject))]
    public interface IAdapted1
    {
        string Name { get; set; }

        ////IAdaptedChild1 Child { get; set; }

        IAdaptedChild1 GetChild();

        void SetChild(IAdaptedChild1 child);
    }
}
