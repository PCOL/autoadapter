namespace AutoAdapterUnitTests.Resources
{
    public class ChildAdaptee
    {
        public string Property { get; set; }

        public string Method(string parameter)
        {
            return parameter;
        }
    }

}