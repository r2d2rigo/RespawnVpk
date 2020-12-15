﻿/*
 * Read() function was mostly taken from Rick's Gibbed.Valve.FileFormats,
 * which is subject to this license:
 *
 * Copyright (c) 2008 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software
 * in a product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 * distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SteamDatabase.ValvePak
{
    public class Package : IDisposable
    {
        public const int MAGIC = 0x55AA1234;

        /// <summary>
        /// Always '/' as per Valve's vpk implementation.
        /// </summary>
        public const char DirectorySeparatorChar = '/';

        private BinaryReader Reader;
        private bool IsDirVPK;
        private uint HeaderSize;

        /// <summary>
        /// Gets the File Name
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the VPK version.
        /// </summary>
        public uint Version { get; private set; }

        /// <summary>
        /// Gets the size in bytes of the directory tree.
        /// </summary>
        public uint TreeSize { get; private set; }

        /// <summary>
        /// Gets the package entries.
        /// </summary>
        public Dictionary<string, List<PackageEntry>> Entries { get; private set; }

        /// <summary>
        /// Gets the archive MD5 checksum section entries. Also known as cache line hashes.
        /// </summary>
        public List<ArchiveMD5SectionEntry> ArchiveMD5Entries { get; private set; }

        /// <summary>
        /// Releases binary reader.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && Reader != null)
            {
                Reader.Dispose();
                Reader = null;
            }
        }

        /// <summary>
        /// Sets the file name.
        /// </summary>
        /// <param name="fileName">Filename.</param>
        public void SetFileName(string fileName)
        {
            if (fileName.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - 4);
            }

            if (fileName.EndsWith("_dir", StringComparison.OrdinalIgnoreCase))
            {
                IsDirVPK = true;

                fileName = fileName.Substring(0, fileName.Length - 4);
            }

            FileName = fileName;
        }

        /// <summary>
        /// Opens and reads the given filename.
        /// The file is held open until the object is disposed.
        /// </summary>
        /// <param name="filename">The file to open and read.</param>
        public void Read(string filename)
        {
            SetFileName(filename);

            var fs = new FileStream($"{FileName}{(IsDirVPK ? "_dir" : string.Empty)}.vpk", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            Read(fs);
        }

        /// <summary>
        /// Reads the given <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">The input <see cref="Stream"/> to read from.</param>
        public void Read(Stream input)
        {
            if (FileName == null)
            {
                throw new InvalidOperationException("If you call Read() directly with a stream, you must call SetFileName() first.");
            }

            Reader = new BinaryReader(input);

            if (Reader.ReadUInt32() != MAGIC)
            {
                throw new InvalidDataException("Given file is not a VPK.");
            }

            Version = Reader.ReadUInt32();
            TreeSize = Reader.ReadUInt32();
            var headerUnknown = Reader.ReadUInt32();

            if (Version != 0x00030002)
            {
                throw new InvalidDataException($"Bad VPK version. ({Version})");
            }

            HeaderSize = (uint)input.Position;

            ReadEntries();
        }

        /// <summary>
        /// Searches for a given file entry in the file list.
        /// </summary>
        /// <param name="filePath">Full path to the file to find.</param>
        public PackageEntry FindEntry(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            filePath = filePath.Replace('\\', DirectorySeparatorChar);

            var lastSeparator = filePath.LastIndexOf(DirectorySeparatorChar);
            var directory = lastSeparator > -1 ? filePath.Substring(0, lastSeparator) : string.Empty;
            var fileName = filePath.Substring(lastSeparator + 1);

            return FindEntry(directory, fileName);
        }

        /// <summary>
        /// Searches for a given file entry in the file list.
        /// </summary>
        /// <param name="directory">Directory to search in.</param>
        /// <param name="fileName">File name to find.</param>
        public PackageEntry FindEntry(string directory, string fileName)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var dot = fileName.LastIndexOf('.');
            string extension;

            if (dot > -1)
            {
                extension = fileName.Substring(dot + 1);
                fileName = fileName.Substring(0, dot);
            }
            else
            {
                // Valve uses a space for missing extensions
                extension = " ";
            }

            return FindEntry(directory, fileName, extension);
        }

        /// <summary>
        /// Searches for a given file entry in the file list.
        /// </summary>
        /// <param name="directory">Directory to search in.</param>
        /// <param name="fileName">File name to find, without the extension.</param>
        /// <param name="extension">File extension, without the leading dot.</param>
        public PackageEntry FindEntry(string directory, string fileName, string extension)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            if (!Entries.ContainsKey(extension))
            {
                return null;
            }

            // We normalize path separators when reading the file list
            // And remove the trailing slash
            directory = directory.Replace('\\', DirectorySeparatorChar).Trim(DirectorySeparatorChar);

            // If the directory is empty after trimming, set it to a space to match Valve's behaviour
            if (directory.Length == 0)
            {
                directory = " ";
            }

            return Entries[extension].Find(x => x.DirectoryName == directory && x.FileName == fileName);
        }

        /// <summary>
        /// Reads the entry from the VPK package.
        /// </summary>
        /// <param name="entry">Package entry.</param>
        /// <param name="output">Output buffer.</param>
        /// <param name="validateCrc">If true, CRC32 will be calculated and verified for read data.</param>
        public void ReadEntry(PackageEntry entry, out byte[] output, bool validateCrc = true)
        {
            output = new byte[entry.SmallData.Length + entry.Length];

            if (entry.SmallData.Length > 0)
            {
                entry.SmallData.CopyTo(output, 0);
            }

            if (entry.Length > 0)
            {
                Stream fs = null;

                try
                {
                    var offset = entry.Offset;

                    if (entry.ArchiveIndex != 0x7FFF)
                    {
                        if (!IsDirVPK)
                        {
                            throw new InvalidOperationException("Given VPK is not a _dir, but entry is referencing an external archive.");
                        }

                        var fileName = $"{FileName}_{entry.ArchiveIndex:D3}.vpk";

                        fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                    }
                    else
                    {
                        fs = Reader.BaseStream;

                        offset += HeaderSize + TreeSize;
                    }

                    fs.Seek(offset, SeekOrigin.Begin);
                    fs.Read(output, entry.SmallData.Length, (int)entry.Length);
                }
                finally
                {
                    if (entry.ArchiveIndex != 0x7FFF)
                    {
                        fs?.Close();
                    }
                }
            }

            if (validateCrc && entry.CRC32 != Crc32.Compute(output))
            {
                throw new InvalidDataException("CRC32 mismatch for read data.");
            }
        }

        private void ReadEntries()
        {
            var typeEntries = new Dictionary<string, List<PackageEntry>>();

            // Types
            while (true)
            {
                var typeName = Reader.ReadNullTermString(Encoding.UTF8);

                if (string.IsNullOrEmpty(typeName))
                {
                    break;
                }

                var entries = new List<PackageEntry>();

                // Directories
                while (true)
                {
                    var directoryName = Reader.ReadNullTermString(Encoding.UTF8);

                    if (directoryName?.Length == 0)
                    {
                        break;
                    }

                    // Files
                    while (true)
                    {
                        var fileName = Reader.ReadNullTermString(Encoding.UTF8);

                        if (fileName?.Length == 0)
                        {
                            break;
                        }

                        var entry = new PackageEntry
                        {
                            FileName = fileName,
                            DirectoryName = directoryName,
                            TypeName = typeName,
                            CRC32 = Reader.ReadUInt32(),
                            SmallData = new byte[Reader.ReadUInt16()],
                            ArchiveIndex = Reader.ReadUInt16(),
                            Offset = Reader.ReadUInt32(),
                            Length = Reader.ReadUInt32()
                        };

                        if (Reader.ReadUInt16() != 0xFFFF)
                        {
                            throw new FormatException("Invalid terminator.");
                        }

                        if (entry.SmallData.Length > 0)
                        {
                            Reader.Read(entry.SmallData, 0, entry.SmallData.Length);
                        }

                        entries.Add(entry);
                    }
                }

                typeEntries.Add(typeName, entries);
            }

            Entries = typeEntries;
        }
    }
}
