/*
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

using RespawnVpk.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RespawnVpk
{
    public class Package : IDisposable
    {
        private static readonly int MAGIC = 0x55AA1234;
        private static readonly int MAX_ENTRY_CHUNK_SIZE = 1024 * 1024;
        private static readonly string[] VPK_PREFIXES = new[]
        {
            "tchinese",
            "schinese",
            "japanese",
            "korean",
            "russian",
            "polish",
            "portuguese",
            "italian",
            "german",
            "spanish",
            "french",
            "english",
        };

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
        /// <param name="validateCrc">If true, CRC32 will be calculated and verified for read data.</param>
        /// <returns>Output buffer.</returns>
        public Task<Memory<byte>> ReadEntryAsync(PackageEntry entry, bool validateCrc = true)
        {
            return ReadEntryAsync(entry, null, validateCrc);
        }

        /// <summary>
        /// Reads the entry from the VPK package.
        /// </summary>
        /// <param name="entry">Package entry.</param>
        /// <param name="progressReporter">Progress report callback.</param>
        /// <param name="validateCrc">If true, CRC32 will be calculated and verified for read data.</param>
        /// <returns>Output buffer.</returns>
        public async Task<Memory<byte>> ReadEntryAsync(PackageEntry entry, IProgress<int> progressReporter, bool validateCrc = true)
        {
            DecompressState decompressState = null;
            var parameters = new DecompressParameters();
            parameters.DictSizeLog2 = 20;

            var blockReadBuffer = new Memory<byte>(new byte[MAX_ENTRY_CHUNK_SIZE]);
            var outputBuffer = new Memory<byte>(new byte[entry.SmallData.Length + entry.TotalLength]);
            var outputOffset = 0;

            if (entry.SmallData.Length > 0)
            {
                entry.SmallData.CopyTo(outputBuffer);
                outputOffset += entry.SmallData.Length;
            }

            if (entry.TotalCompressedLength < entry.TotalLength)
            {
                decompressState = Lzham.DecompressInit(parameters);
            }

            if (entry.TotalLength > 0)
            {
                Stream fs = null;
                ushort currentArchiveIndex = 0x7FFF;

                try
                {
                    foreach (var chunk in entry.Chunks)
                    {
                        var streamOffset = chunk.Offset;

                        if (currentArchiveIndex != chunk.ArchiveIndex)
                        {
                            currentArchiveIndex = chunk.ArchiveIndex;
                            fs?.Close();
                        }

                        if (chunk.ArchiveIndex != 0x7FFF)
                        {
                            if (currentArchiveIndex != chunk.ArchiveIndex)
                            {
                                currentArchiveIndex = chunk.ArchiveIndex;
                                fs?.Close();
                            }

                            if (!IsDirVPK)
                            {
                                throw new InvalidOperationException("Given VPK is not a _dir, but entry is referencing an external archive.");
                            }

                            var vpkDirectory = Path.GetDirectoryName(FileName);
                            var vpkName = Path.GetFileName(FileName);

                            foreach (var prefix in VPK_PREFIXES)
                            {
                                if (vpkName.StartsWith(prefix))
                                {
                                    vpkName = vpkName.Substring(prefix.Length);
                                    break;
                                }
                            }

                            var fileName = Path.Combine(vpkDirectory, $"{vpkName}_{chunk.ArchiveIndex:D3}.vpk");

                            fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                        }
                        else
                        {
                            fs = Reader.BaseStream;

                            streamOffset += HeaderSize + TreeSize;
                        }

                        fs.Seek(streamOffset, SeekOrigin.Begin);
                        var readBuffer = blockReadBuffer.Slice(0, (int)chunk.CompressedLength);
                        var bytesRead = await fs.ReadAsync(readBuffer);

                        if (bytesRead != chunk.CompressedLength)
                        {
                            throw new InvalidOperationException($"Attempted to read {chunk.CompressedLength} bytes, got {bytesRead.ToString()}.");
                        }

                        if (chunk.CompressedLength < chunk.Length)
                        {
                            var decompressedSpan = outputBuffer.Slice(outputOffset);
                            var decompressResult = Lzham.DecompressMemory(parameters, readBuffer, ref decompressedSpan);

                            if (decompressResult != DecompressStatus.Success)
                            {
                                throw new InvalidOperationException($"Error attempting to decompress: {decompressResult.ToString()}");
                            }

                            outputOffset += (int)chunk.Length;
                        }
                        else
                        {
                            readBuffer.CopyTo(outputBuffer.Slice(outputOffset));

                            outputOffset += bytesRead;
                        }

                        if (progressReporter != null)
                        {
                            progressReporter.Report(outputOffset);
                        }
                    }
                }
                finally
                {
                    if (currentArchiveIndex != 0x7FFF)
                    {
                        fs?.Close();
                    }
                }
            }

            if (validateCrc && entry.CRC32 != Crc32.Compute(outputBuffer))
            {
                throw new InvalidDataException("CRC32 mismatch for read data.");
            }

            if (decompressState != null)
            {
                var deinitResult = Lzham.DecompressDeinit(decompressState);
                
                if (deinitResult >= DecompressStatus.FirstFailureCode)
                {
                    throw new InvalidOperationException($"Error attempting to deinitialize compressor: {deinitResult.ToString()}");
                }
            }

            return outputBuffer;
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
                        };

                        var entryChunks = new List<PackageEntryChunk>();
                        PackageEntryChunk chunk = null;

                        var archiveIndex = Reader.ReadUInt16();

                        while (archiveIndex != 0xFFFF)
                        {
                            chunk = new PackageEntryChunk()
                            {
                                ArchiveIndex = archiveIndex,
                                Unknown1 = Reader.ReadBytes(6),
                                Offset = Reader.ReadUInt32(),
                                Unknown2 = Reader.ReadBytes(4),
                                CompressedLength = Reader.ReadUInt32(),
                                Unknown3 = Reader.ReadBytes(4),
                                Length = Reader.ReadUInt32(),
                                Unknown4 = Reader.ReadBytes(4),
                            };

                            entryChunks.Add(chunk);

                            archiveIndex = Reader.ReadUInt16();
                        }

                        entry.Chunks = entryChunks.ToArray();

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
