using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NUnit.Framework;
using RespawnVpk;

namespace Tests
{
    [TestFixture]
    public class PackageTest 
    {
        private static readonly string TF2_VPK_DIRECTORY = "D:\\Origin Games\\Titanfall2\\vpk";
        private static readonly string APEX_VPK_DIRECTORY = "D:\\Steam\\steamapps\\common\\Apex Legends\\vpk";
        private static readonly string APEX_TEST_VPK_FILENAME = "englishclient_frontend.bsp.pak000_dir.vpk";
        private static readonly string TF2_TEST_VPK_FILENAME = "englishclient_mp_angel_city.bsp.pak000_dir.vpk";

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
        public async Task ExtractApexDirVPKAsync()
        {
            var path = Path.Combine(APEX_VPK_DIRECTORY, APEX_TEST_VPK_FILENAME);

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

                        var entry = await package.ReadEntryAsync(b);

                        data.Add(b.GetFullPath(), BitConverter.ToString(sha1.ComputeHash(entry.ToArray())).Replace("-", string.Empty));
                    }
                }

                Assert.AreNotEqual(0, data.Count);
                Assert.AreEqual("A9FF3616D6D58C78579D1A49CDB469A22D068D37", data["gameinfo.txt"]);
                Assert.AreEqual("2EF43AAF78B644702990D43F0F72ADAB8E644396", data["resource/notosansjp-regular.vfont"]);
            }

            Assert.AreEqual(flatEntries["gameinfo.txt"].TotalLength, 1498);
            Assert.AreEqual(flatEntries["resource/notosansjp-regular.vfont"].TotalLength, 4479600);
        }

        [Test]
        public async Task ExtractTF2DirVPKAsync()
        {
            var path = Path.Combine(TF2_VPK_DIRECTORY, TF2_TEST_VPK_FILENAME);

            using var package = new Package();
            package.Read(path);

            Assert.AreEqual(11, package.Entries.Count);
            Assert.Contains("vtf", package.Entries.Keys);
            Assert.Contains("vmt", package.Entries.Keys);

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

                        var entry = await package.ReadEntryAsync(b);

                        data.Add(b.GetFullPath(), BitConverter.ToString(sha1.ComputeHash(entry.ToArray())).Replace("-", string.Empty));
                    }
                }

                Assert.AreNotEqual(0, data.Count);
                Assert.AreEqual("07187C9BA4333AD9C095DB34837D7CDE2648090A", data["depot/r2dlc11/game/r2/maps/mp_angel_city.bsp.0069.bsp_lump"]);
                Assert.AreEqual("1893CD50D51DDAF2BDCBF7143704C2169C249A84", data["models/vistas/angel_city_se.mdl"]);
                Assert.AreEqual("AB63D65309027DA45979CDC37427BFC37BB1420F", data["scripts/vscripts/client/cl_carrier.gnut"]);
            }

            Assert.AreEqual(flatEntries["depot/r2dlc11/game/r2/maps/mp_angel_city.bsp.0069.bsp_lump"].TotalLength, 34603008);
            Assert.AreEqual(flatEntries["models/vistas/angel_city_se.mdl"].TotalLength, 10146095);
            Assert.AreEqual(flatEntries["scripts/vscripts/client/cl_carrier.gnut"].TotalLength, 32000);
        }

        [Test]
        public async Task ExtractWithProgressAsync()
        {
            var path = Path.Combine(APEX_VPK_DIRECTORY, APEX_TEST_VPK_FILENAME);

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
                    var progressCalled = false;

                    foreach (var b in a.Value)
                    {
                        Assert.AreEqual(a.Key, b.TypeName);

                        flatEntries.Add(b.GetFullPath(), b);

                        var progress = new Progress<int>(progressValue =>
                        {
                            progressCalled = true;

                            Assert.AreNotEqual(0, progressValue);
                            Assert.LessOrEqual(progressValue, b.TotalLength);
                        });

                        var entry = await package.ReadEntryAsync(b, progress);

                        data.Add(b.GetFullPath(), BitConverter.ToString(sha1.ComputeHash(entry.ToArray())).Replace("-", string.Empty));
                    }

                    Assert.IsTrue(progressCalled);
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
