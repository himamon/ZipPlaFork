﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SevenZipExtractor
{
    public class ArchiveFile : IDisposable
    {
        private SevenZipHandle sevenZipHandle;
        private readonly IInArchive archive;
        private readonly InStreamWrapper archiveStream;
        private IList<Entry> entries;

        private string libraryFilePath;

        public ArchiveFile(string archiveFilePath, string libraryFilePath = null)
        {
            this.libraryFilePath = libraryFilePath;

            this.InitializeAndValidateLibrary();

            if (!File.Exists(archiveFilePath))
            {
                throw new SevenZipException("Archive file not found");
            }

            string extension = Path.GetExtension(archiveFilePath);

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new SevenZipException("Unable to guess format for file: " + archiveFilePath);
            }

            string fileExtension = extension.Trim('.').ToLowerInvariant();

            if (!Formats.ExtensionFormatMapping.ContainsKey(fileExtension))
            {
                throw new SevenZipException(fileExtension + " is not a known archive type");
            }

            KnownSevenZipFormat format = Formats.ExtensionFormatMapping[fileExtension];

            this.archive = this.sevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format]);
            this.archiveStream = new InStreamWrapper(File.OpenRead(archiveFilePath));
        }

        public ArchiveFile(Stream archiveStream, KnownSevenZipFormat format, string libraryFilePath = null)
        {
            this.libraryFilePath = libraryFilePath;

            this.InitializeAndValidateLibrary();

            if (archiveStream == null)
            {
                throw new SevenZipException("archiveStream is null");
            }

            this.archive = this.sevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format]);
            this.archiveStream = new InStreamWrapper(archiveStream);
        }

        public IList<Entry> Entries
        {
            get
            {
                if (this.entries != null)
                {
                    return this.entries;
                }

                ulong checkPos = 32 * 1024;
                int open = this.archive.Open(this.archiveStream, ref checkPos, null);

                if (open != 0)
                {
                    throw new SevenZipException("Unable to open archive");
                }

                uint itemsCount = this.archive.GetNumberOfItems();

                this.entries = new List<Entry>();

                uint fileIndex = 0;

                for (; fileIndex < itemsCount; fileIndex++)
                {
                    string fileName = this.GetProperty<string>(fileIndex, ItemPropId.kpidPath);
                    bool isFolder = this.GetProperty<bool>(fileIndex, ItemPropId.kpidIsFolder);

                    this.entries.Add(new Entry(this.archive, fileIndex)
                    {
                        FileName = fileName,
                        IsFolder = isFolder
                    });
                }

                return this.entries;
            }
        }


        private T GetProperty<T>(uint fileIndex, ItemPropId name)
        {
            PropVariant propVariant = new PropVariant();
            this.archive.GetProperty(fileIndex, name, ref propVariant);
            return (T) propVariant.GetObject();
        }

        private void InitializeAndValidateLibrary()
        {
            if (string.IsNullOrWhiteSpace(this.libraryFilePath))
            {
                string suffix = IntPtr.Size == 4 ? "x86" : "x64"; // magic check

                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z-" + suffix + ".dll")))
                {
                    this.libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z-" + suffix + ".dll");
                }
                else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "7z-" + suffix + ".dll")))
                {
                    this.libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "7z-" + suffix + ".dll");
                }
                else if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll")))
                {
                    this.libraryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll");
                }
            }

            if (string.IsNullOrWhiteSpace(this.libraryFilePath))
            {
                throw new SevenZipException("libraryFilePath not set");
            }

            if (!File.Exists(this.libraryFilePath))
            {
                throw new SevenZipException("7z.dll not found");
            }

            try
            {
                this.sevenZipHandle = new SevenZipHandle(this.libraryFilePath);
            }
            catch (Exception e)
            {
                throw new SevenZipException("Unable to initialize SevenZipHandle", e);
            }
        }

        ~ArchiveFile()
        {
            this.Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (this.archiveStream != null)
            {
                this.archiveStream.Dispose();
            }

            if (this.archive != null)
            {
                Marshal.ReleaseComObject(this.archive);
            }

            if (this.sevenZipHandle != null)
            {
                this.sevenZipHandle.Dispose();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Rio's 追記
        
            
        public IEnumerable<Entry> EnumerableEntries
        {
            get
            {
                ulong checkPos = 32 * 1024;
                int open = this.archive.Open(this.archiveStream, ref checkPos, null);
                if (open != 0)
                {
                    throw new SevenZipException("Unable to open archive");
                }
                var entriesListCount = archive.GetNumberOfItems();
                
                for (uint fileIndex = 0; fileIndex < entriesListCount; fileIndex++)
                {
                    string fileName = this.GetProperty<string>(fileIndex, ItemPropId.kpidPath);
                    bool isFolder = this.GetProperty<bool>(fileIndex, ItemPropId.kpidIsFolder);

                    var result = new Entry(this.archive, fileIndex)
                    {
                        FileName = fileName,
                        IsFolder = isFolder
                    };
                    yield return result;
                }
            }
        }
    }
}
