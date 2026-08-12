using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Convoys.Scenarios
{
    public abstract class ConvoyScenario
    {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }

        /// <summary>
        /// Validates whether world conditions allow this scenario to spawn.
        /// </summary>
        public abstract bool CanSpawn(Settlement origin, Settlement target);

        /// <summary>
        /// Creates and returns the leader hero for this specific convoy.
        /// </summary>
        public abstract Hero CreateLeaderHero(Settlement origin, Clan clan);

        /// <summary>
        /// Defines troop composition template for guards.
        /// </summary>
        public abstract PartyTemplateObject GetPartyTemplate(CultureObject culture);

        /// <summary>
        /// Multiplier applied to prisoner ransom value if this hero is captured.
        /// </summary>
        public virtual int RansomValueMultiplier => 1;

        /// <summary>
        /// Optional hook for scenario-specific behavior when the party spawns.
        /// </summary>
        public virtual void OnPartyCreated(MobileParty party, Hero leader) { }
    }
}