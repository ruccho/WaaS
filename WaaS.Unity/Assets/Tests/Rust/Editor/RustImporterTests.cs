using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WaaS.Unity.Tests.Rust
{
    /// <summary>
    ///     Tests that the crates generated for Rust scripts follow the scripts, so that the Cargo workspace at the root of
    ///     the project (which is also loaded by IDEs) stays valid.
    /// </summary>
    public class RustImporterTests
    {
        private const string Folder = "Assets/Tests/Rust/Temp";
        private const string Source = "#[no_mangle]\npub extern \"C\" fn answer() -> i32 {\n    42\n}\n";

        private string lockFile;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            lockFile = File.Exists("Cargo.lock") ? File.ReadAllText("Cargo.lock") : null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // the builds of the scripts add them to the lock file of the workspace
            if (lockFile != null) File.WriteAllText("Cargo.lock", lockFile);
        }

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Tests/Rust", "Temp");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
        }

        private static string CreateScript(string name)
        {
            var path = $"{Folder}/{name}";
            File.WriteAllText(path, Source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return path;
        }

        private static bool CrateExists(string assetPath)
        {
            return Directory.Exists($"Library/com.ruccho.waas/rust/crates/waas_{Hash128.Compute(assetPath)}");
        }

        [Test]
        public void CrateIsRemovedWhenScriptIsDeleted()
        {
            var path = CreateScript("deleted.rs");
            Assert.That(CrateExists(path), Is.True);

            AssetDatabase.DeleteAsset(path);
            Assert.That(CrateExists(path), Is.False);
        }

        [Test]
        public void CrateFollowsMovedScript()
        {
            var path = CreateScript("moved-from.rs");
            var newPath = $"{Folder}/moved-to.rs";
            Assert.That(AssetDatabase.MoveAsset(path, newPath), Is.Empty);

            Assert.That(CrateExists(path), Is.False);
            Assert.That(CrateExists(newPath), Is.True);
        }

        [Test]
        public void CrateIsRemovedWhenScriptIsNotCrateRoot()
        {
            var path = CreateScript("module.rs");
            Assert.That(CrateExists(path), Is.True);

            var importer = AssetImporter.GetAtPath(path);
            using (var serializedImporter = new SerializedObject(importer))
            {
                serializedImporter.FindProperty("crateRoot").boolValue = false;
                serializedImporter.ApplyModifiedPropertiesWithoutUndo();
            }

            importer.SaveAndReimport();
            Assert.That(CrateExists(path), Is.False);
        }
    }
}
