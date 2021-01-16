namespace GreengladeLookout
{
    public class GreengladeSettings
    {
        public float SubstringBookendWeight { get; init; } = 1f;

        public float SubstringBookendTaper { get; init; } = 1f;

        public float StringMatchThreshold { get; init; } = 0.5f;

        public float UncollectibleCardDownscaleFactor { get; init; } = 1f;

        public float GlobalKeywordDownscaleFactor { get; init; } = 1f;

        public string? BotName { get; init; }

        public string? InfoMaintainerName { get; init; }

        public string? InfoInviteLink { get; init; }

        public string? InfoSourceLink { get; init; }

        public string? InfoDonationLink { get; init; }
    }
}
