using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using Convoys.Scenarios;

namespace Convoys.Behaviors
{
    public class ConvoyPartyComponent : PartyComponent
    {
        private readonly TextObject _name;
        private readonly Settlement _homeSettlement;
        private Hero _leaderHero;

        public ConvoyScenario Scenario { get; private set; }

        public override TextObject Name => _name;
        public override Settlement HomeSettlement => _homeSettlement;
        public override Hero Leader => _leaderHero;
        public override Hero PartyOwner => _leaderHero ?? _homeSettlement?.OwnerClan?.Leader;

        public ConvoyPartyComponent(TextObject name, Settlement homeSettlement, ConvoyScenario scenario)
        {
            _name = name;
            _homeSettlement = homeSettlement;
            Scenario = scenario;
        }

        public void SetLeader(Hero leader)
        {
            _leaderHero = leader;
        }

        public override Banner GetDefaultComponentBanner()
        {
            return _leaderHero?.Clan?.Banner ?? _homeSettlement?.Banner ?? _homeSettlement?.OwnerClan?.Banner;
        }

        public static MobileParty CreateConvoyParty(
            string stringId,
            Settlement homeSettlement,
            CultureObject culture,
            CampaignVec2 spawnPosition,
            ConvoyScenario scenario)
        {
            TextObject name = new TextObject($"{{=!}}{scenario.DisplayName} from {{SETTLEMENT}}");
            name.SetTextVariable("SETTLEMENT", homeSettlement?.Name ?? new TextObject("{=!}Unknown"));

            ConvoyPartyComponent component = new ConvoyPartyComponent(name, homeSettlement, scenario);
            MobileParty convoy = MobileParty.CreateParty(stringId, component);

            Clan targetClan = homeSettlement?.OwnerClan
                ?? Clan.All.FirstOrDefault(c => c.Culture == culture && !c.IsEliminated)
                ?? Clan.All.FirstOrDefault(c => !c.IsEliminated);

            convoy.ActualClan = targetClan;
            convoy.Party.SetCustomName(name);
            convoy.InitializePartyTrade(MobileParty.DefaultPartyTradeInitialGold);

            // Fetch scenario troop template
            PartyTemplateObject template = scenario.GetPartyTemplate(culture);
            convoy.InitializeMobilePartyAtPosition(template, spawnPosition);

            // Execute scenario hero creation
            Hero leaderHero = scenario.CreateLeaderHero(homeSettlement, targetClan);

            // Roster and leadership binding
            convoy.MemberRoster.AddToCounts(leaderHero.CharacterObject, 1, false, 0, 0, true, -1);
            AddHeroToPartyAction.Apply(leaderHero, convoy);
            component.SetLeader(leaderHero);
            convoy.ChangePartyLeader(leaderHero);

            // Scenario post-creation hook
            scenario.OnPartyCreated(convoy, leaderHero);

            convoy.IsVisible = true;
            convoy.Aggressiveness = 0f;

            return convoy;
        }
    }
}