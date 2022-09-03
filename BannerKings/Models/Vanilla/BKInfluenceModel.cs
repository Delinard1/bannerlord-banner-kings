using System;
using BannerKings.Behaviours;
using BannerKings.Managers.CampaignStart;
using BannerKings.Managers.Education.Lifestyles;
using BannerKings.Managers.Institutions.Religions;
using BannerKings.Managers.Institutions.Religions.Doctrines;
using BannerKings.Managers.Populations;
using BannerKings.Managers.Populations.Villages;
using BannerKings.Managers.Skills;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static BannerKings.Managers.PopulationManager;

namespace BannerKings.Models.Vanilla
{
    public class BKInfluenceModel : DefaultClanPoliticsModel
    {
        public float GetRejectKnighthoodCost(Clan clan)
        {
            return 10f + MathF.Max(CalculateInfluenceChange(clan).ResultNumber, 5f) * 0.025f * CampaignTime.DaysInYear;
        }

        public override ExplainedNumber CalculateInfluenceChange(Clan clan, bool includeDescriptions = false)
        {
            var baseResult = base.CalculateInfluenceChange(clan, includeDescriptions);

            if (clan == Clan.PlayerClan && Campaign.Current.GetCampaignBehavior<BKCampaignStartBehavior>().HasDebuff(DefaultStartOptions.Instance.IndebtedLord))
            {
                baseResult.Add(-5f, DefaultStartOptions.Instance.IndebtedLord.Name);
            }

            var generalSupport = 0f;
            var generalAutonomy = 0f;
            float i = 0;

            var education = BannerKingsConfig.Instance.EducationManager.GetHeroEducation(clan.Leader);
            if (clan.IsUnderMercenaryService && clan.Leader != null)
            {
                var mercenaryChange = MathF.Ceiling(clan.Influence * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction()));
                if (mercenaryChange != 0)
                {
                    if (education.Lifestyle != null && education.Lifestyle.Equals(DefaultLifestyles.Instance.Mercenary))
                    {
                        baseResult.Add((float)(mercenaryChange * 0.2f), new TextObject("{=cCQO7noU}{LIFESTYLE} lifestyle")
                                        .SetTextVariable("LIFESTYLE", DefaultLifestyles.Instance.Mercenary.Name));
                    }

                    if (education.HasPerk(BKPerks.Instance.VaryagRecognizedMercenary))
                    {
                        baseResult.Add((float)(mercenaryChange * 0.1f), BKPerks.Instance.VaryagRecognizedMercenary.Name);
                    }
                }
            }

            if (BannerKingsConfig.Instance.ReligionsManager.HasBlessing(clan.Leader, DefaultDivinities.Instance.DarusosianSecondary1))
            {
                baseResult.Add(2f, DefaultDivinities.Instance.DarusosianSecondary1.Name);
            }
            else if (BannerKingsConfig.Instance.ReligionsManager.HasBlessing(clan.Leader, DefaultDivinities.Instance.VlandiaSecondary1))
            {
                baseResult.Add(2f, DefaultDivinities.Instance.VlandiaSecondary1.Name);
            }


            if (education.HasPerk(BKPerks.Instance.OutlawPlunderer))
            {
                float bandits = 0;
                if (clan.Leader.PartyBelongedTo != null && !clan.Leader.IsPrisoner)
                {
                    foreach (var element in clan.Leader.PartyBelongedTo.MemberRoster.GetTroopRoster())
                    {
                        if (element.Character.Occupation == Occupation.Bandit)
                        {
                            bandits += element.Number;
                        }
                    }
                }

                baseResult.Add(bandits * 0.1f, BKPerks.Instance.OutlawPlunderer.Name);
            }

            var council = BannerKingsConfig.Instance.CourtManager.GetCouncil(clan);
            var religion = BannerKingsConfig.Instance.ReligionsManager.GetHeroReligion(clan.Leader);
            if (religion != null && clan.Settlements.Count > 0)
            {
                if (religion.HasDoctrine(DefaultDoctrines.Instance.Druidism) && 
                    council.GetMemberFromPosition(Managers.Court.CouncilPosition.Spiritual).Member == null) 
                {
                    baseResult.Add(-5f, DefaultDoctrines.Instance.Druidism.Name);
                }
            }

            foreach (var settlement in clan.Settlements)
            {
                if (BannerKingsConfig.Instance.PopulationManager == null || !BannerKingsConfig.Instance.PopulationManager.IsSettlementPopulated(settlement))
                {
                    continue;
                }

                var data = BannerKingsConfig.Instance.PopulationManager.GetPopData(settlement);
                if (BannerKingsConfig.Instance.AI.AcceptNotableAid(clan, data))
                {
                    foreach (var notable in data.Settlement.Notables)
                    {
                        if (notable.SupporterOf == clan && notable.Gold > 5000)
                        {
                            baseResult.Add(-1f,
                                new TextObject("{=WDHTvasY}Aid from {NOTABLE}").SetTextVariable("NOTABLE", notable.Name));
                        }
                    }
                }

                generalSupport += data.NotableSupport.ResultNumber - 0.5f;
                generalAutonomy += -0.5f * data.Autonomy;
                i++;

                var settlementResult = CalculateSettlementInfluence(settlement, data);
                baseResult.Add(settlementResult.ResultNumber, settlement.Name);
                if (!settlement.IsVillage || BannerKingsConfig.Instance.TitleManager == null)
                {
                    continue;
                }

                var title = BannerKingsConfig.Instance.TitleManager.GetTitle(settlement);
                if (title.deJure == null)
                {
                    continue;
                }

                var deJureClan = title.deJure.Clan;
                if (title.deJure != deJureClan.Leader && settlement.OwnerClan == deJureClan)
                {
                    BannerKingsConfig.Instance.TitleManager.AddKnightInfluence(title.deJure, settlementResult.ResultNumber * 0.1f);
                }
            }

            var position = BannerKingsConfig.Instance.CourtManager.GetHeroPosition(clan.Leader);
            if (position != null)
            {
                baseResult.Add(position.IsCorePosition(position.Position) ? 1f : 0.5f, new TextObject("{=WvhXhUFS}Councillor role"));
            }
            

            if (i > 0)
            {
                var finalSupport = MBMath.ClampFloat(generalSupport / i, -0.5f, 0.5f);
                var finalAutonomy = MBMath.ClampFloat(generalAutonomy / i, -0.5f, 0f);
                if (finalSupport != 0f)
                {
                    baseResult.AddFactor(finalSupport, new TextObject("{=RkKAd2Yp}Overall notable support"));
                }

                if (finalAutonomy != 0f)
                {
                    baseResult.AddFactor(finalAutonomy, new TextObject("{=qJbYtZjH}Overall settlement autonomy"));
                }
            }


            return baseResult;
        }

        public ExplainedNumber CalculateSettlementInfluence(Settlement settlement, PopulationData data)
        {
            var settlementResult = new ExplainedNumber(0f, true);
            float nobles = data.GetTypeCount(PopType.Nobles);
            settlementResult.Add(MBMath.ClampFloat(nobles * 0.01f, 0f, 12f), new TextObject($"{{=!}}Nobles influence from {settlement.Name}"));

            var villageData = data.VillageData;
            if (villageData != null)
            {
                float manor = villageData.GetBuildingLevel(DefaultVillageBuildings.Instance.Manor);
                if (manor > 0)
                {
                    settlementResult.AddFactor(Math.Abs(manor - 3) < 0.1f ? 0.5f : manor * 0.15f, new TextObject("{=UHyznyEy}Manor"));
                }
            }

            var lordshipManorLord = BKPerks.Instance.LordshipManorLord;
            if (settlement.Owner.GetPerkValue(lordshipManorLord))
            {
                settlementResult.Add(0.2f, lordshipManorLord.Name);
            }

            if (!BannerKingsConfig.Instance.PopulationManager.PopSurplusExists(settlement, PopType.Nobles, true))
            {
                return settlementResult;
            }

            var result = settlementResult.ResultNumber;
            float extra = BannerKingsConfig.Instance.PopulationManager.GetPopCountOverLimit(settlement, PopType.Nobles);
            settlementResult.Add(MBMath.ClampFloat(extra * -0.01f, result * -0.5f, -0.1f), new TextObject($"{{=!}}Excess noble population at {settlement.Name}"));

            return settlementResult;
        }
    }
}