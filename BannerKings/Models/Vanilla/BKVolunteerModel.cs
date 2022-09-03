using BannerKings.Managers.Court;
using BannerKings.Managers.Policies;
using BannerKings.Managers.Skills;
using BannerKings.Managers.Titles;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static BannerKings.Managers.PopulationManager;
using static BannerKings.Managers.Policies.BKDraftPolicy;
using BannerKings.Managers.Institutions.Religions.Doctrines;

namespace BannerKings.Models.Vanilla
{
    public class BKVolunteerModel : CalradiaExpandedKingdoms.Models.CEKVolunteerProductionModel
    {
        public override bool CanHaveRecruits(Hero hero)
        {
            var occupation = hero.Occupation;
            var valid = occupation == Occupation.Mercenary || occupation - Occupation.Artisan <= 5;
            if (valid)
            {
                var settlement = hero.CurrentSettlement;
                if (settlement != null && BannerKingsConfig.Instance.PopulationManager != null &&
                    BannerKingsConfig.Instance.PopulationManager.IsSettlementPopulated(settlement))
                {
                    var data = BannerKingsConfig.Instance.PopulationManager.GetPopData(settlement);
                    return data.MilitaryData.Manpower > 0;
                }

                return true;
            }

            return false;
        }

        public override CharacterObject GetBasicVolunteer(Hero sellerHero)
        {
            var settlement = sellerHero.CurrentSettlement;
            if (BannerKingsConfig.Instance.PopulationManager != null &&
                BannerKingsConfig.Instance.PopulationManager.IsSettlementPopulated(settlement))
            {
                var data = BannerKingsConfig.Instance.PopulationManager.GetPopData(settlement);
                var power = sellerHero.Power;
                var chance = settlement.IsTown ? power * 0.03f : power * 0.05f;
                var random = MBRandom.RandomFloatRanged(1f, 100f);

                var ownerEducation = BannerKingsConfig.Instance.EducationManager.GetHeroEducation(settlement.OwnerClan.Leader);
                if (ownerEducation.HasPerk(BKPerks.Instance.RitterPettySuzerain))
                {
                    chance *= 1.2f;
                }

                if (data.MilitaryData.NobleManpower > 0)
                {

                    if (sellerHero.IsPreacher && data.ReligionData != null)
                    {
                        var religion = data.ReligionData.DominantReligion;
                        if (religion != null && religion.HasDoctrine(DefaultDoctrines.Instance.Druidism))
                        {
                            return sellerHero.Culture.EliteBasicTroop;
                        }
                    }

                    if (chance >= random)
                    {
                        return sellerHero.Culture.EliteBasicTroop;
                    }
                }

                return sellerHero.Culture.BasicTroop;
            }

            return base.GetBasicVolunteer(sellerHero);
        }

        public ExplainedNumber GetDraftEfficiency(Hero hero, int index, Settlement settlement)
        {
            if (hero == null)
            {
                return new ExplainedNumber(0f);
            }

            var num = 0.7f;
            var num2 = 0;
            foreach (var town in hero.CurrentSettlement.MapFaction.Fiefs)
            {
                num2 += town.IsTown
                    ? (town.Settlement.Prosperity < 3000f ? 1 : town.Settlement.Prosperity < 6000f ? 2 : 3) +
                      town.Villages.Count
                    : town.Villages.Count;
            }

            var num3 = num2 < 46 ? num2 / 46f * (num2 / 46f) : 1f;
            num += hero.CurrentSettlement != null && num3 < 1f ? (1f - num3) * 0.2f : 0f;
            var baseNumber = 0.75f * MathF.Clamp(MathF.Pow(num, index + 1), 0f, 1f);
            var explainedNumber = new ExplainedNumber(baseNumber, true);
            var clan = hero.Clan;
            if (clan?.Kingdom != null && hero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Cantons))
            {
                explainedNumber.AddFactor(0.2f, new TextObject("{=zGx6c77M}Cantons kingdom policy"));
            }

            if (hero.VolunteerTypes?[index] != null && hero.VolunteerTypes[index].IsMounted && PerkHelper.GetPerkValueForTown(DefaultPerks.Riding.CavalryTactics, settlement.IsVillage ? settlement.Village.Bound.Town : settlement.Town))
            {
                explainedNumber.AddFactor(DefaultPerks.Riding.CavalryTactics.PrimaryBonus * 0.01f, DefaultPerks.Riding.CavalryTactics.PrimaryDescription);
            }

            BannerKingsConfig.Instance.CourtManager.ApplyCouncilEffect(ref explainedNumber, settlement.OwnerClan.Leader, CouncilPosition.Marshall, 0.25f, true);
            
            var draftPolicy = ((BKDraftPolicy) BannerKingsConfig.Instance.PolicyManager.GetPolicy(settlement, "draft")).Policy;
            switch (draftPolicy)
            {
                case DraftPolicy.Conscription:
                    explainedNumber.Add(0.15f, new TextObject("{=2z4YQTuu}Draft policy"));
                    break;
                case DraftPolicy.Demobilization:
                    explainedNumber.Add(-0.15f, new TextObject("{=2z4YQTuu}Draft policy"));
                    break;
            }

            var government = BannerKingsConfig.Instance.TitleManager.GetSettlementGovernment(settlement);
            if (government == GovernmentType.Tribal)
            {
                explainedNumber.AddFactor(0.2f, new TextObject("{=PSrEtF5L}Government"));
            }

            var lordshipMilitaryAdministration = BKPerks.Instance.LordshipMilitaryAdministration;
            if (settlement.Owner.GetPerkValue(lordshipMilitaryAdministration))
            {
                explainedNumber.AddFactor(0.2f, lordshipMilitaryAdministration.Name);
            }

            var religionData = BannerKingsConfig.Instance.PopulationManager.GetPopData(settlement).ReligionData;
            if (religionData != null)
            {
                var religion = religionData.DominantReligion;
                if (religion != null && religion.HasDoctrine(DefaultDoctrines.Instance.Pastoralism))
                {
                    explainedNumber.Add(-0.2f, DefaultDoctrines.Instance.Pastoralism.Name);
                }
            }

            return explainedNumber;

        }

        public ExplainedNumber GetMilitarism(Settlement settlement)
        {
            var explainedNumber = new ExplainedNumber(0f, true);
            var serfMilitarism = GetClassMilitarism(PopType.Serfs);
            var craftsmenMilitarism = GetClassMilitarism(PopType.Craftsmen);
            var nobleMilitarism = GetClassMilitarism(PopType.Nobles);

            explainedNumber.Add((serfMilitarism + craftsmenMilitarism + nobleMilitarism) / 3f, new TextObject("{=AaNeOd9n}Base"));

            BannerKingsConfig.Instance.CourtManager.ApplyCouncilEffect(ref explainedNumber, settlement.OwnerClan.Leader, CouncilPosition.Marshall, 0.03f, false);

            if (settlement.Culture == settlement.Owner.Culture)
            {
                var lordshipTraditionalistPerk = BKPerks.Instance.LordshipTraditionalist;
                if (settlement.Owner.GetPerkValue(BKPerks.Instance.LordshipTraditionalist))
                {
                    explainedNumber.AddFactor(0.01f, lordshipTraditionalistPerk.Name);
                }

                var lordshipMilitaryAdministration = BKPerks.Instance.LordshipMilitaryAdministration;
                if (settlement.Owner.GetPerkValue(lordshipMilitaryAdministration))
                {
                    explainedNumber.AddFactor(0.02f, lordshipMilitaryAdministration.Name);
                }
            }

            return explainedNumber;
        }

        public float GetClassMilitarism(PopType type)
        {
            return type switch
            {
                PopType.Serfs => 0.1f,
                PopType.Craftsmen => 0.03f,
                PopType.Nobles => 0.12f,
                _ => 0
            };
        }
    }
}