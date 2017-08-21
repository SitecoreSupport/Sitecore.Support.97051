namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    using Lucene.Net.Store;
    using Sitecore.ContentSearch.Diagnostics;
    using Sitecore.ContentSearch.LuceneProvider;
    using Sitecore.Diagnostics;
    using System;
    using System.Reflection;

    public class LockScope : Sitecore.ContentSearch.LuceneProvider.LockScope
    {
        public LockScope(Directory directory) : this(directory, "write.lock")
        {
            Assert.ArgumentNotNull(directory, "directory");
        }

        public LockScope(Directory directory, string lockName) : base(directory, lockName)
        {
            FieldInfo field = typeof(LockScope).GetField("releaseFlag", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(this, false);
            }
            else
            {
                CrawlingLog.Log.Warn("SUPPORT LockScope: Can't get releaseFlag field...", null);
            }
        }
    }
}
