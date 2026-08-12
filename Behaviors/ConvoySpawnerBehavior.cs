using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using Convoys.Scenarios;

namespace Convoys.Behaviors
{
    public class ConvoySpawnerBehavior : CampaignBehaviorBase
    {
        private readonly List<ConvoyTrackData> _trackedConvoys = new List<ConvoyTrackData>();
        private readonly List<ConvoyScenario> _registeredScenarios = new List<ConvoyScenario>();

        private Hero _pendingDialogueHero = null;
        private readonly Color DebugColor = Color.FromUint(0x00FFFFFF); // Cyan debug text

        public ConvoySpawnerBehavior()
        {
            _registeredScenarios.Add(new RoyalHeirConvoyScenario());
            _registeredScenarios.Add(new MerchantApprenticeScenario());
        }

        public override void RegisterEvents()
        {
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddConvoyCaptureDialogues(starter);
        }

        private void OnCampaignTick(float dt)
        {
            if (Input.IsKeyPressed(InputKey.F10))
            {
                SpawnRandomScenarioConvoy();
            }
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (mapEvent == null || !mapEvent.IsPlayerMapEvent || mapEvent.WinningSide != mapEvent.PlayerSide)
            {
                return;
            }

            ProcessDefeatedSideForTargetHero(mapEvent);
        }

        private void ProcessDefeatedSideForTargetHero(MapEvent mapEvent)
        {
            if (mapEvent == null) return;

            MapEventSide defeatedSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
            MapEventSide winnerSide = mapEvent.GetMapEventSide(mapEvent.WinningSide);
            if (defeatedSide == null || winnerSide == null) return;

            foreach (MapEventParty mapEventParty in defeatedSide.Parties)
            {
                PartyBase defeatedParty = mapEventParty?.Party;
                if (defeatedParty == null) continue;

                // Match against tracked scenario convoy leader
                ConvoyTrackData track = _trackedConvoys?.FirstOrDefault(t =>
                    t.Party == defeatedParty.MobileParty ||
                    (t.Leader != null && defeatedParty.MemberRoster != null && defeatedParty.MemberRoster.Contains(t.Leader.CharacterObject))
                );

                if (track != null && track.Leader != null)
                {
                    Hero targetHero = track.Leader;
                    InformationManager.DisplayMessage(new InformationMessage($"[DEBUG] Queuing defeat logic for {targetHero.Name}...", DebugColor));

                    // 1. Ensure target hero is active and wounded
                    if (!targetHero.IsAlive)
                    {
                        targetHero.ChangeState(Hero.CharacterStates.Active);
                    }
                    targetHero.MakeWounded();

                    // 2. Remove hero from defeated combat roster so they aren't treated as active combatants
                    if (defeatedParty.MemberRoster != null && defeatedParty.MemberRoster.Contains(targetHero.CharacterObject))
                    {
                        defeatedParty.MemberRoster.RemoveTroop(targetHero.CharacterObject, 1);
                    }

                    // 3. Clear leadership to prevent party AI loops
                    if (defeatedParty.MobileParty != null && defeatedParty.MobileParty.LeaderHero == targetHero)
                    {
                        defeatedParty.MobileParty.RemovePartyLeader();
                    }

                    // 4. Add hero to the winner party's PrisonRoster for the post-battle screen
                    if (winnerSide.LeaderParty != null && winnerSide.LeaderParty.PrisonRoster != null)
                    {
                        winnerSide.LeaderParty.PrisonRoster.AddToCounts(targetHero.CharacterObject, 1);
                    }

                    // 5. Store reference for post-battle dialogue
                    _pendingDialogueHero = targetHero;

                    break; // Process one leader per battle
                }
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent.IsPlayerMapEvent && _pendingDialogueHero != null)
            {
                Hero targetHero = _pendingDialogueHero;
                _pendingDialogueHero = null; // Clear queue

                // Check if the hero was actually selected and kept in player's prisoner roster
                bool isKeptAsPrisoner = PartyBase.MainParty.PrisonRoster.Contains(targetHero.CharacterObject) ||
                                        targetHero.PartyBelongedToAsPrisoner == PartyBase.MainParty;

                if (isKeptAsPrisoner)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"[DEBUG] Opening post-battle dialogue with {targetHero.Name}...", DebugColor));

                    // Safely open conversation on campaign map
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty),
                        new ConversationCharacterData(targetHero.CharacterObject)
                    );
                }
            }
        }

        private void AddConvoyCaptureDialogues(CampaignGameStarter starter)
        {
            // Root dialogue line
            starter.AddDialogLine(
                "convoy_hero_defeated_start",
                "start",
                "convoy_hero_defeated_options",
                "I yield! My escort is broken. What are you going to do with me?",
                IsConvoyTargetHeroInConversation,
                null
            );

            // Choice 1: Hold as prisoner
            starter.AddPlayerLine(
                "convoy_hero_take_prisoner",
                "convoy_hero_defeated_options",
                "close_window",
                "You are my prisoner now. Yield your arms.",
                null,
                OnTakeHeroPrisonerConsequence
            );

            // Choice 2: Set free
            starter.AddPlayerLine(
                "convoy_hero_release_free",
                "convoy_hero_defeated_options",
                "close_window",
                "Go. I have no interest in holding you captive.",
                null,
                OnReleaseHeroConsequence
            );
        }

        private bool IsConvoyTargetHeroInConversation()
        {
            Hero conversationHero = Hero.OneToOneConversationHero;
            if (conversationHero == null) return false;

            // Must be a tracked convoy leader
            bool isTracked = _trackedConvoys.Any(t => t.Leader == conversationHero);
            if (!isTracked) return false;

            // FIX: Only trigger post-battle yield dialogue if the hero is taken prisoner or defeated
            bool isPrisoner = conversationHero.IsPrisoner;
            bool partyDefeated = conversationHero.PartyBelongedTo == null ||
                                 conversationHero.PartyBelongedTo.MemberRoster.TotalHealthyCount == 0;

            return isPrisoner || partyDefeated;
        }

        private void OnTakeHeroPrisonerConsequence()
        {
            Hero targetHero = Hero.OneToOneConversationHero;
            if (targetHero != null)
            {
                // Ensure captive state and prisoner roster assignment are updated natively
                if (targetHero.PartyBelongedToAsPrisoner != PartyBase.MainParty)
                {
                    TakePrisonerAction.Apply(PartyBase.MainParty, targetHero);
                }
            }
        }

        private void OnReleaseHeroConsequence()
        {
            Hero targetHero = Hero.OneToOneConversationHero;
            if (targetHero != null)
            {
                // Releases the hero and updates campaign state cleanly
                EndCaptivityAction.ApplyByPeace(targetHero);
            }
        }

        private void OnHeroPrisonerTaken(PartyBase captor, Hero prisoner)
        {
            ConvoyTrackData track = _trackedConvoys.FirstOrDefault(t => t.Leader == prisoner);

            if (track != null && captor == PartyBase.MainParty)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[HIGH VALUE PRISONER] You captured {prisoner.Name}! Ransom Value: {track.RansomValue} denars.",
                    Color.FromUint(0x00FF00FF)
                ));
            }
        }

        private void SpawnRandomScenarioConvoy()
        {
            if (Hero.MainHero == null || MobileParty.MainParty == null)
                return;

            CampaignVec2 playerPos = MobileParty.MainParty.Position;
            CultureObject culture = Hero.MainHero.Culture;

            Settlement originSettlement = Settlement.All
                .Where(s => s.IsTown)
                .OrderBy(s => s.Position.ToVec2().DistanceSquared(playerPos.ToVec2()))
                .FirstOrDefault();

            Settlement targetSettlement = Settlement.All
                .Where(s => (s.IsTown || s.IsCastle) && s != originSettlement)
                .OrderBy(s => s.Position.ToVec2().DistanceSquared(originSettlement.Position.ToVec2()))
                .FirstOrDefault();

            List<ConvoyScenario> validScenarios = _registeredScenarios
                .Where(s => s.CanSpawn(originSettlement, targetSettlement))
                .ToList();

            if (!validScenarios.Any())
                return;

            ConvoyScenario chosenScenario = validScenarios[MBRandom.RandomInt(validScenarios.Count)];

            MobileParty convoy = ConvoyPartyComponent.CreateConvoyParty(
                $"convoy_{chosenScenario.Id}",
                originSettlement,
                culture,
                playerPos,
                chosenScenario
            );

            if (targetSettlement != null)
            {
                convoy.SetMoveGoToSettlement(targetSettlement, MobileParty.NavigationType.Default, false);
            }

            ConvoyTrackData track = new ConvoyTrackData(convoy, convoy.LeaderHero, targetSettlement, chosenScenario);
            _trackedConvoys.Add(track);

            InformationManager.DisplayMessage(new InformationMessage(
                $"[{chosenScenario.DisplayName}] Spawned '{track.PartyName}' led by {convoy.LeaderHero?.Name}! Ransom Value: {track.RansomValue} denars."
            ));
        }
    }
}