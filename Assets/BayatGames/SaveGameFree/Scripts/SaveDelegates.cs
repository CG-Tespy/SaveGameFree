using BayatGames.SaveGameFree.Encoders;
using BayatGames.SaveGameFree.Serializers;
using System.Text;

namespace BayatGames.SaveGameFree
{
    /// <summary>
    /// Save handler.
    /// </summary>
    public delegate void SaveHandler(object obj, string identifier, bool encode,
        string password, ISaveGameSerializer serializer, ISaveGameEncoder encoder,
        Encoding encoding, SaveGamePath path);

    /// <summary>
    /// Load handler.
    /// </summary>
    public delegate void LoadHandler(object loadedObj, string identifier, bool encode,
        string password, ISaveGameSerializer serializer, ISaveGameEncoder encoder,
        Encoding encoding, SaveGamePath path);
}