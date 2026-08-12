using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.Core.Equipment;

namespace Convoys.Scenarios
{
    public class RoyalHeirConvoyScenario : ConvoyScenario
    {
        public override string Id => "royal_heir_convoy";
        public override string DisplayName => "Royal Escort";
        public override int RansomValueMultiplier => 15; // Worth 15x standard ransom

        public override bool CanSpawn(Settlement origin, Settlement target)
        {
            // Only spawn if origin town belongs to a kingdom with an active ruler clan
            return origin?.MapFaction is Kingdom kingdom && kingdom.Leader != null;
        }

        public override Hero CreateLeaderHero(Settlement origin, Clan clan)
        {
            Kingdom kingdom = origin.MapFaction as Kingdom;
            Clan royalClan = kingdom?.Leader?.Clan ?? clan;

            // Pick template from elite noble line
            CharacterObject heroTemplate = origin.Culture.EliteBasicTroop;

            // Create a royal heir
            Hero heir = HeroCreator.CreateSpecialHero(
                heroTemplate,
                origin,
                royalClan,
                null,
                15
            );

            TextObject name = new TextObject("{=!}Royal Heir {FIRST_NAME} of {CLAN}");
            name.SetTextVariable("FIRST_NAME", heir.FirstName ?? heir.Name);
            name.SetTextVariable("CLAN", royalClan.Name);
            heir.SetName(name, name);

            // Apply Culture-Specific Elite Armor (No Helmet) & Mount
            EquipEliteArmorAndMountNoHelmet(heir, origin.Culture);

            heir.ChangeState(Hero.CharacterStates.Active);

            return heir;
        }

        private void EquipEliteArmorAndMountNoHelmet(Hero hero, CultureObject culture)
        {
            Equipment equipment = new Equipment(EquipmentType.Battle);

            string cultureId = culture?.StringId ?? "empire";
            string bodyId, capeId, gloveId, bootId, weaponId, shieldId, horseId, harnessId;

            switch (cultureId)
            {
                case "vlandia":
                    bodyId = "coat_of_plates";
                    capeId = "vlandia_pauldrons";
                    gloveId = "lordly_mail_gauntlets";
                    bootId = "plate_greaves";
                    weaponId = "vlandia_sword_4_t5";
                    shieldId = "vlandian_fortress_shield";
                    horseId = "charger";
                    harnessId = "vlandia_horse_harness_a";
                    break;

                case "sturgia":
                    bodyId = "sturgia_heavy_lamellar";
                    capeId = "sturgia_noble_shoulder";
                    gloveId = "mail_mitons";
                    bootId = "sturgian_boots_a";
                    weaponId = "sturgia_sword_4_t5";
                    shieldId = "sturgian_heavy_shield";
                    horseId = "charger";
                    harnessId = "sturgia_horse_harness_a";
                    break;

                case "aserai":
                    bodyId = "aserai_scale_armor";
                    capeId = "aserai_shoulder_a";
                    gloveId = "aserai_arm_guards";
                    bootId = "aserai_boots_a";
                    weaponId = "aserai_sword_3_t5";
                    shieldId = "aserai_heavy_shield";
                    horseId = "charger";
                    harnessId = "aserai_horse_harness_a";
                    break;

                case "khuzait":
                    bodyId = "khuzait_heavy_lamellar";
                    capeId = "khuzait_shoulder_leather";
                    gloveId = "khuzait_arm_guards";
                    bootId = "khuzait_boots_a";
                    weaponId = "khuzait_sword_4_t5";
                    shieldId = "khuzait_round_shield";
                    horseId = "charger";
                    harnessId = "khuzait_horse_harness_a";
                    break;

                case "battania":
                    bodyId = "battania_noble_mail";
                    capeId = "battania_shoulder_a";
                    gloveId = "battania_arm_guards";
                    bootId = "battania_boots_a";
                    weaponId = "battania_sword_3_t5";
                    shieldId = "battanian_wood_shield";
                    horseId = "charger";
                    harnessId = "battania_horse_harness_a";
                    break;

                case "empire":
                default:
                    bodyId = "imperial_scale_armor";
                    capeId = "imperial_lord_pauldrons";
                    gloveId = "mail_mitons";
                    bootId = "strapped_leather_boots";
                    weaponId = "empire_noble_sword";
                    shieldId = "imperial_heavy_shield";
                    horseId = "charger";                  
                    harnessId = "empire_horse_harness_a"; 
                    break;
            }

            // Explicitly leave head empty
            equipment[EquipmentIndex.Head] = EquipmentElement.Invalid;

            // Load and assign body armor slots
            AssignSlot(equipment, EquipmentIndex.Body, bodyId, "coat_of_plates");
            AssignSlot(equipment, EquipmentIndex.Cape, capeId, "imperial_lord_pauldrons");
            AssignSlot(equipment, EquipmentIndex.Gloves, gloveId, "mail_mitons");
            AssignSlot(equipment, EquipmentIndex.Leg, bootId, "strapped_leather_boots");

            // Load and assign weapons
            AssignSlot(equipment, EquipmentIndex.Weapon0, weaponId, "empire_noble_sword");
            AssignSlot(equipment, EquipmentIndex.Weapon1, shieldId, "imperial_heavy_shield");

            // Load and assign horse and harness
            AssignSlot(equipment, EquipmentIndex.Horse, horseId, "empire_horse");
            AssignSlot(equipment, EquipmentIndex.HorseHarness, harnessId, "empire_horse_harness_a");

            // Apply custom equipment configuration to hero
            hero.BattleEquipment.FillFrom(equipment);
            hero.CivilianEquipment.FillFrom(equipment);
        }

        private void AssignSlot(Equipment equipment, EquipmentIndex slot, string primaryItemId, string fallbackItemId)
        {
            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(primaryItemId)
                           ?? MBObjectManager.Instance.GetObject<ItemObject>(fallbackItemId);

            if (item != null)
            {
                equipment[slot] = new EquipmentElement(item);
            }
        }


        public override PartyTemplateObject GetPartyTemplate(CultureObject culture)
        {
            return culture.EliteBasicTroop?.Culture?.DefaultPartyTemplate
                   ?? culture.DefaultPartyTemplate;
        }

        public override void OnPartyCreated(MobileParty party, Hero leader)
        {
            party.SetCustomHomeSettlement(leader.HomeSettlement);
        }
    }
}