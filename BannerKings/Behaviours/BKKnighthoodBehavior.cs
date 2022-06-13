﻿using TaleWorlds.CampaignSystem;
using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Actions;
using HarmonyLib;
using TaleWorlds.Localization;
using System.Reflection;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using SandBox;
using System.Linq;
using BannerKings.Managers.Titles;

namespace BannerKings.Behaviors
{
    public class BKKnighthoodBehavior : CampaignBehaviorBase
    {

        private FeudalTitle titleGiven = null;
        List<InquiryElement> lordshipsToGive = new List<InquiryElement>();
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(OnHeroDailyTick));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnHeroDailyTick(Hero hero)
        {
            if (hero.Clan == null || hero == hero.Clan.Leader || hero.Occupation != Occupation.Lord || 
                BannerKingsConfig.Instance.TitleManager == null) return;

            FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(hero);
            if (title == null ||  title.type != TitleType.Lordship || title.fief.Village == null || !CanCreateClan(hero)) return;

            Clan originalClan = hero.Clan;
            Settlement settlement = title.fief.Village.TradeBound;
            TextObject name = GetRandomName(hero.Culture, settlement);
            if (name == null || Clan.All.Any((Clan t) => t.Name.Equals(name)) || Clan.PlayerClan.Name.Equals(name)) return;
            Clan clan = Clan.CreateClan(hero.StringId + "_knight_clan");
            clan.InitializeClan(name, name, hero.Culture, Banner.CreateOneColoredBannerWithOneIcon(settlement.MapFaction.Banner.GetFirstIconColor(), settlement.MapFaction.Banner.GetPrimaryColor(), 
                hero.Culture.PossibleClanBannerIconsIDs.GetRandomElement()), settlement.GatePosition, false);
            clan.Kingdom = originalClan.Kingdom;
            clan.AddRenown(150f);
            hero.Clan = null;
            hero.CompanionOf = null;
            hero.Clan = clan;
            clan.SetLeader(hero);
            hero.AddPower(-hero.Power);
            InformationManager.AddQuickInformation(new TextObject("{=!}The {NEW} has been formed by {HERO}, previously a knight of {ORIGINAL}.")
                            .SetTextVariable("NEW", clan.Name)
                            .SetTextVariable("HERO", hero.Name)
                            .SetTextVariable("ORIGINAL", originalClan.Name));
        }

        private TextObject GetRandomName(CultureObject culture, Settlement settlement)
        {
            TextObject random = null;
            if (culture.ClanNameList.Count > 1)
                random = culture.ClanNameList.GetRandomElementInefficiently();
            else
            {
                random = culture.ClanNameList[0];
                random.SetTextVariable("ORIGIN_SETTLEMENT", settlement.Name);
            }
            return random;
        }
        private bool CanCreateClan(Hero hero) => hero.Gold >= 30000 && hero.Power >= 250f && hero.Occupation == Occupation.Lord &&
            hero.Father != hero.Clan.Leader && hero.Mother != hero.Clan.Leader && !hero.Children.Contains(hero.Clan.Leader) && 
            !hero.Siblings.Contains(hero.Clan.Leader);

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddDialog(campaignGameStarter);
        }

        private void AddDialog(CampaignGameStarter starter)
        {
            StringBuilder knighthoodSb = new StringBuilder();
            knighthoodSb.Append("By knighting, you are granting this person nobility and they will be bound to you as your vassal by the standard contract of the kingdom. A lordship must be given away to seal the contract.");
            knighthoodSb.Append(Environment.NewLine);
            knighthoodSb.Append(" ");
            knighthoodSb.Append(Environment.NewLine);
            knighthoodSb.Append("Their lands and titles henceforth can not be revoked without lawful cause, and any fief revenue will be theirs, taxed or not by you as per contract");
            knighthoodSb.Append(Environment.NewLine);
            knighthoodSb.Append(" ");
            knighthoodSb.Append(Environment.NewLine);
            knighthoodSb.Append("As a knight, they are capable of raising a personal retinue and are obliged to fulfill their duties.");

            starter.AddPlayerLine("companion_grant_knighthood", "companion_role", "companion_knighthood_question", "Would you like to serve me as my knight?", 
                new ConversationSentence.OnConditionDelegate(this.GrantKnighthoodOnCondition), delegate {
                    InformationManager.ShowInquiry(new InquiryData("Bestowing Knighthood", knighthoodSb.ToString(), true, false, "Understood", null, null, null), false);
                }, 100, new ConversationSentence.OnClickableConditionDelegate(GrantKnighthoodOnClickable), null);

            starter.AddDialogLine("companion_grant_knighthood_response", "companion_knighthood_question", "companion_knighthood_response",
                "My lord, I would be honored.", null, null, 100, null); 

            starter.AddPlayerLine("companion_grant_knighthood_response_confirm", "companion_knighthood_response", "companion_knighthood_accepted", "Let us decide your fief.",
                new ConversationSentence.OnConditionDelegate(this.CompanionKnighthoodAcceptedCondition), new ConversationSentence.OnConsequenceDelegate(this.GrantKnighthoodOnConsequence), 100, null, null);

            starter.AddPlayerLine("companion_grant_knighthood_response_cancel", "companion_knighthood_response", "companion_role_pretalk", "Actualy, I would like to discuss this at a later time.",
               null, null, 100, null, null);

            starter.AddPlayerLine("companion_grant_knighthood_granted", "companion_knighthood_accepted", "close_window", "It is decided then. I bestow upon you the title of Knight.",
                null, null, 100, null, null);
        }

        private bool GrantKnighthoodOnCondition()
        {
            if (BannerKingsConfig.Instance.TitleManager == null) return false;
            Hero companion = Hero.OneToOneConversationHero;
            return companion != null && companion.Clan == Clan.PlayerClan && BannerKingsConfig.Instance.TitleManager.Knighthood;
        }

        private bool CompanionKnighthoodAcceptedCondition()
        {
            lordshipsToGive.Clear();
            List<FeudalTitle> titles = BannerKingsConfig.Instance.TitleManager.GetAllDeJure(Hero.MainHero);
            foreach (FeudalTitle title in titles)
            {
                if (title.type != TitleType.Lordship || title.fief == null || title.deJure != Hero.MainHero) continue;
                lordshipsToGive.Add(new InquiryElement(title, title.FullName.ToString(), new ImageIdentifier()));
            }

            if (lordshipsToGive.Count == 0)
                InformationManager.DisplayMessage(new InformationMessage("You currently do not lawfully own a lordship that could be given away."));

            return lordshipsToGive.Count >= 1;
        }

        public bool GrantKnighthoodOnClickable(out TextObject hintText)
        {
            Hero companion = Hero.OneToOneConversationHero;
            FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(Hero.MainHero);

            int tier = Clan.PlayerClan.Tier;
            if (tier < 2)
            {
                hintText = new TextObject("{=!}Your Clan Tier needs to be at least {TIER}.", null);
                hintText.SetTextVariable("TIER", 2);
                return false;
            }

            Kingdom kingdom = Clan.PlayerClan.Kingdom;
            if (kingdom == null)
            {
                hintText = new TextObject("{=!}Before bestowing knighthood, you need to be formally part of a kingdom.", null);
                return false;
            }

            List<FeudalTitle> titles = BannerKingsConfig.Instance.TitleManager.GetAllDeJure(Hero.MainHero);
            if (titles.Count == 0)
            {
                hintText = new TextObject("{=!}You do not legally own any title.", null);
                return false;
            }

            List<FeudalTitle> lordships = titles.FindAll(x => x.type == TitleType.Lordship);
            if (lordships.Count == 0)
            {
                hintText = new TextObject("{=!}You do not legally own any lordship that could be given to land a new vassal.", null);
                return false;
            }

            if (Clan.PlayerClan.Influence < 150)
            {
                hintText = new TextObject("{=!}Bestowing knighthood requires {INFLUENCE} influence to legitimize your new vassal.", null);
                hintText.SetTextVariable("INFLUENCE", 150);
                return false;
            }

            hintText = new TextObject("{=!}Bestowing knighthood requires {GOLD} gold to give your vassal financial security.", null);
            hintText.SetTextVariable("GOLD", 5000);

            return Hero.MainHero.Gold >= 5000;
        }

        private void GrantKnighthoodOnConsequence()
        {
            InformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                    "Select the fief you would like to give away", string.Empty, lordshipsToGive, true, 1, 
                    GameTexts.FindText("str_done", null).ToString(), "", new Action<List<InquiryElement>>(this.OnNewPartySelectionOver), 
                    new Action<List<InquiryElement>>(this.OnNewPartySelectionOver), ""), false);
        }

        private void OnNewPartySelectionOver(List<InquiryElement> element)
        {
            if (element.Count == 0) return;

            Hero knight = Hero.OneToOneConversationHero;
            titleGiven = (FeudalTitle)element[0].Identifier;
            BannerKingsConfig.Instance.TitleManager.GrantLordship(this.titleGiven, Hero.MainHero, knight);
            GainKingdomInfluenceAction.ApplyForDefault(Hero.MainHero, -150f);
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, knight, 5000);
            knight.SetNewOccupation(Occupation.Lord);
            knight.Clan = null;
            knight.Clan = Clan.PlayerClan;
        }
    }

    namespace Patches
    {

        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "FindSuitableCompanionsToLeadCaravan")]
        class SuitableCaravanLeaderPatch
        {
            static bool Prefix(ref List<CharacterObject> __result)
            {
                if (BannerKingsConfig.Instance.TitleManager == null) return true;
                List<CharacterObject> list = new List<CharacterObject>();
                foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
                {
                    Hero heroObject = troopRosterElement.Character.HeroObject;
                    if (heroObject != null && heroObject != Hero.MainHero && heroObject.Clan == Clan.PlayerClan && heroObject.GovernorOf == null && heroObject.CanLeadParty())
                    {
                        FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(heroObject);
                        if (title == null) list.Add(troopRosterElement.Character);
                    }
                }
                __result = list;
                return false;
            }
        }

        [HarmonyPatch(typeof(Hero), "SetHeroEncyclopediaTextAndLinks")]
        class HeroDescriptionPatch
        {
            static void Postfix(ref string __result, Hero o)
            {
                List<FeudalTitle> titles = BannerKingsConfig.Instance.TitleManager.GetAllDeJure(o);
                if (titles != null && titles.Count > 0)
                {
                    string desc = "";
                    FeudalTitle current = null;
                    List<FeudalTitle> finalList = titles.OrderBy(x => (int)x.type).ToList();
                    GovernmentType government = GovernmentType.Feudal;
                    foreach (FeudalTitle title in finalList)
                    {
                        if (title.contract != null) government = title.contract.Government;
                        if (current == null)
                            desc += string.Format("{0} of {1}", Helpers.Helpers.GetTitleHonorary(title.type, government, false), title.shortName);
                        else if (current.type == title.type)
                            desc += ", " + title.shortName;
                        else if (current.type != title.type)
                            desc += string.Format(" and {0} of {1}", Helpers.Helpers.GetTitleHonorary(title.type, government, false), title.shortName);
                        current = title;
                    }
                    __result = __result + Environment.NewLine + desc;
                }
            }
        }

        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_lord_give_oath_go_on_condition")]
        class ShowContractPatch
        {
            static void Postfix()
            {
                if (BannerKingsConfig.Instance.TitleManager != null)
                {
                    Hero lord = null;
                    PartyBase party = PlayerEncounter.EncounteredParty;
                    if (party != null) lord = party.LeaderHero;
                    if (lord == null) lord = Hero.OneToOneConversationHero;
                    BannerKingsConfig.Instance.TitleManager.ShowContract(lord, "I Accept");
                }
            }
        }

        [HarmonyPatch(typeof(ClanPartiesVM), "ExecuteCreateNewParty")]
        class ClanCreatePartyPatch
        {
            static bool Prefix(ClanPartiesVM __instance, Clan ____faction, Func<Hero, Settlement> ____getSettlementOfGovernor)
            {
                if (!BannerKingsConfig.Instance.TitleManager.Knighthood) return true;

                if (__instance.CanCreateNewParty)
                {
                    List<InquiryElement> list = new List<InquiryElement>();
                    foreach (Hero hero in (from h in ____faction.Heroes
                                           where !h.IsDisabled
                                           select h).Union(____faction.Companions))
                    {
                        if ((hero.IsActive || hero.IsReleased || hero.IsFugitive) && !hero.IsChild && hero != Hero.MainHero && hero.CanLeadParty())
                        {
                            bool isEnabled = false;
                            MethodInfo hintMethod = __instance.GetType().GetMethod("GetPartyLeaderAssignmentSkillsHint", BindingFlags.NonPublic | BindingFlags.Instance);
                            string hint = (string)hintMethod.Invoke(__instance, new object[] { hero });
                            if (hero.PartyBelongedToAsPrisoner != null)
                                hint = new TextObject("{=vOojEcIf}You cannot assign a prisoner member as a new party leader", null).ToString();
                            else if (hero.IsReleased)
                                hint = new TextObject("{=OhNYkblK}This hero has just escaped from captors and will be available after some time.", null).ToString();
                            else if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero == hero)
                                hint = new TextObject("{=aFYwbosi}This hero is already leading a party.", null).ToString();
                            else if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.LeaderHero != Hero.MainHero)
                                hint = new TextObject("{=FjJi1DJb}This hero is already a part of an another party.", null).ToString();
                            else if (____getSettlementOfGovernor(hero) != null)
                                hint = new TextObject("{=Hz8XO8wk}Governors cannot lead a mobile party and be a governor at the same time.", null).ToString();
                            else if (hero.HeroState == Hero.CharacterStates.Disabled)
                                hint = new TextObject("{=slzfQzl3}This hero is lost", null).ToString();
                            else if (hero.HeroState == Hero.CharacterStates.Fugitive)
                                hint = new TextObject("{=dD3kRDHi}This hero is a fugitive and running from their captors. They will be available after some time.", null).ToString();
                            else if (!BannerKingsConfig.Instance.TitleManager.IsHeroKnighted(hero))
                                hint = new TextObject("A hero must be knighted and granted land before being able to raise a personal retinue. You may bestow knighthood by talking to them.", null).ToString();
                            else isEnabled = true;

                            list.Add(new InquiryElement(hero, hero.Name.ToString(), new ImageIdentifier(CampaignUIHelper.GetCharacterCode(hero.CharacterObject, false)), isEnabled, hint));
                        }
                    }
                    if (list.Count > 0)
                    {
                        MethodInfo method = __instance.GetType().GetMethod("OnNewPartySelectionOver", BindingFlags.NonPublic | BindingFlags.Instance);
                        InformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=0Q4Xo2BQ}Select the Leader of the New Party", null).ToString(),
                            string.Empty, list, true, 1, GameTexts.FindText("str_done", null).ToString(), "", new Action<List<InquiryElement>>(delegate (List<InquiryElement> x) { method.Invoke(__instance, new object[] { x }); }),
                            new Action<List<InquiryElement>>(delegate (List<InquiryElement> x) { method.Invoke(__instance, new object[] { x }); }), ""), false);
                    }
                    else InformationManager.AddQuickInformation(new TextObject("{=qZvNIVGV}There is no one available in your clan who can lead a party right now.", null), 0, null, "");
                }

                return false;
            }
        }

        /*
        [HarmonyPatch(typeof(Settlement))]
        [HarmonyPatch("Owner", MethodType.Getter)]
        class VillageOwnerPatch
        {
            static void Postfix(Settlement __instance, ref Hero __result)
            {
                if (__instance.IsVillage && BannerKingsConfig.Instance.TitleManager != null)
                {
                    FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetTitle(__instance);
                    if (title != null)
                        __result = title.deFacto;
                }
            }
        } */
    }
}
