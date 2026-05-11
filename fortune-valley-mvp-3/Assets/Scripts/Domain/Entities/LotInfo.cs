namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Lightweight info about an owned lot for accident rolling.
    /// </summary>
    public struct LotInfo
    {
        private string _lotId;

        public LotInfo(string lotId)
        {
            _lotId = lotId;
        }

        public string LotId => _lotId;
    }
}
