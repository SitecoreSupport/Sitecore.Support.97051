namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.LuceneProvider;
    using Sitecore.ContentSearch.Maintenance;
    using Sitecore.ContentSearch.Utilities;
    using Sitecore.Diagnostics;
    using Sitecore.IO;
    using Sitecore.Search;
    using Sitecore.Support;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public class LuceneIndex : Sitecore.ContentSearch.LuceneProvider.LuceneIndex
    {
        private bool disposed2;
        private readonly IContentSearchConfigurationSettings settings;
        private volatile IndexWriter writer2;
        private readonly object writerSyncObj2;

        public LuceneIndex(string name, string folder, IIndexPropertyStore propertyStore) : base(name, folder, propertyStore)
        {
            this.settings = base.Locator.GetInstance<IContentSearchConfigurationSettings>();
            this.writerSyncObj2 = new object();
        }

        protected override Lucene.Net.Store.FSDirectory CreateDirectory(string folder)
        {
            Assert.ArgumentNotNullOrEmpty(folder, "folder");
            base.EnsureInitialized();
            DirectoryInfo path = new DirectoryInfo(folder);
            FileUtil.EnsureFolder(folder);
            Lucene.Net.Store.FSDirectory directory = Lucene.Net.Store.FSDirectory.Open(path, new Sitecore.ContentSearch.LuceneProvider.SitecoreLockFactory(path.FullName));
            using (new IndexLocker(directory.MakeLock("write.lock")))
            {
                if (IndexReader.IndexExists(directory))
                {
                    return directory;
                }
                using (IndexWriter writer = new IndexWriter(directory, ((LuceneIndexConfiguration)this.Configuration).Analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    this.InitializeWithCustomScheduler(writer, -1);
                }
            }
            return directory;
        }

        public override IProviderUpdateContext CreateUpdateContext()
        {
            base.EnsureInitialized();
            ICommitPolicyExecutor commitPolicyExecutor = (ICommitPolicyExecutor)base.CommitPolicyExecutor.Clone();
            commitPolicyExecutor.Initialize(this);
            return new Sitecore.Support.ContentSearch.LuceneProvider.LuceneUpdateContext(this, commitPolicyExecutor);
        }

        public override IndexWriter CreateWriter(bool recreate)
        {
            base.EnsureInitialized();
            this.EnsureWriterIsValid();
            if (this.writer2 == null)
            {
                lock (this.writerSyncObj2)
                {
                    if (this.writer2 == null)
                    {
                        IndexWriter writer;
                        using (new IndexLocker(this.Directory.MakeLock("write.lock")))
                        {
                            recreate |= !IndexReader.IndexExists(this.Directory);
                            writer = this.CreateWriterInstance2(recreate);
                        }
                        this.writer2 = writer;
                    }
                }
            }
            if (recreate)
            {
                this.writer2.DeleteAll();
                this.writer2.Commit();
            }
            return this.writer2;
        }

        private IndexWriter CreateWriterInstance2(bool recreate)
        {
            IndexWriter writer = new IndexWriter(this.Directory, ((LuceneIndexConfiguration)this.Configuration).Analyzer, recreate, IndexWriter.MaxFieldLength.UNLIMITED);
            LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy(writer);
            writer.TermIndexInterval = this.settings.TermIndexInterval();
            writer.MergeFactor = this.settings.IndexMergeFactor();
            writer.MaxMergeDocs = this.settings.MaxMergeDocs();
            writer.UseCompoundFile = this.settings.UseCompoundFile();
            mp.MaxMergeMB = this.settings.MaxMergeMB();
            mp.MinMergeMB = this.settings.MinMergeMB();
            mp.CalibrateSizeByDeletes = this.settings.CalibrateSizeByDeletes();
            writer.SetMergePolicy(mp);
            writer.SetRAMBufferSizeMB((double)this.settings.RamBufferSize());
            writer.SetMaxBufferedDocs(this.settings.MaxDocumentBufferSize());
            this.InitializeWithCustomScheduler(writer, this.settings.ConcurrentMergeSchedulerThreads());
            return writer;
        }

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed2)
            {
                this.DisposeWriter2();
                this.disposed2 = true;
                typeof(Sitecore.ContentSearch.LuceneProvider.LuceneIndex).GetField("disposed", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, true);
            }
        }

        private void DisposeWriter2()
        {
            if (this.writer2 != null)
            {
                StreamWriter infoStream = null;
                if (this.writer2.InfoStream != null)
                {
                    infoStream = this.writer2.InfoStream;
                }
                this.writer2.Dispose();
                if (infoStream != null)
                {
                    infoStream.Close();
                }
            }
        }

        private void EnsureWriterIsValid()
        {
            if (this.writer2 != null)
            {
                lock (this.writerSyncObj2)
                {
                    if (this.writer2 != null)
                    {
                        try
                        {
                            this.writer2.HasDeletions();
                        }
                        catch (AlreadyClosedException)
                        {
                            this.writer2 = null;
                        }
                    }
                }
            }
        }

        protected void InitializeWithCustomScheduler(IndexWriter writer, int threadsLimit = -1)
        {
            Sitecore.Support.ConcurrentMergeScheduler mergeScheduler = new Sitecore.Support.ConcurrentMergeScheduler();
            if (threadsLimit != -1)
            {
                mergeScheduler.MaxThreadCount = threadsLimit;
            }
            writer.SetMergeScheduler(mergeScheduler);
        }

        public override Lucene.Net.Store.Directory Directory
        {
            get { return base.Directory; }
            protected set
            {
                base.directory = value;
                Assert.IsNotNull(this.PropertyStore, "Property Store is not set");
                this.PropertyStore.Set(IndexProperties.ReadUpdateDirectory, value.ToString());
                if (this.writer2 != null)
                {
                    lock (this.writerSyncObj2)
                    {
                        if (this.writer2 != null)
                        {
                            this.DisposeWriter2();
                            this.writer2 = null;
                        }
                    }
                }
            }
        }
    }
}
