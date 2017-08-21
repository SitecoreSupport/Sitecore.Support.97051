namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.LuceneProvider;
    using System;

    public class LuceneUpdateContext : Sitecore.ContentSearch.LuceneProvider.LuceneUpdateContext
    {
        public LuceneUpdateContext(ILuceneProviderIndex index) : base(index)
        {
            this.ReplaceLockScope(index);
        }

        public LuceneUpdateContext(ILuceneProviderIndex index, ICommitPolicyExecutor commitPolicyExecutor) : base(index, commitPolicyExecutor)
        {
            this.ReplaceLockScope(index);
        }

        private void ReplaceLockScope(ILuceneProviderIndex index)
        {
            LockScope scope = (LockScope)this.scope;
            this.scope = new LockScope(index.Directory, "write.lock");
        }
    }
}
