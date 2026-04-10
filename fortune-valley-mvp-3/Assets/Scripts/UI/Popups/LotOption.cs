namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Simple data holder for a lot choice in the LotSelectionPopup.
    /// </summary>
    public readonly struct LotOption
    {
        public string LotId { get; }
        public string LotName { get; }

        public LotOption(string lotId, string lotName)
        {
            LotId = lotId;
            LotName = lotName;
        }
    }
}
