﻿using BannerKings.Managers.Duties;
using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static BannerKings.Managers.TitleManager;

namespace BannerKings.Behaviours
{
    public class BKArmyBehavior : CampaignBehaviorBase
    {
        public static AuxiliumDuty PlayerArmyDuty { get; set; }

        public override void RegisterEvents()
        {
            CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyJoinedArmyEvent));
            CampaignEvents.ArmyCreated.AddNonSerializedListener(this, new Action<Army>(this.OnArmyCreated));
            CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, new Action<Army, Army.ArmyDispersionReason, bool>(this.OnArmyDispersed));
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(this.AiHourlyTick));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public void OnPartyJoinedArmyEvent(MobileParty party)
        {
            Kingdom playerKingdom = Clan.PlayerClan.Kingdom;
            if (playerKingdom == null || playerKingdom != party.LeaderHero.Clan.Kingdom ||
                BannerKingsConfig.Instance.TitleManager == null || party == MobileParty.MainParty)
                return;

            FeudalTitle playerTitle = BannerKingsConfig.Instance.TitleManager.GetHighestTitleWithinFaction(Hero.MainHero, playerKingdom);
            if (playerTitle != null)
                this.EvaluateSummonPlayer(playerTitle, party.Army, party);
        }

        public void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
        {

            MobileParty leaderParty = army.LeaderParty;
            Kingdom playerKingdom = Clan.PlayerClan.Kingdom;
            if (playerKingdom == null || playerKingdom != army.Kingdom || PlayerArmyDuty == null ||
                BannerKingsConfig.Instance.TitleManager == null || leaderParty == MobileParty.MainParty)
                return;

            if (army.LeaderParty == PlayerArmyDuty.Party || army.Parties.Contains(PlayerArmyDuty.Party))
            {
                PlayerArmyDuty.Finish();
                PlayerArmyDuty = null;
            }   
        }

        public void OnArmyCreated(Army army)
        {

            MobileParty leaderParty = army.LeaderParty;
            Kingdom playerKingdom = Clan.PlayerClan.Kingdom;
            if (playerKingdom == null || playerKingdom != leaderParty.LeaderHero.Clan.Kingdom ||
                BannerKingsConfig.Instance.TitleManager == null || leaderParty == MobileParty.MainParty)
                return;

            FeudalTitle playerTitle = BannerKingsConfig.Instance.TitleManager.GetHighestTitleWithinFaction(Hero.MainHero, playerKingdom);
            if (playerTitle != null)
                this.EvaluateSummonPlayer(playerTitle, army);
        }

        private void EvaluateSummonPlayer(FeudalTitle playerTitle, Army army, MobileParty joinningParty = null)
        {
            FeudalTitle suzerain = BannerKingsConfig.Instance.TitleManager.GetImmediateSuzerain(playerTitle);
            if (suzerain == null) return;
            if (Hero.MainHero.IsPrisoner) return;

            MobileParty suzerainParty = this.EvaluateSuzerainParty(army, suzerain.deJure, joinningParty);
            if (suzerainParty != null)
            {
                float days = 2f;
                Settlement settlement = SettlementHelper.FindNearestSettlement((Settlement x) => x.IsFortification || x.IsVillage, army.AiBehaviorObject);
                InformationManager.ShowInquiry(new InquiryData("Duty Calls",
                    string.Format("Your suzerain, {0}, has summoned you to fulfill your oath of military aid. You have {1} to join {2}, currently close to {3}.", 
                    suzerainParty.LeaderHero.Name.ToString(), days, army.Name, settlement.Name),
                    true, false, "Understood", null, null, null), false);
                BKArmyBehavior.PlayerArmyDuty = new Managers.Duties.AuxiliumDuty(CampaignTime.DaysFromNow(days), suzerainParty);
            }
        }

        private MobileParty EvaluateSuzerainParty(Army army, Hero target, MobileParty joinningParty = null)
        {
            MobileParty suzerainParty = null;
            MobileParty leaderParty = army.LeaderParty;
            if (leaderParty.LeaderHero == target)
                suzerainParty = leaderParty;
            else if (joinningParty != null && joinningParty.LeaderHero == target)
                suzerainParty = joinningParty;
            else foreach (MobileParty party in army.Parties)
                    if (party.LeaderHero == target)
                        suzerainParty = party;

            return suzerainParty;
        }

        public void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
        {
            if (mobileParty.IsMilitia || mobileParty.IsCaravan || mobileParty.IsVillager || 
                mobileParty.IsBandit || !mobileParty.MapFaction.IsKingdomFaction)
                return;

            if (PlayerArmyDuty == null || mobileParty != PlayerArmyDuty.Party) return;

            Army army = mobileParty.Army;
            if (army == null)
            {
                PlayerArmyDuty.Finish();
                PlayerArmyDuty = null;
                return;
            }

            PlayerArmyDuty.Tick();
        }
    }
}
