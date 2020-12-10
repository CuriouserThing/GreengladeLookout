namespace GreengladeLookout
{
    public class GreengladeSettings
    {
        public float SubstringBookendWeight { get; init; } = 1f;

        public float SubstringBookendTaper { get; init; } = 1f;

        public float StringMatchThreshold { get; init; } = 0.5f;

        public float UncollectibleCardDownscaleFactor { get; init; } = 1f;

        public float GlobalKeywordDownscaleFactor { get; init; } = 1f;
    }
}
