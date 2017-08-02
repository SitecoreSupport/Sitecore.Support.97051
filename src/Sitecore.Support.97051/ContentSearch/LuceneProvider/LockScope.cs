namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    using Lucene.Net.Store;
    using Sitecore.ContentSearch.LuceneProvider;
    using System;
    using System.Reflection;

    public class LockScope : Sitecore.ContentSearch.LuceneProvider.LockScope
    {
        private readonly FieldInfo releaseFlagField;

        public LockScope(Directory directory, string lockName) : base(directory, lockName)
        {
            this.releaseFlagField = typeof(LockScope).GetField("releaseFlag", BindingFlags.NonPublic | BindingFlags.Instance);
            if (this.releaseFlagField != null)
            {
                this.releaseFlagField.SetValue(this, false);
            }
        }
    }
}
