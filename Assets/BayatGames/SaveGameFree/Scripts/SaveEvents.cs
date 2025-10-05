namespace BayatGames.SaveGameFree
{
    public static class SaveEvents 
    {
        /// <summary>
        /// Occurs when started saving.
        /// </summary>
        public static SaveHandler OnSaving = delegate { };

        /// <summary>
        /// Occurs when saving finished.
        /// </summary>
        public static SaveHandler OnSaved = delegate { };

        /// <summary>
        /// Occurs when started loading.
        /// </summary>
        public static LoadHandler OnLoading = delegate { };

        /// <summary>
        /// Occurs when on loading finishes.
        /// </summary>
        public static LoadHandler OnLoaded = delegate { };

        /// <summary>
        /// The save callback.
        /// </summary>
        public static SaveHandler SaveCallback = delegate { };

        /// <summary>
        /// The load callback.
        /// </summary>
        public static LoadHandler LoadCallback = delegate { };
    }
}