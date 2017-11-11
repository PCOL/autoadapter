namespace AdapterTestApp
{
    public class AdaptedTestObject
    {
        public string Name { get; set; }

        public string Address { get; set; }


        public  AdaptedChildObject Child { get; set; }

        public AdaptedChildObject GetChild()
        {
            return this.Child;
        }


        public void SetChild(AdaptedChildObject value)
        {
            this.Child = value;
        }

    }
}
