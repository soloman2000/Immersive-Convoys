using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using Convoys.Scenarios;

namespace Convoys.Behaviors
{
    public class ConvoyTrackData
    {
        public TextObject PartyName { get; set; }
        public MobileParty Party { get; set; }
        public Hero Leader { get; set; }
        public Settlement TargetSettlement { get; set; }
        public ConvoyScenario Scenario { get; set; }

        public int RansomValue => (Leader?.CharacterObject?.Level ?? 10) * 500 * Scenario.RansomValueMultiplier;

        public ConvoyTrackData(MobileParty party, Hero leader, Settlement targetSettlement, ConvoyScenario scenario)
        {
            Party = party;
            Leader = leader;
            TargetSettlement = targetSettlement;
            Scenario = scenario;
            PartyName = party?.Party?.CustomName ?? party?.Name ?? new TextObject("{=!}Unknown Convoy");
        }
    }
}