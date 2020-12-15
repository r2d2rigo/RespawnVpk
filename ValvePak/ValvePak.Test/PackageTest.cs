using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using SteamDatabase.ValvePak;

namespace Tests
{
    [TestFixture]
    public class PackageTest 
    {
        private static readonly string APEX_VPK_DIRECTORY = "D:\\Steam\\steamapps\\common\\Apex Legends\\vpk";
        private static readonly string APEX_TEST_VPK_FILENAME = "englishclient_frontend.bsp.pak000_dir.vpk";

        [Test]
        public void ParseVPK()
        {
            var path = Path.Combine(APEX_VPK_DIRECTORY, APEX_TEST_VPK_FILENAME);

            using var package = new Package();
            package.Read(path);
        }

        [Test]
        public void InvalidPackageThrows()
        {
            using var resource = new Package();
            using var ms = new MemoryStream(Enumerable.Repeat<byte>(1, 12).ToArray());

            // Should yell about not setting file name
            Assert.Throws<InvalidOperationException>(() => resource.Read(ms));

            resource.SetFileName("a.vpk");

            Assert.Throws<InvalidDataException>(() => resource.Read(ms));
        }

        [Test]
        public void CorrectHeaderWrongVersionThrows()
        {
            using var resource = new Package();
            resource.SetFileName("a.vpk");

            using var ms = new MemoryStream(new byte[] { 0x34, 0x12, 0xAA, 0x55, 0x11, 0x11, 0x11, 0x11, 0x22, 0x22, 0x22, 0x22, 0x33, 0x33, 0x33, 0x33 });
            Assert.Throws<InvalidDataException>(() => resource.Read(ms));
        }

        [Test]
        public void FindEntryDeep()
        {
            var path = Path.Combine(APEX_VPK_DIRECTORY, APEX_TEST_VPK_FILENAME);

            using var package = new Package();
            package.Read(path);

            Assert.AreEqual(0x8CF300C4, package.FindEntry("scripts\\aibehavior\\common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("scripts\\aibehavior\\", "common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("scripts\\aibehavior\\", "common_schedules", "txt")?.CRC32);

            Assert.AreEqual(0x8CF300C4, package.FindEntry("scripts/aibehavior\\common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("scripts/aibehavior\\", "common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("scripts/aibehavior\\", "common_schedules", "txt")?.CRC32);

            Assert.AreEqual(0x8CF300C4, package.FindEntry("scripts/aibehavior/common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("scripts/aibehavior/", "common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("scripts/aibehavior/", "common_schedules", "txt")?.CRC32);

            Assert.AreEqual(0x8CF300C4, package.FindEntry("\\scripts/aibehavior/common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("\\scripts/aibehavior/", "common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("\\scripts/aibehavior/", "common_schedules", "txt")?.CRC32);

            Assert.AreEqual(0x8CF300C4, package.FindEntry("/scripts/aibehavior/common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("/scripts/aibehavior/", "common_schedules.txt")?.CRC32);
            Assert.AreEqual(0x8CF300C4, package.FindEntry("/scripts/aibehavior/", "common_schedules", "txt")?.CRC32);

            Assert.IsNull(package.FindEntry("\\scripts/aibehavior/hello_github_reader.vdf"));
            Assert.IsNull(package.FindEntry("\\scripts/aibehavior/", "hello_github_reader.vdf"));
            Assert.IsNull(package.FindEntry("\\scripts/aibehavior/", "hello_github_reader", "vdf"));
        }

        [Test]
        public void FindEntryRoot()
        {
            var path = Path.Combine(APEX_VPK_DIRECTORY, APEX_TEST_VPK_FILENAME);

            using var package = new Package();
            package.Read(path);

            Assert.AreEqual(0x744A2D89, package.FindEntry("gameinfo.txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry(string.Empty, "gameinfo.txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry(string.Empty, "gameinfo", "txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry(" ", "gameinfo.txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry(" ", "gameinfo", "txt")?.CRC32);

            Assert.AreEqual(0x744A2D89, package.FindEntry("\\gameinfo.txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry("\\", "gameinfo.txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry("\\", "gameinfo", "txt")?.CRC32);

            Assert.AreEqual(0x744A2D89, package.FindEntry("/gameinfo.txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry("/", "gameinfo.txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry("/", "gameinfo", "txt")?.CRC32);

            Assert.AreEqual(0x744A2D89, package.FindEntry("\\/gameinfo.txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry("\\/\\", "gameinfo.txt")?.CRC32);
            Assert.AreEqual(0x744A2D89, package.FindEntry("\\\\/", "gameinfo", "txt")?.CRC32);
        }

        [Test]
        public void ThrowsNullArgumentInFindEntry()
        {
            var path = Path.Combine(APEX_VPK_DIRECTORY, APEX_TEST_VPK_FILENAME);

            using var package = new Package();
            package.Read(path);

            Assert.Throws<ArgumentNullException>(() => package.FindEntry(null));
            Assert.Throws<ArgumentNullException>(() => package.FindEntry("", null));
            Assert.Throws<ArgumentNullException>(() => package.FindEntry(null, ""));
            Assert.Throws<ArgumentNullException>(() => package.FindEntry(null, "", ""));
            Assert.Throws<ArgumentNullException>(() => package.FindEntry("", null, ""));
            Assert.Throws<ArgumentNullException>(() => package.FindEntry("", "", null));
        }

        [Test]
        public void ExtractDirVPK()
        {
            var path = Path.Combine(APEX_VPK_DIRECTORY, APEX_TEST_VPK_FILENAME);

            TestVPKExtraction(path);
        }

        private void TestVPKExtraction(string path)
        {
            using var package = new Package();
            package.Read(path);

            Assert.AreEqual(11, package.Entries.Count);
            Assert.Contains("txt", package.Entries.Keys);
            Assert.Contains("cfg", package.Entries.Keys);

            var flatEntries = new Dictionary<string, PackageEntry>();

            using (var sha1 = SHA1.Create())
            {
                var data = new Dictionary<string, string>();

                foreach (var a in package.Entries)
                {
                    foreach (var b in a.Value)
                    {
                        Assert.AreEqual(a.Key, b.TypeName);

                        flatEntries.Add(b.GetFullPath(), b);

                        package.ReadEntry(b, out var entry);

                        data.Add(b.GetFullPath(), BitConverter.ToString(sha1.ComputeHash(entry)).Replace("-", string.Empty));
                    }
                }

                Assert.AreNotEqual(0, data.Count);
                Assert.AreEqual("A9FF3616D6D58C78579D1A49CDB469A22D068D37", data["gameinfo.txt"]);
                Assert.AreEqual("2EF43AAF78B644702990D43F0F72ADAB8E644396", data["resource/notosansjp-regular.vfont"]);
            }

            Assert.AreEqual(flatEntries["gameinfo.txt"].TotalLength, 1498);
            Assert.AreEqual(flatEntries["resource/notosansjp-regular.vfont"].TotalLength, 4479600);
        }
    }
}
