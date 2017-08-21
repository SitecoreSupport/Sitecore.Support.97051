namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    using Sitecore.ContentSearch;
    using Sitecore.Diagnostics;
    using System;
    using Sitecore.ContentSearch.Utilities;

    public class LuceneFullRebuildContext : LuceneUpdateContext
    {
        public LuceneFullRebuildContext(SwitchOnRebuildLuceneIndex index, ICommitPolicyExecutor commitPolicyExecutor) : base(index, commitPolicyExecutor)
        {
            Assert.ArgumentNotNull(index, "index");
            base.scope = new LockScope(index.FullRebuildDirectory, "write.lock");
            base.writer = index.CreateWriter(false, index.FullRebuildDirectory);
            base.waitForMerges = index.Locator.GetInstance<IContentSearchConfigurationSettings>().WaitForMerges();
        }
    }
}
