namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.LuceneProvider;
    using Sitecore.ContentSearch.Maintenance;
    using System;

    public class LuceneIndex : Sitecore.ContentSearch.LuceneProvider.LuceneIndex
    {
        public LuceneIndex(string name, string folder, IIndexPropertyStore propertyStore) : base(name, folder, propertyStore)
        {
        }

        public override IProviderUpdateContext CreateUpdateContext()
        {
            base.EnsureInitialized();
            ICommitPolicyExecutor commitPolicyExecutor = (ICommitPolicyExecutor)base.CommitPolicyExecutor.Clone();
            commitPolicyExecutor.Initialize(this);
            return new Sitecore.Support.ContentSearch.LuceneProvider.LuceneUpdateContext(this, commitPolicyExecutor);
        }
    }
}
