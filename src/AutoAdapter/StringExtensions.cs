namespace AutoAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Extension methods for the <see cref="string"/> type.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a value indicating whether the specified <see cref="T:System.String"/> object occurs within this string.
        /// </summary>
        /// <param name="str">The string to search.</param>
        /// <param name="value">The string to seek.</param>
        /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search. </param>
        /// <returns>True if the <paramref name="value" /> parameter occurs within this string, or if <paramref name="value" /> is the empty string (""); otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="value" /> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="comparisonType" /> is not a valid <see cref="T:System.StringComparison" /> value.</exception>
        public static bool Contains(this string str, string value, StringComparison comparisonType)
        {
            if (str != null)
            {
                return str.IndexOf(value, comparisonType) != -1;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the specified string is null or an <see cref="F:System.String.Empty"/> string.
        /// </summary>
        /// <param name="value">The string to test. </param>
        /// <returns>True if the <paramref name="value" /> parameter is null or an empty string (""); otherwise, false.</returns>
        /// <filterpriority>1</filterpriority>
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Indicates whether a specified string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <returns>True if the <paramref name="value" /> parameter is null or <see cref="F:System.String.Empty"/>, or if <paramref name="value"/> consists exclusively of white-space characters.</returns>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Indicates whether a specified string is empty only.
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <returns>true if the string is not null and has zero length</returns>
        public static bool IsEmpty(this string value)
        {
            return value != null && value.Length == 0;
        }

        /// <summary>
        /// Tests if a string contains another string ignoring the case
        /// </summary>
        /// <param name="str">The string to search</param>
        /// <param name="value">The string to find</param>
        /// <returns>true if the search is successful, false otherwise</returns>
        public static bool ContainsIgnoreCase(this string str, string value)
        {
            return str.Contains(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests if two strings are equal ignoring the case
        /// </summary>
        /// <param name="str">The string to search</param>
        /// <param name="value">The string to test</param>
        /// <returns>true if the strings are equal, false otherwise</returns>
        public static bool EqualsIgnoreCase(this string str, string value)
        {
            return string.Compare(str, value, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Tests if the two strings either equal or both have no content (meaning they are both null or empty)
        /// </summary>
        /// <param name="str">The string to search</param>
        /// <param name="value">The string to test</param>
        /// <returns>true if the strings are equal or both have no content, false otherwise</returns>
        public static bool EqualsOrNeitherHaveContent(this string str, string value)
        {
            return (string.IsNullOrEmpty(str) && string.IsNullOrEmpty(value)) || str.Equals(value);
        }

        /// <summary>
        /// Tests a string and returns either the strings contents if it is not null, or a string containing the word null if it is null.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>The contents of the string if it is not null, otherwise a string containing the word null if it is null.</returns>
        public static string StringOrNull(this string str)
        {
            const string NullString = "<NULL>";
            return str ?? NullString;
        }

        /// <summary>
        /// Gets the hash code of a string that is safe for null strings
        /// </summary>
        /// <param name="str">string to get the hash code of</param>
        /// <returns>Hash code value of the string or 0 if string is null</returns>
        public static int SafeHashCode(this string str)
        {
            if (str == null)
            {
                return string.Empty.GetHashCode();
            }
            else
            {
                return str.GetHashCode();
            }
        }

        /// <summary>
        /// Converts a string to its unicode base 64 string equivalent.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>A base 64 string.</returns>
        public static string ToBase64(this string str)
        {
            return str.ToBase64(System.Text.Encoding.Unicode);
        }

        /// <summary>
        /// Converts a string to its base 64 string equivalent.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="encoding">The byte encoding to use.</param>
        /// <returns>A base 64 string.</returns>
        public static string ToBase64(this string str, System.Text.Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            if (str != null)
            {
                byte[] bytes = encoding.GetBytes(str);
                return Convert.ToBase64String(bytes);
            }

            return null;
        }

        /// <summary>
        /// Converts a base 64 string to it unicode string equivalent.
        /// </summary>
        /// <param name="str">The base 64 string to convert.</param>
        /// <returns>A string.</returns>
        public static string FromBase64(this string str)
        {
            return str.FromBase64(System.Text.Encoding.Unicode);
        }

        /// <summary>
        /// Converts a base 64 string to it string equivalent.
        /// </summary>
        /// <param name="str">The base 64 string to convert.</param>
        /// <param name="encoding">The byte encoding to use.</param>
        /// <returns>A string.</returns>
        public static string FromBase64(this string str, System.Text.Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            if (str != null)
            {
                byte[] bytes = Convert.FromBase64String(str);
                return encoding.GetString(bytes);
            }

            return null;
        }

        /// <summary>
        /// Checks to see if the string is in a specified list of strings.
        /// </summary>
        /// <param name="str">The string to look for.</param>
        /// <param name="list">The list of strings to check.</param>
        /// <param name="comparisonType">The type of string comparison.</param>
        /// <returns><b>true</b> if the string is in the list; otherwise <b>false</b>.</returns>
        public static bool IsInList(this string str, IList<string> list, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (string.Compare(str, list[i], comparisonType) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks to see if the sub string matches a string in a specified list of strings.
        /// </summary>
        /// <param name="str">The string to look for.</param>
        /// <param name="index">The position of sub string.</param>
        /// <param name="length">The length of the sub string.</param>
        /// <param name="list">The list of strings to check.</param>
        /// <param name="comparisonType">The type of string comparison.</param>
        /// <returns><b>true</b> if the string is in the list; otherwise <b>false</b>.</returns>
        public static bool IsInList(this string str, int index, int length, IList<string> list, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (length == list[i].Length &&
                        string.Compare(str, index, list[i], 0, length, comparisonType) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a the string if it is not null and not empty; otherwise the alternate string is returned.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <param name="alternateString">The alternate string.</param>
        /// <returns>The string if it is not null and not empty; otherwise the alternate string.</returns>
        public static string OrIfNullOrEmpty(this string str, string alternateString)
        {
            if (str.IsNullOrEmpty() == false)
            {
                return str;
            }

            return alternateString;
        }

        /// <summary>
        /// Joins a list of items.
        /// </summary>
        /// <typeparam name="T">The type of list.</typeparam>
        /// <param name="list">The list instance.</param>
        /// <param name="separator">The separator.</param>
        /// <param name="function">The function to call for each element</param>
        /// <returns>A string containing the contents of the list, separated by the separator.</returns>
        public static string Join<T>(this IEnumerable<T> list, string separator, Func<T, string> function)
        {
            // Utility.ThrowIfArgumentIsNull(nameof(separator), separator);
            // Utility.ThrowIfArgumentIsNull(nameof(function), function);

            if (list != null)
            {
                StringBuilder str = new StringBuilder(1024);
                bool first = true;
                foreach (T item in list)
                {
                    if (first == false)
                    {
                        str.Append(separator);
                    }

                    str.Append(function(item));

                    first = false;
                }

                return str.ToString();
            }

            return string.Empty;
        }
    }
}