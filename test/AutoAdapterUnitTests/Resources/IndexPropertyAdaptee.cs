namespace AutoAdapterUnitTests.Resources
{
    using System.Collections.Generic;

    public class IndexPropertyAdaptee
    {
        private string[] values;

        private Dictionary<string, short> shortValues = new Dictionary<string, short>();

        public IndexPropertyAdaptee(int count)
        {
            this.values = new string[count];
        }

        public string this[int index]
        {
            get
            {
                return this.values[index];
            }

            set
            {
                this.values[index] = value;
            }
        }

        public short this[int indexX, int indexY]
        {
            get
            {
                short value;
                if (this.shortValues.TryGetValue($"{indexX}-{indexY}", out value) == false)
                {
                    return 0;
                }

                return value;
            }

            set
            {
                this.shortValues[$"{indexX}-{indexY}"] = value;
            }
        }
    }
}