using System.IO;

namespace SevenZipExtractor
{
    public class Entry
    {
        private readonly IInArchive archive;
        private readonly uint index;

        internal Entry(IInArchive archive, uint index)
        {
            this.archive = archive;
            this.index = index;
        }

        public string FileName { get; internal set; }
        public bool IsFolder { get; internal set; }

        public void Extract(string fileName)
        {
            using (FileStream fileStream = File.Create(fileName))
            {
                this.Extract(fileStream);
            }
        }
        public void Extract(Stream stream)
        {
            this.archive.Extract(new[] { this.index }, 1, 0, new ArchiveStreamCallback(this.index, stream));
        }

        // Rio's 追記
        public long Size
        {
            get
            {
                PropVariant value = new PropVariant();
                archive.GetProperty(index, ItemPropId.kpidSize, ref value);
                return value.longValue;
            }
        }

        public System.DateTime LastWriteTime
        {
            get
            {
                PropVariant value = new PropVariant();
                archive.GetProperty(index, ItemPropId.kpidLastWriteTime, ref value);
                return System.DateTime.FromFileTime(value.longValue);
            }
        }

        public bool IsSolid
        {
            get
            {
                PropVariant value = new PropVariant();
                archive.GetProperty(index, ItemPropId.kpidSolid, ref value);
                return value.longValue == 0;
            }
        }
    }
}