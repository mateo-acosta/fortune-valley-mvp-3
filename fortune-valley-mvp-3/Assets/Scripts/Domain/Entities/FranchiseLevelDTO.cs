using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Serializable DTO representing a single lot's franchise tier.
    /// Rails contract uses a hash {"lot_id": tier}; Unity uses an
    /// array of these objects because JsonUtility cannot serialize dicts.
    /// </summary>
    [Serializable]
    public class FranchiseLevelDTO
    {
        public string lot_id;
        public int tier;
    }
}
