namespace AutoAdapterUnitTests.Resources
{
    public class AdapteeWithChildAdaptee
    {
        public ChildAdaptee Child { get; set; }

        public ChildAdaptee[] Children { get; set; }

        public bool TryGetChild(out ChildAdaptee child)
        {
            child = null;
            if (this.Child != null)
            {
                child = this.Child;
                return true;
            }

            return false;
        }

    }
}