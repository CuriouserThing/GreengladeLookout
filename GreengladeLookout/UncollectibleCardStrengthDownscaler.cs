using System;
using Bjerg;
using Bjerg.CatalogSearching;

namespace GreengladeLookout
{
    public class UncollectibleCardStrengthDownscaler : IItemStrengthDownscaler<ICard>
    {
        public UncollectibleCardStrengthDownscaler(float multiplier)
        {
            if (multiplier < 0f || multiplier > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            }

            Multiplier = multiplier;
        }

        public float Multiplier { get; }

        public float GetMultiplier(ICard item)
        {
            return item.Collectible ? 1f : Multiplier;
        }
    }
}
