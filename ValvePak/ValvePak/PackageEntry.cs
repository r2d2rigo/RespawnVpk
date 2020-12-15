using System;

namespace SteamDatabase.ValvePak
{
    public class PackageEntry
    {
        /// <summary>
        /// Gets or sets file name of this entry.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the name of the directory this file is in.
        /// '/' is always used as a dictionary separator in Valve's implementation.
        /// Directory names are also always lower cased in Valve's implementation.
        /// </summary>
        public string DirectoryName { get; set; }

        /// <summary>
        /// Gets or sets the file extension.
        /// If the file has no extension, this is an empty string.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the CRC32 checksum of this entry.
        /// </summary>
        public uint CRC32 { get; set; }

        public PackageEntryChunk[] Chunks { get; set; }

        /// <summary>
        /// Gets the length in bytes by adding Length and length of SmallData.
        /// </summary>
        public uint TotalLength
        {
            get
            {
                uint totalLength = 0;

                for (int i = 0; i < Chunks.Length; ++i)
                {
                    totalLength += Chunks[i].Length;
                }

                if (SmallData != null)
                {
                    totalLength += (uint)SmallData.Length;
                }

                return totalLength;
            }
        }

        public uint TotalCompressedLength
        {
            get
            {
                uint totalCompressedLength = 0;

                for (int i = 0; i < Chunks.Length; ++i)
                {
                    totalCompressedLength += Chunks[i].CompressedLength;
                }

                return totalCompressedLength;
            }
        }

        /// <summary>
        /// Gets or sets the preloaded bytes.
        /// </summary>
        public byte[] SmallData { get; set; }

        /// <summary>
        /// Returns the file name and extension.
        /// </summary>
        /// <returns>File name and extension.</returns>
        public string GetFileName()
        {
            var fileName = FileName;

            if (TypeName == " ")
            {
                return fileName;
            }

            return fileName + "." + TypeName;
        }

        /// <summary>
        /// Returns the absolute path of the file in the package.
        /// </summary>
        /// <returns>Absolute path.</returns>
        public string GetFullPath()
        {
            if (DirectoryName == " ")
            {
                return GetFileName();
            }

            return DirectoryName + Package.DirectorySeparatorChar + GetFileName();
        }

        public override string ToString()
        {
            return $"{GetFullPath()} crc=0x{CRC32:x2} metadatasz={SmallData.Length} csz={TotalCompressedLength} sz={TotalLength}";
        }
    }
}
