using System;

namespace SteamDatabase.ValvePak
{
    public class PackageEntryChunk
    {
        /// <summary>
        /// Gets or sets the length in bytes.
        /// </summary>
        public uint Length { get; set; }

        /// <summary>
        /// Gets or sets the offset in the package.
        /// </summary>
        public uint Offset { get; set; }

        /// <summary>
        /// Gets or sets which archive this entry is in.
        /// </summary>
        public ushort ArchiveIndex { get; set; }

        public byte[] Unknown1 { get; set; }

        public byte[] Unknown2 { get; set; }

        public byte[] Unknown3 { get; set; }

        public byte[] Unknown4 { get; set; }

        public uint CompressedLength { get; set; }
    }
}
