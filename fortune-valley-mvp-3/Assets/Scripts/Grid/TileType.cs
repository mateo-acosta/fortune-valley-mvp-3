namespace FortuneValley.Grid
{
    /// <summary>
    /// Defines the type of a tile in the city grid.
    /// Each tile type has specific placement rules for assets.
    /// </summary>
    public enum TileType
    {
        Empty,      // Nothing here - default state
        Road,       // Roads, paths - vehicles allowed
        Park,       // Green spaces - trees, benches allowed
        Building,   // Generic buildings (decorative, not purchasable)
        Lot,        // Purchasable lots (links to CityLotDefinition)
        Water,      // Rivers, ponds - decorative only
        Special,    // Unique landmarks, player restaurant
        Border      // Impassable decorative border (rocks, trees)
    }
}
