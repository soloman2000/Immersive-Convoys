using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace Convoys.Scenarios
{
    public class MerchantApprenticeScenario : ConvoyScenario
    {
        public override string Id => "merchant_apprentice_convoy";
        public override string DisplayName => "Trade Convoy";
        public override int RansomValueMultiplier => 2;

        public override bool CanSpawn(Settlement origin, Settlement target) => true;

        public override Hero CreateLeaderHero(Settlement origin, Clan clan)
        {
            CharacterObject heroTemplate = origin.Culture.BasicTroop;

            Hero apprentice = HeroCreator.CreateSpecialHero(
                heroTemplate,
                origin,
                clan,
                null,
                18
            );

            TextObject name = new TextObject("{=!}Apprentice {FIRST_NAME}");
            name.SetTextVariable("FIRST_NAME", apprentice.FirstName ?? apprentice.Name);
            apprentice.SetName(name, name);
            apprentice.ChangeState(Hero.CharacterStates.Active);

            return apprentice;
        }

        public override PartyTemplateObject GetPartyTemplate(CultureObject culture)
        {
            return culture.DefaultPartyTemplate;
        }
    }
}