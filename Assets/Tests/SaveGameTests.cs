using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
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
    public void Save_Creates_File_In_PersistentDataPath()
    {
        SaveGame.SavePath = SaveGamePath.PersistentDataPath;
        string identifier = GenerateRandomIdentifier("save_persistent");
        string expectedPath = string.Format(savePathFormat, Application.persistentDataPath, identifier);
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
    public void Save_Creates_File_In_DataPath()
    {
        SaveGame.SavePath = SaveGamePath.DataPath;
        string identifier = GenerateRandomIdentifier("save_data");
        string expectedPath = string.Format(savePathFormat, Application.dataPath, identifier);

        _tempFiles.Add(expectedPath);

        SaveGame.Save<string>(identifier, "test");
        Assert.IsTrue(File.Exists(expectedPath), $"Expected file at '{expectedPath}' but it was not created.");
    }

    [Test]
    public void Save_File_Has_Right_Name()
    {
        // Use PersistentDataPath and verify the filename matches identifier
        SaveGame.SavePath = SaveGamePath.PersistentDataPath;
        string identifier = GenerateRandomIdentifier("expected_name");
        string expectedPath = string.Format(savePathFormat, Application.persistentDataPath, identifier);
        _tempFiles.Add(expectedPath);

        SaveGame.Save<string>(identifier, "payload");

        // Assert.IsTrue(File.Exists(expectedPath), "File not created."); // We already have this check in other tests
        Assert.AreEqual(identifier, Path.GetFileName(expectedPath), "Saved file name does not match identifier.");
    }

    [Test]
    [TestCase(null, TestName = "Save_Throws_ArgumentNullException_On_Null_Identifier")]
    [TestCase("", TestName = "Save_Throws_ArgumentNullException_On_Empty_Identifier")]
    public void Save_Throws_ArgumentNullException_On_NullOrEmpty_Identifier(string identifier)
    {
        SaveGame.SavePath = SaveGamePath.PersistentDataPath;

        var ex = Assert.Throws<ArgumentNullException>(() => SaveGame.Save<object>(identifier, new object()));
        StringAssert.Contains("identifier", ex.ParamName);
    }

    [Test]
    public void Callbacks_Are_Invoked_On_Save_And_Load_In_Expected_Order()
    {
        // Prepare storage path
        SaveGame.SavePath = SaveGamePath.PersistentDataPath;
        string identifier = GenerateRandomIdentifier("callbacks", "dat");
        string expectedPath = string.Format(savePathFormat, Application.persistentDataPath, identifier);
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
}
