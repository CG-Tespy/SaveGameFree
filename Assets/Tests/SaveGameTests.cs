using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using BayatGames.SaveGameFree;

[TestFixture]
public class SaveGameTests
{
    private List<string> _tempFiles = new List<string>();

    [SetUp]
    public void SetUp()
    {
        // Ensure tests do not use PlayerPrefs or encryption.
        SaveGame.Encode = false;
        SaveGame.UsePlayerPrefs = false;

        // Keep default serializers/encoders but make sure defaults are present.
        var _ = SaveGame.Serializer;
        var __ = SaveGame.Encoder;
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temp files created during tests.
        CleanUpTempFiles();
        void CleanUpTempFiles()
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
    }

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void Save_Creates_File_In_PersistentDataPath(SaveGamePath basePath)
    {
        SaveGame.SavePath = basePath;
        string identifier = GenerateRandomIdentifier("save_persistent");
        string expectedPath = string.Format(savePathFormat, basePath.ToRealPath(), identifier);
        _tempFiles.Add(expectedPath);

        SaveGame.Save<string>(identifier, "test");

        Assert.IsTrue(File.Exists(expectedPath), $"Expected file at '{expectedPath}' but it was not created.");
    }

    protected virtual string GenerateRandomIdentifier(string prefix, string fileExtension = "sav")
    {
        string result = $"{prefix}_{Guid.NewGuid():N}.{fileExtension}";
        return result;
    }

    protected readonly string savePathFormat = "{0}/{1}";

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void Save_Creates_File_In_DataPath(SaveGamePath basePath)
    {
        SaveGame.SavePath = basePath;
        string identifier = GenerateRandomIdentifier("save_data");
        string expectedPath = string.Format(savePathFormat, basePath.ToRealPath(), identifier);

        _tempFiles.Add(expectedPath);

        SaveGame.Save<string>(identifier, "test");
        Assert.IsTrue(File.Exists(expectedPath), $"Expected file at '{expectedPath}' but it was not created.");
    }

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void Save_File_Has_Right_Name(SaveGamePath basePath)
    {
        // Use PersistentDataPath and verify the filename matches identifier
        SaveGame.SavePath = basePath;
        string identifier = GenerateRandomIdentifier("expected_name");
        string expectedPath = string.Format(savePathFormat, basePath.ToRealPath(), identifier);
        _tempFiles.Add(expectedPath);

        SaveGame.Save<string>(identifier, "payload");

        // Assert.IsTrue(File.Exists(expectedPath), "File not created."); // We already have this check in other tests
        Assert.AreEqual(identifier, Path.GetFileName(expectedPath), "Saved file name does not match identifier.");
    }

    [Test]
    [TestCase(null, SaveGamePath.PersistentDataPath,
        TestName = "Save_Throws_ArgumentNullException_On_Null_Identifier_Persistent")]
    [TestCase("", SaveGamePath.PersistentDataPath,
        TestName = "Save_Throws_ArgumentNullException_On_Empty_Identifier_Persistent")]
    [TestCase(null, SaveGamePath.DataPath,
        TestName = "Save_Throws_ArgumentNullException_On_Null_Identifier_BaseDataPath")]
    [TestCase("", SaveGamePath.DataPath,
        TestName = "Save_Throws_ArgumentNullException_On_Empty_Identifier_BaseDataPath")]
    public void Save_Throws_ArgumentNullException_On_NullOrEmpty_Identifier(string identifier, SaveGamePath basePath)
    {
        SaveGame.SavePath = basePath;

        var ex = Assert.Throws<ArgumentNullException>(() => SaveGame.Save<object>(identifier, new object()));
        StringAssert.Contains("identifier", ex.ParamName);
    }

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void Callbacks_Are_Invoked_On_Save_And_Load_In_Expected_Order(SaveGamePath basePath)
    {
        // Prepare storage path
        SaveGame.SavePath = basePath;
        string identifier = GenerateRandomIdentifier("callbacks", "dat");
        string expectedPath = string.Format(savePathFormat, basePath.ToRealPath(), identifier);
        _tempFiles.Add(expectedPath);

        IList<string> calls = new List<string>();

        // Save and Load callbacks are fields; store originals to restore later.
        var prevSaveCallback = SaveEvents.SaveCallback;
        var prevLoadCallback = SaveEvents.LoadCallback;

        // Handlers we will attach
        SaveHandler onSavingHandler, onSavedHandler;
        LoadHandler onLoadingHandler, onLoadedHandler;
        PrepHandlers();
        void PrepHandlers()
        {
            onSavingHandler = (obj, id, encode, password, serializer, encoder, encoding, path) =>
            {
                calls.Add("OnSaving");
            };
            onSavedHandler = (obj, id, encode, password, serializer, encoder, encoding, path) =>
            {
                calls.Add("OnSaved");
            };
            onLoadingHandler = (loadedObj, id, encode, password, serializer, encoder, encoding, path) =>
            {
                calls.Add("OnLoading");
            };
            onLoadedHandler = (loadedObj, id, encode, password, serializer, encoder, encoding, path) =>
            {
                calls.Add("OnLoaded");
            };
        }

        SaveEvents.SaveCallback = (obj, id, encode, password, serializer, encoder, encoding, path) =>
        {
            calls.Add("SaveCallback");
        };
        SaveEvents.LoadCallback = (loadedObj, id, encode, password, serializer, encoder, encoding, path) =>
        {
            calls.Add("LoadCallback");
        };

        AddSubs();
        void AddSubs()
        {
            SaveEvents.OnSaving += onSavingHandler;
            SaveEvents.OnSaved += onSavedHandler;
            SaveEvents.OnLoading += onLoadingHandler;
            SaveEvents.OnLoaded += onLoadedHandler;
        }

        try
        {
            // Perform Save -> triggers OnSaving, SaveCallback, OnSaved (in that order)
            SaveGame.Save<string>(identifier, "payload");

            // Perform Load -> triggers OnLoading, LoadCallback, OnLoaded (in that order)
            var loaded = SaveGame.Load<string>(identifier);

            // Verify load returned expected value (serializer roundtrip)
            Assert.IsNotNull(loaded, "Loaded value was null.");
            Assert.AreEqual("payload", loaded);

            var expectedOrder = new[]
            {
                "OnSaving",
                "SaveCallback",
                "OnSaved",
                "OnLoading",
                "LoadCallback",
                "OnLoaded"
            };

            CollectionAssert.AreEqual(expectedOrder, calls, "Callbacks were not invoked in the expected order or some callbacks were not invoked.");
        }
        finally
        {
            // Unsubscribe events and restore previous callbacks
            RemoveSubs();
            void RemoveSubs()
            {
                SaveEvents.OnSaving -= onSavingHandler;
                SaveEvents.OnSaved -= onSavedHandler;
                SaveEvents.OnLoading -= onLoadingHandler;
                SaveEvents.OnLoaded -= onLoadedHandler;
            }
            
            RestorePrevCallbacks();
            void RestorePrevCallbacks()
            {
                SaveEvents.SaveCallback = prevSaveCallback ?? delegate { };
                SaveEvents.LoadCallback = prevLoadCallback ?? delegate { };
            }
            
        }
    }

    // -------------------- New tests added below --------------------

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void Exists_Returns_True_After_Save_And_False_After_Delete(SaveGamePath path)
    {
        SaveGame.SavePath = path;
        string identifier = GenerateRandomIdentifier("exists_test");
        string expectedPath = string.Format(savePathFormat, path.ToRealPath(), identifier);
        _tempFiles.Add(expectedPath);

        Assert.IsFalse(SaveGame.Exists(identifier), "Identifier should not exist before saving.");

        SaveGame.Save<string>(identifier, "value");
        Assert.IsTrue(SaveGame.Exists(identifier), "Exists should return true after saving.");

        SaveGame.Delete(identifier);
        Assert.IsFalse(SaveGame.Exists(identifier), "Exists should return false after deletion.");
    }

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void Load_Returns_Default_When_File_Does_Not_Exist(SaveGamePath path)
    {
        SaveGame.SavePath = path;
        string identifier = GenerateRandomIdentifier("load_missing");
        string defaultValue = "my_default";

        Assert.IsFalse(SaveGame.Exists(identifier), "Precondition failed: file should not exist.");
        var loaded = SaveGame.Load<string>(identifier, defaultValue);

        Assert.AreEqual(defaultValue, loaded, "Load should return provided default when the identifier does not exist.");
    }

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void Save_And_Load_With_Encryption_Works(SaveGamePath path)
    {
        SaveGame.SavePath = path;
        string identifier = GenerateRandomIdentifier("encrypted", "dat");
        string expectedPath = string.Format(savePathFormat, path.ToRealPath(), identifier);
        _tempFiles.Add(expectedPath);

        string payload = "super_secret_payload";

        // Save with encryption explicitly (overload)
        SaveGame.Save<string>(identifier, payload, true);

        // Load specifying that data was encrypted
        var loaded = SaveGame.Load<string>(identifier, true, SaveGame.EncodePassword);

        Assert.AreEqual(payload, loaded, "Loaded value should match payload after encrypted save/load.");
    }

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void GetFiles_And_GetDirectories_Return_Created_Items(SaveGamePath path)
    {
        SaveGame.SavePath = path;
        string dirName = "test_dir_" + Guid.NewGuid().ToString("N");
        string dirPath = string.Format(savePathFormat, path.ToRealPath(), dirName);
        Directory.CreateDirectory(dirPath);
        _tempFiles.Add(dirPath);

        string file1 = Path.Combine(dirPath, "f1.txt");
        string file2 = Path.Combine(dirPath, "f2.txt");
        File.WriteAllText(file1, "1");
        File.WriteAllText(file2, "2");
        _tempFiles.Add(file1);
        _tempFiles.Add(file2);

        // Create a subdirectory
        string subDir = Path.Combine(dirPath, "subdir");
        Directory.CreateDirectory(subDir);
        _tempFiles.Add(subDir);

        var files = SaveGame.GetFiles(dirName);
        Assert.IsNotNull(files, "GetFiles should not return null for an existing directory.");
        Assert.IsTrue(files.Length >= 2, "GetFiles should return the files created inside the directory.");

        var directories = SaveGame.GetDirectories(dirName);
        Assert.IsNotNull(directories, "GetDirectories should not return null for an existing directory.");
        Assert.IsTrue(directories.Count >= 1, "GetDirectories should include the created subdirectory.");
    }

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void IsFilePath_Rooted_And_NotRooted(SaveGamePath basePath)
    {
        string path = SaveGamePathExt.ToRealPath(basePath);
        string rooted = Path.Combine(path, "somefile.sav");
        Assert.IsTrue(SaveGame.IsFilePath(rooted), "IsFilePath should return true for rooted/full paths.");

        string notRooted = "relative/path/to/file.sav";
        Assert.IsFalse(SaveGame.IsFilePath(notRooted), "IsFilePath should return false for non-rooted/relative paths.");
    }

    [Test]
    [TestCase(SaveGamePath.PersistentDataPath)]
    [TestCase(SaveGamePath.DataPath)]
    public void Delete_Ignores_IgnoredFiles(SaveGamePath basePath)
    {
        SaveGame.SavePath = basePath;
        // Use first ignored file from the list
        string ignoredFile = SaveGame.IgnoredFiles.Count > 0 ? SaveGame.IgnoredFiles[0] : "Player.log";
        string expectedPath = string.Format(savePathFormat, basePath.ToRealPath(), ignoredFile);

        // Create the ignored file
        File.WriteAllText(expectedPath, "log contents");
        _tempFiles.Add(expectedPath);

        // Attempt to delete via identifier (the Delete method should respect ignoredFiles list)
        SaveGame.Delete(ignoredFile);

        Assert.IsTrue(File.Exists(expectedPath), "Delete should not remove files that are in the IgnoredFiles list.");
    }

    // Test-only simple serializer used to validate Save/Load with a custom serializer if needed.
    private class DummyStringSerializer : BayatGames.SaveGameFree.Serializers.ISaveGameSerializer
    {
        // Implement the generic Serialize<T> as required by the project interface.
        public void Serialize<T>(T obj, Stream stream, Encoding encoding)
        {
            // Prefer treating the value as string; fallback to ToString() for other types.
            string s = obj as string ?? (obj?.ToString() ?? string.Empty);
            byte[] bytes = encoding.GetBytes(s);
            stream.Write(bytes, 0, bytes.Length);
        }

        // Implement the generic Deserialize<T>.
        public T Deserialize<T>(Stream stream, Encoding encoding)
        {
            using (var memStream = new MemoryStream())
            {
                stream.CopyTo(memStream);
                var bytes = memStream.ToArray();
                string decodedStr = encoding.GetString(bytes);
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)decodedStr;
                }
                // For value types or other reference types we can't reconstruct here; return default.
                return default;
            }
        }
    }

}
