using System;

namespace AutoAdapterUnitTests.Resources
{
    public delegate bool CheckHandler(ChildAdaptee child);

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

        public bool TryGetChildren(out ChildAdaptee[] children)
        {
            children = null;
            if (this.Children != null)
            {
                children = this.Children;
                return true;
            }

            return false;
        }

        public void ActionParameter(Action<ChildAdaptee> action)
        {
            action(this.Child);
        }

        public void FuncParameter(Func<ChildAdaptee> func)
        {
            this.Child = func();
        }

        public bool PredicateParameter(Predicate<ChildAdaptee> predicate)
        {
            return predicate(this.Child);
        }

        public bool Check(CheckHandler handler)
        {
            return handler(this.Child);
        }
   }
}