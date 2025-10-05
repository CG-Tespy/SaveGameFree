using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Bayat.Unity.SaveGameFree.Serializers;
using UnityEngine.TestTools;

namespace Bayat.Unity.SaveGameFree.Tests
{

    [TestFixture]
    public class SaveGameExtendedTests
    {
        private List<string> _tempFiles = new List<string>();

        [SetUp]
        public void SetUp()
        {
            SaveGame.Encode = false;
            SaveGame.UsePlayerPrefs = false;
            SaveGame.LogError = true;

            // Reset to defaults
            var _ = SaveGame.Serializer;
            var __ = SaveGame.Encoder;
        }

        [TearDown]
        public void TearDown()
        {
            CleanUpTempFiles();
        }

        private void CleanUpTempFiles()
        {
            foreach (var elem in _tempFiles)
            {
                try
                {
                    if (File.Exists(elem))
                        File.Delete(elem);
                    if (Directory.Exists(elem))
                        Directory.Delete(elem, true);
                }
                catch { }
            }
            _tempFiles.Clear();
        }

        private string GenerateRandomIdentifier(string prefix, string fileExtension = "sav")
        {
            return $"{prefix}_{Guid.NewGuid():N}.{fileExtension}";
        }

        #region Async Tests

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public async Task SaveAsync_Creates_File(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("async_save");
            string expectedPath = $"{path.ToRealPath()}/{identifier}";
            _tempFiles.Add(expectedPath);

            await SaveGame.SaveAsync<string>(identifier, "async test data");

            Assert.IsTrue(File.Exists(expectedPath), "Async save did not create file.");
        }

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public async Task LoadAsync_Returns_Saved_Data(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("async_load");
            string expectedPath = $"{path.ToRealPath()}/{identifier}";
            _tempFiles.Add(expectedPath);

            string testData = "async load test";
            await SaveGame.SaveAsync<string>(identifier, testData);
            string loaded = await SaveGame.LoadAsync<string>(identifier);

            Assert.AreEqual(testData, loaded, "Async loaded data doesn't match saved data.");
        }

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public async Task SaveAsync_And_LoadAsync_With_Encryption(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("async_encrypted");
            string expectedPath = $"{path.ToRealPath()}/{identifier}";
            _tempFiles.Add(expectedPath);

            string testData = "encrypted async data";
            string password = "testpassword123";

            await SaveGame.SaveAsync<string>(identifier, testData, true, password,
                SaveGame.Serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, SaveGamePath.PersistentDataPath);

            string loaded = await SaveGame.LoadAsync<string>(identifier, default(string), true, password,
                SaveGame.Serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, SaveGamePath.PersistentDataPath);

            Assert.AreEqual(testData, loaded, "Async encrypted data doesn't match.");
        }

        #endregion

        #region Encryption Tests

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public void Save_And_Load_With_Encryption(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("encrypted");
            string expectedPath = $"{path.ToRealPath()}/{identifier}";
            _tempFiles.Add(expectedPath);

            string testData = "sensitive data";
            string password = "securepassword456";

            SaveGame.Save<string>(identifier, testData, true, password,
                SaveGame.Serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, path);

            string loaded = SaveGame.Load<string>(identifier, default(string), true, password,
                SaveGame.Serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, path);

            Assert.AreEqual(testData, loaded, "Encrypted data doesn't match.");
        }

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public void Load_With_Wrong_Password_Throws_Exception(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("wrong_password");
            string expectedPath = $"{path.ToRealPath()}/{identifier}";
            _tempFiles.Add(expectedPath);

            string testData = "secret data";
            string correctPassword = "correct123";
            string wrongPassword = "wrong456";

            SaveGame.Save<string>(identifier, testData, true, correctPassword,
                SaveGame.Serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, path);

            Assert.Throws<InvalidOperationException>(() =>
            {
                // We're expecting two error messages here
                string expectedLogMessage = $"Failed to load data with identifier '{identifier}': " +
                $"Padding is invalid and cannot be removed.";
                LogAssert.Expect(LogType.Error, expectedLogMessage);

                expectedLogMessage = "CryptographicException: Padding is invalid and cannot be removed.";
                LogAssert.Expect(LogType.Exception, expectedLogMessage);

                SaveGame.Load<string>(identifier, default(string), true, wrongPassword,
                    SaveGame.Serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, path);
            });
        }

        #endregion

        #region Serializer Tests

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public void Save_And_Load_With_Binary_Serializer(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("binary");
            string expectedPath = $"{path.ToRealPath()}/{identifier}";
            _tempFiles.Add(expectedPath);

            var serializer = new SaveGameBinarySerializer();
            string testData = "binary serialized data";

            SaveGame.Save<string>(identifier, testData, false, string.Empty,
                serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, path);

            string loaded = SaveGame.Load<string>(identifier, default(string), false, string.Empty,
                serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, path);

            Assert.AreEqual(testData, loaded, "Binary serialized data doesn't match.");
        }

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public void Save_And_Load_With_Xml_Serializer(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("xml");
            string expectedPath = $"{path.ToRealPath()}/{identifier}";
            _tempFiles.Add(expectedPath);

            var serializer = new SaveGameXmlSerializer();
            string testData = "xml serialized data";

            SaveGame.Save<string>(identifier, testData, false, string.Empty,
                serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, path);

            string loaded = SaveGame.Load<string>(identifier, default(string), false, string.Empty,
                serializer, SaveGame.Encoder, SaveGame.DefaultEncoding, path);

            Assert.AreEqual(testData, loaded, "XML serialized data doesn't match.");
        }

        #endregion

        #region Complex Data Tests

        [Serializable]
        public class TestDataClass
        {
            public int IntValue;
            public string StringValue;
            public float FloatValue;
            public List<string> StringList;

            public override bool Equals(object obj)
            {
                if (obj is TestDataClass other)
                {
                    return IntValue == other.IntValue &&
                           StringValue == other.StringValue &&
                           Mathf.Approximately(FloatValue, other.FloatValue) &&
                           StringList.Count == other.StringList.Count;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(IntValue, StringValue, FloatValue, StringList);
            }
        }

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public void Save_And_Load_Complex_Object(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("complex");
            string expectedPath = $"{path.ToRealPath()}/{identifier}";
            _tempFiles.Add(expectedPath);

            var testData = new TestDataClass
            {
                IntValue = 42,
                StringValue = "test string",
                FloatValue = 3.14f,
                StringList = new List<string> { "item1", "item2", "item3" }
            };

            SaveGame.Save(identifier, testData);
            var loaded = SaveGame.Load<TestDataClass>(identifier);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(testData.IntValue, loaded.IntValue);
            Assert.AreEqual(testData.StringValue, loaded.StringValue);
            Assert.AreEqual(testData.FloatValue, loaded.FloatValue, 0.001f);
            Assert.AreEqual(testData.StringList.Count, loaded.StringList.Count);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public void Load_Nonexistent_File_Returns_Default(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("nonexistent");

            string loaded = SaveGame.Load<string>(identifier, "default value");

            Assert.AreEqual("default value", loaded);
        }

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public void Delete_Removes_File(SaveGamePath path)
        {
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("delete_test");
            string expectedPath = $"{path.ToRealPath()}/{identifier}";
            _tempFiles.Add(expectedPath);

            SaveGame.Save<string>(identifier, "data to delete");
            Assert.IsTrue(SaveGame.Exists(identifier));

            SaveGame.Delete(identifier);
            Assert.IsFalse(SaveGame.Exists(identifier));
        }

        [Test]
        public void Exists_Returns_False_For_Nonexistent_File()
        {
            string identifier = GenerateRandomIdentifier("nonexistent_check");
            Assert.IsFalse(SaveGame.Exists(identifier));
        }

        #endregion

        #region PlayerPrefs Tests

        [Test]
        [TestCase(SaveGamePath.PersistentDataPath)]
        [TestCase(SaveGamePath.DataPath)]
        public void Save_And_Load_Using_PlayerPrefs(SaveGamePath path)
        {
            SaveGame.UsePlayerPrefs = true;
            SaveGame.SavePath = path;
            string identifier = GenerateRandomIdentifier("playerprefs");

            string testData = "playerprefs data";
            SaveGame.Save<string>(identifier, testData);

            string loaded = SaveGame.Load<string>(identifier);

            Assert.AreEqual(testData, loaded);

            // Cleanup
            string filePath = $"{Application.persistentDataPath}/{identifier}";
            PlayerPrefs.DeleteKey(filePath);
            SaveGame.UsePlayerPrefs = false;
        }

        #endregion
    }

}
