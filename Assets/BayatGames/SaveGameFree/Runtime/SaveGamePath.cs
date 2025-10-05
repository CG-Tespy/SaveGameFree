using UnityEngine;

namespace Bayat.Unity.SaveGameFree
{
    /// <summary>
    /// Save game path. base paths for your save games.
    /// </summary>
    public enum SaveGamePath
    {
        /// <summary>
        /// The persistent data path. Application.persistentDataPath
        /// </summary>
        PersistentDataPath,

        /// <summary>
        /// The data path. Application.dataPath
        /// </summary>
        DataPath,

        /// <summary>
        /// The custom path
        /// </summary>
        Custom

    }

    public static class SaveGamePathExt
    {
        public static string ToRealPath(this SaveGamePath path)
        {
            return path switch
            {
                SaveGamePath.PersistentDataPath => Application.persistentDataPath,
                SaveGamePath.DataPath => Application.dataPath,
                SaveGamePath.Custom => "",
                _ => Application.persistentDataPath,
            };
        }
    }

}
