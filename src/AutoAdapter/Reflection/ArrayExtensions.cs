namespace AutoAdapter.Reflection
{
    /// <summary>
    /// Array extension methods.
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Copies an array and adds extra elements.
        /// </summary>
        /// <typeparam name="T">The type of array to copy.</typeparam>
        /// <param name="array">The array to copy.</param>
        /// <param name="extras">The extra elements to add</param>
        /// <returns>A new array containing the orignal array and extra elements.</returns>
        public static T[] CopyToAndAppendExtras<T>(this T[] array, params T[] extras)
        {
            if (array != null)
            {
                T[] newArray = new T[array.Length + extras.Length];
                array.CopyTo(newArray, 0);
                if (extras.IsNullOrEmpty() == false)
                {
                    extras.CopyTo(newArray, array.Length);
                }

                return newArray;
            }

            return default(T[]);
        }
    }
}
