namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.LuceneProvider;
    using System;
    using System.Reflection;

    public class LuceneUpdateContext : Sitecore.ContentSearch.LuceneProvider.LuceneUpdateContext
    {
        private readonly PropertyInfo commitPolicyExecutorProperty;

        public LuceneUpdateContext(ILuceneProviderIndex index) : base(index)
        {
            this.commitPolicyExecutorProperty = typeof(Sitecore.ContentSearch.LuceneProvider.LuceneUpdateContext).GetProperty("CommitPolicyExecutor", BindingFlags.Public | BindingFlags.Instance);
            base.scope = new LockScope(index.Directory, "write.lock");
        }

        public LuceneUpdateContext(ILuceneProviderIndex index, ICommitPolicyExecutor commitPolicyExecutor) : this(index)
        {
            if (commitPolicyExecutor == null)
            {
                throw new ArgumentNullException("commitPolicyExecutor");
            }
            if (this.commitPolicyExecutorProperty != null)
            {
                this.commitPolicyExecutorProperty.SetValue(this, commitPolicyExecutor);
            }
        }
    }
}
