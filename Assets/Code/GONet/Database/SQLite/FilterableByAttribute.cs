using System;

namespace GONet.Database.Sqlite
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FilterableByAttribute : Attribute
    {
        public bool CreateIndex { get; private set; }

        public FilterableByAttribute(bool createIndex)
        {
            CreateIndex = createIndex;
        }

        public FilterableByAttribute()
            : this(false)
        { }
    }
}
