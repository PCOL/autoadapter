using System;

namespace AutoAdapterUnitTests.Resources
{
    public class GenericAdaptee<T>
    {
        public T Property { get; set; }

        public bool GetProperty<U>(out U property)
        {
            property = default(U);
            if (typeof(T) == typeof(U))
            {
                property = (U)(object)this.Property;
                return true;
            }

            return false;
        }
    }
}