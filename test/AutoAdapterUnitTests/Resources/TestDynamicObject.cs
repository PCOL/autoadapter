namespace AutoAdapterUnitTests.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;

    public class TestDynamicObject
        : DynamicObject
    {
        private Dictionary<string, object> properties = new Dictionary<string, object>();

        private Dictionary<string, object> indexProperties = new Dictionary<string, object>();

        public TestDynamicObject()
        {
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return this.properties.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this.properties[binder.Name] = value;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            string key = string.Join("-", indexes.Select(i => i.ToString()));
            return this.indexProperties.TryGetValue(key, out result);
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            string key = string.Join("-", indexes.Select(i => i.ToString()));
            this.indexProperties[key] = value;
            return true;
        }
    }
}