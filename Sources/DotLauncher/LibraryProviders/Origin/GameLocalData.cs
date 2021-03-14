namespace DotLauncher.LibraryProviders.Origin
{
    internal sealed class GameLocalData
    {
        public GameLocalData(string offerType, string displayName)
        {
            OfferType = offerType;
            DisplayName = displayName;
        }

        public string OfferType { get; }
        public string DisplayName { get; }
    }
}
