﻿using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static TaleWorlds.CampaignSystem.Actions.ChangeKingdomAction;
using TaleWorlds.CampaignSystem.Election;
using BannerKings.Managers.Kingdoms;
using BannerKings.Managers.Titles;
using BannerKings.Managers.Helpers;

namespace BannerKings.Behaviours
{
    public class BKTitleBehavior : CampaignBehaviorBase
    {
        private Dictionary<Settlement, List<Clan>> conqueredByArmies = new Dictionary<Settlement, List<Clan>>();

        public override void RegisterEvents()
        {
            //CampaignEvents.RulingCLanChanged.AddNonSerializedListener(this, new Action<Kingdom, Clan>(this.OnRulingClanChanged));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyTick));
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(OnHeroKilled));
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(OnDailyTickSettlement));
            CampaignEvents.ClanChangedKingdom.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomActionDetail, bool>(OnClanChangedKingdom));
            CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, new Action<Clan>(OnClanDestroyed));
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(OnOwnerChanged));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("bannerkings-armies", ref conqueredByArmies);
        }

        private void OnRulingClanChanged(Kingdom kingdom, Clan clan)
        {
            if (BannerKingsConfig.Instance.TitleManager == null) return;

            FeudalTitle sovereign = BannerKingsConfig.Instance.TitleManager.GetSovereignTitle(kingdom);
            if (sovereign == null || sovereign.contract == null) return;

            if (sovereign.deJure == clan.Leader) return;

        }

        private void OnDailyTick()
        {
            if (BannerKingsConfig.Instance.TitleManager == null) return;

            foreach (FeudalTitle duchy in BannerKingsConfig.Instance.TitleManager.GetAllTitlesByType(TitleType.Dukedom))
            {
                duchy.TickClaims();
                Kingdom faction = duchy.deJure.Clan.Kingdom;
                if (faction == null || faction != duchy.DeFacto.Clan.Kingdom) continue;

                FeudalTitle currentSovereign = duchy.sovereign;
                FeudalTitle currentFactionSovereign = BannerKingsConfig.Instance.TitleManager.GetSovereignTitle(faction);
                if (currentSovereign == null || currentFactionSovereign == null || currentSovereign == currentFactionSovereign) continue;

                duchy.TickDrift(currentFactionSovereign);
            }

            foreach (FeudalTitle kingdom in BannerKingsConfig.Instance.TitleManager.GetAllTitlesByType(TitleType.Kingdom))
                kingdom.TickClaims();
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
        {
            if (victim == null || victim.Clan == null || BannerKingsConfig.Instance.TitleManager == null) return;

            BannerKingsConfig.Instance.TitleManager.RemoveKnights(victim);
            FeudalTitle sovereign = BannerKingsConfig.Instance.TitleManager.GetSovereignTitle(victim.Clan.Kingdom);
            if (sovereign == null || sovereign.contract == null) return;

            List<FeudalTitle> titles = new List<FeudalTitle>(BannerKingsConfig.Instance.TitleManager.GetAllDeJure(victim));
            if (titles.Count == 0) return;

            InheritanceHelper.ApplyInheritanceAllTitles(titles, victim);

            if (sovereign != null)
                foreach (FeudalTitle title in titles)
                {
                    if (title.Equals(sovereign))
                    {
                        SuccessionHelper.ApplySuccession(title, victim, victim.Clan.Kingdom);
                        break;
                    }
                }
        }

        public void OnDailyTickSettlement(Settlement settlement)
        {
            if (settlement.Town == null || !settlement.Town.IsOwnerUnassigned || settlement.OwnerClan == null
                || BannerKingsConfig.Instance.TitleManager == null) return;

            Kingdom kingdom = settlement.OwnerClan.Kingdom;
            if (kingdom == null) return;

            FeudalTitle sovereign = BannerKingsConfig.Instance.TitleManager.GetSovereignTitle(kingdom);
            if (sovereign == null || sovereign.contract == null) return;


            if (sovereign.contract.Rights.Contains(FeudalRights.Conquest_Rights))
            {
                List<KingdomDecision> decisions = kingdom.UnresolvedDecisions.ToList();
                KingdomDecision bkDecision = decisions.FirstOrDefault(x => x is BKSettlementClaimantDecision && (x as SettlementClaimantDecision).Settlement == settlement);
                if (bkDecision != null) return;
                
                KingdomDecision vanillaDecision = decisions.FirstOrDefault(x => x is SettlementClaimantDecision && !(x is BKSettlementClaimantDecision) && (x as SettlementClaimantDecision).Settlement == settlement);
                if (vanillaDecision != null)
                    kingdom.RemoveDecision(vanillaDecision);

                if (!conqueredByArmies.ContainsKey(settlement)) return;

                List<Clan> clans = conqueredByArmies[settlement].FindAll(x => x.Kingdom == kingdom && !x.IsUnderMercenaryService);
                if (clans.Count == 1) ChangeOwnerOfSettlementAction.ApplyByKingDecision(clans[0].Leader, settlement);
                else if (clans.Count == 0) kingdom.AddDecision(new KingSelectionKingdomDecision(kingdom.RulingClan), true);
                else
                {
                    kingdom.AddDecision(new BKSettlementClaimantDecision(kingdom.RulingClan, settlement, null, null, conqueredByArmies[settlement], true), true);
                    if (clans.Contains(Clan.PlayerClan) && !Clan.PlayerClan.IsUnderMercenaryService)
                    {
                        MobileParty party = clans[0].Leader.PartyBelongedTo;
                        Army army = null;
                        if (party != null)
                            army = party.Army;

                        if (army != null) GameTexts.SetVariable("ARMY", army.Name);
                        else GameTexts.SetVariable("ARMY", new TextObject("{=!}the conquering army"));
                        GameTexts.SetVariable("SETTLEMENT", settlement.Name);
                        InformationManager.ShowInquiry(new InquiryData(new TextObject("Conquest Right - Election").ToString(),
                            new TextObject("By contract law, you and the participants of {ARMY} will compete in election for the ownership of {SETTLEMENT}.").ToString(),
                            true, false, GameTexts.FindText("str_done").ToString(), null, null, null), true);
                    }
                }        
            } 
        }

        private void OnOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (BannerKingsConfig.Instance.TitleManager == null) return;

            if (conqueredByArmies.ContainsKey(settlement))
                conqueredByArmies.Remove(settlement);

            if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByBarter)
            {
                FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetTitle(settlement);
                if (oldOwner == title.deJure)
                {
                    BannerKingsConfig.Instance.TitleManager.InheritTitle(oldOwner, newOwner, title);
                    if (!settlement.IsVillage)
                        foreach (Village village in settlement.BoundVillages)
                        {
                            FeudalTitle villageTitle = BannerKingsConfig.Instance.TitleManager.GetTitle(village.Settlement);
                            if (villageTitle.deJure == oldOwner)
                                BannerKingsConfig.Instance.TitleManager.InheritTitle(oldOwner,
                                newOwner,
                                villageTitle);
                        }  
                }
                return;
            }

            if (settlement.Town != null && settlement.Town.IsOwnerUnassigned &&
                detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByLeaveFaction) return;

            BannerKingsConfig.Instance.TitleManager.ApplyOwnerChange(settlement, newOwner);
            Kingdom kingdom = newOwner.Clan.Kingdom;
            if (kingdom == null) return;

            FeudalTitle sovereign = BannerKingsConfig.Instance.TitleManager.GetSovereignTitle(kingdom);
            if (sovereign == null || sovereign.contract == null) return;


            if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
            {
                bool absoluteRightGranted = false;
                if (sovereign.contract.Rights.Contains(FeudalRights.Absolute_Land_Rights))
                {
                    FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetTitle(settlement);
                    if (title != null)
                    {
                        foreach (Clan clan in kingdom.Clans)
                            if (clan.Leader == title.deJure)
                            {
                                ChangeOwnerOfSettlementAction.ApplyByKingDecision(clan.Leader, settlement);
                                absoluteRightGranted = true;
                                if (clan.Leader == Hero.MainHero)
                                {
                                    GameTexts.SetVariable("SETTLEMENT", settlement.Name);
                                    InformationManager.ShowInquiry(new InquiryData(new TextObject("Absolute Land Right").ToString(),
                                        new TextObject("By contract law, you have been awarded the ownership of {SETTLEMENT} due to your legal right to this fief.").ToString(),
                                        true, false, GameTexts.FindText("str_done").ToString(), null, null, null), true);
                                }
                            }
                    }
                }

                if (!absoluteRightGranted && sovereign.contract.Rights.Contains(FeudalRights.Conquest_Rights))
                {
                    List<KingdomDecision> decisions = kingdom.UnresolvedDecisions.ToList();
                    KingdomDecision decision = decisions.FirstOrDefault(x => x is SettlementClaimantDecision && (x as SettlementClaimantDecision).Settlement == settlement);
                    if (decision != null)
                        kingdom.RemoveDecision(decision);

                    MobileParty party = settlement.LastAttackerParty;
                    Army army = party.Army;
                    if (army != null)
                    {
                        if (!conqueredByArmies.ContainsKey(settlement))
                            conqueredByArmies.Add(settlement, new List<Clan>());
                        else conqueredByArmies[settlement].Clear();

                        foreach (MobileParty clanParty in army.Parties)
                            if (!conqueredByArmies[settlement].Contains(clanParty.ActualClan) && !clanParty.ActualClan.IsUnderMercenaryService)
                                conqueredByArmies[settlement].Add(clanParty.ActualClan);

                        return;
                    }

                    ChangeOwnerOfSettlementAction.ApplyByKingDecision(capturerHero, settlement);
                    if (capturerHero == Hero.MainHero)
                    {
                        GameTexts.SetVariable("SETTLEMENT", settlement.Name);
                        InformationManager.ShowInquiry(new InquiryData(new TextObject("Conquest Right").ToString(),
                            new TextObject("By contract law, you have been awarded the ownership of {SETTLEMENT} due to you conquering it.").ToString(),
                            true, false, GameTexts.FindText("str_done").ToString(), null, null, null), true);
                    }
                }
            }
        }

        public void OnDailyTickClan(Clan clan)
        {
            if (BannerKingsConfig.Instance.TitleManager == null || clan.Kingdom == null || clan.IsUnderMercenaryService ||
                !clan.IsEliminated || clan.IsRebelClan || clan.IsBanditFaction) return;
            BannerKingsConfig.Instance.TitleManager.GiveLordshipOnKingdomJoin(clan.Kingdom, clan);
        }

        public void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            if (detail != ChangeKingdomActionDetail.JoinKingdom || BannerKingsConfig.Instance.TitleManager == null) return;
            BannerKingsConfig.Instance.TitleManager.GiveLordshipOnKingdomJoin(newKingdom, clan);
        }

        private void OnClanDestroyed(Clan clan) 
        {
            
            if (BannerKingsConfig.Instance.TitleManager == null) return;
            List<FeudalTitle> titles = BannerKingsConfig.Instance.TitleManager.GetAllDeJure(clan);
            if (titles.Count > 0)
            {
                foreach (FeudalTitle title in titles)
                {
                    if (BannerKingsConfig.Instance.TitleManager.HasSuzerain(title))
                    {
                        FeudalTitle suzerain = BannerKingsConfig.Instance.TitleManager.GetImmediateSuzerain(title);
                        if (suzerain.deJure.IsAlive && !clan.Heroes.Contains(suzerain.deJure))
                        {
                            BannerKingsConfig.Instance.TitleManager.InheritAllTitles(title.deJure, suzerain.deJure);
                            continue;
                        }
                            
                    } 
                    
                    if (title.sovereign != null)
                        if (title.sovereign.deJure != title.deJure && title.sovereign.deJure.IsAlive)
                        {
                            BannerKingsConfig.Instance.TitleManager.InheritAllTitles(title.deJure, title.sovereign.deJure); 
                            continue;
                        }    
                    
                    if (title.deJure != title.deFacto)
                    {
                        BannerKingsConfig.Instance.TitleManager.DeactivateDeJure(title);
                        continue;
                    }

                    BannerKingsConfig.Instance.TitleManager.DeactivateTitle(title);
                }  
            }
        } 
    }
}
