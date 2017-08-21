namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Sitecore.Configuration;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Diagnostics;
    using Sitecore.ContentSearch.LuceneProvider;
    using Sitecore.ContentSearch.Maintenance;
    using Sitecore.ContentSearch.Utilities;
    using Sitecore.IO;
    using Sitecore.Search;
    using System;
    using System.IO;

    public class SwitchOnRebuildLuceneIndex : Sitecore.Support.ContentSearch.LuceneProvider.LuceneIndex
    {
        private Lucene.Net.Store.Directory _fullRebuildDirectory;

        public SwitchOnRebuildLuceneIndex(string name, string folder, IIndexPropertyStore propertyStore) : base(name, folder, propertyStore)
        {
        }

        protected override IProviderUpdateContext CreateFullRebuildContext()
        {
            base.EnsureInitialized();
            ICommitPolicyExecutor commitPolicyExecutor = (ICommitPolicyExecutor)base.CommitPolicyExecutor.Clone();
            commitPolicyExecutor.Initialize(this);
            return new Sitecore.Support.ContentSearch.LuceneProvider.LuceneFullRebuildContext(this, commitPolicyExecutor);
        }

        public override IProviderUpdateContext CreateUpdateContext()
        {
            base.EnsureInitialized();
            ICommitPolicyExecutor commitPolicyExecutor = (ICommitPolicyExecutor)base.CommitPolicyExecutor.Clone();
            commitPolicyExecutor.Initialize(this);
            return new Sitecore.Support.ContentSearch.LuceneProvider.LuceneUpdateContext(this, commitPolicyExecutor);
        }

        public virtual IndexWriter CreateWriter(bool recreate, Lucene.Net.Store.Directory directory)
        {
            base.EnsureInitialized();
            using (new IndexLocker(directory.MakeLock("write.lock")))
            {
                recreate |= !IndexReader.IndexExists(directory);
                IContentSearchConfigurationSettings instance = base.Locator.GetInstance<IContentSearchConfigurationSettings>();
                IndexWriter writer = new IndexWriter(directory, ((LuceneIndexConfiguration)this.Configuration).Analyzer, recreate, IndexWriter.MaxFieldLength.UNLIMITED);
                LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy(writer);
                writer.TermIndexInterval = instance.TermIndexInterval();
                writer.MergeFactor = instance.IndexMergeFactor();
                writer.MaxMergeDocs = instance.MaxMergeDocs();
                writer.UseCompoundFile = instance.UseCompoundFile();
                mp.MaxMergeMB = instance.MaxMergeMB();
                mp.MinMergeMB = instance.MinMergeMB();
                mp.CalibrateSizeByDeletes = instance.CalibrateSizeByDeletes();
                writer.SetMergePolicy(mp);
                writer.SetRAMBufferSizeMB((double)instance.RamBufferSize());
                writer.SetMaxBufferedDocs(instance.MaxDocumentBufferSize());
                base.InitializeWithCustomScheduler(writer, instance.ConcurrentMergeSchedulerThreads());
                return writer;
            }
        }

        protected override void InitializeDirectory()
        {
            CrawlingLog.Log.Debug($"[Index={this.Name}] Creating primary and secondary directories", null);
            string str = Path.IsPathRooted(base.FolderName) ? base.FolderName : FileUtil.MapPath(FileUtil.MakePath(Settings.IndexFolder, base.FolderName));
            Lucene.Net.Store.FSDirectory directory = this.CreateDirectory(str);
            Lucene.Net.Store.FSDirectory directory2 = this.CreateDirectory(str + "_sec");
            string str2 = this.PropertyStore.Get(IndexProperties.ReadUpdateDirectory);
            if (!string.IsNullOrEmpty(str2))
            {
                CrawlingLog.Log.Debug(string.Format("[Index={0}] Resolving directories from index property store for index '{0}'", this.Name), null);
                if (str2.Equals(directory.ToString()))
                {
                    this.Directory = directory;
                    this.FullRebuildDirectory = directory2;
                }
                else
                {
                    this.Directory = directory2;
                    this.FullRebuildDirectory = directory;
                }
            }
            else
            {
                CrawlingLog.Log.Debug($"[Index={this.Name}] Resolving directories by last time modified.", null);
                long num = IndexReader.LastModified(directory);
                long num2 = IndexReader.LastModified(directory2);
                CrawlingLog.Log.Debug($"[Index={this.Name}] Primary directory last modified = '{num}'.", null);
                CrawlingLog.Log.Debug($"[Index={this.Name}] Secondary directory last modified = '{num2}'.", null);
                if (num <= num2)
                {
                    this.Directory = directory2;
                    this.FullRebuildDirectory = directory;
                }
                else
                {
                    this.Directory = directory;
                    this.FullRebuildDirectory = directory2;
                }
            }
            CrawlingLog.Log.Debug($"[Index={this.Name}] ReadUpdateDirectory is set to '{this.Directory}'.", null);
            CrawlingLog.Log.Debug($"[Index={this.Name}] FullRebuildDirectory is set to '{this.FullRebuildDirectory}'.", null);
        }

        public override void Rebuild()
        {
            base.Rebuild();
            this.SwitchDirectories();
        }

        public override void Reset()
        {
            base.EnsureInitialized();
            using (new IndexLocker(this.FullRebuildDirectory.MakeLock("write.lock")))
            {
                using (IndexWriter writer = new IndexWriter(this.FullRebuildDirectory, ((LuceneIndexConfiguration)this.Configuration).Analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    base.InitializeWithCustomScheduler(writer, -1);
                }
            }
            this.CreateWriter(true, this.FullRebuildDirectory).Dispose();
        }

        protected void SwitchDirectories()
        {
            Lucene.Net.Store.Directory directory = this.Directory;
            this.Directory = this.FullRebuildDirectory;
            this.FullRebuildDirectory = directory;
            CrawlingLog.Log.Debug($"[Index={this.Name}] Switching directories", null);
            CrawlingLog.Log.Debug($"[Index={this.Name}] Read/Update directory is set to '{this.Directory}'.", null);
            CrawlingLog.Log.Debug($"[Index={this.Name}] Full Rebuild directory is set to '{this.FullRebuildDirectory}'.", null);
        }

        public virtual Lucene.Net.Store.Directory FullRebuildDirectory
        {
            get
            {
                return this._fullRebuildDirectory;
            } 
            protected set
            {
                this._fullRebuildDirectory = value;
                this.PropertyStore.Set(IndexProperties.FullRebuildDirectory, value.ToString());
            }
        }
    }
}
