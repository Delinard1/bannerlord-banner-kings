﻿using BannerKings.Managers.Policies;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using static BannerKings.Managers.Policies.BKGarrisonPolicy;

namespace BannerKings.Models.Vanilla
{
    public class BKGarrisonXpModel : DefaultDailyTroopXpBonusModel
    {
        public override float CalculateGarrisonXpBonusMultiplier(Town town)
        {
            var baseResult = base.CalculateGarrisonXpBonusMultiplier(town);
            if (BannerKingsConfig.Instance.PopulationManager != null &&
                BannerKingsConfig.Instance.PopulationManager.IsSettlementPopulated(town.Settlement))
            {
                var garrison =
                    ((BKGarrisonPolicy) BannerKingsConfig.Instance.PolicyManager.GetPolicy(town.Settlement, "garrison"))
                    .Policy;
                switch (garrison)
                {
                    case GarrisonPolicy.Dischargement:
                        baseResult *= 0.7f;
                        break;
                    case GarrisonPolicy.Enlistment:
                        baseResult *= 1.3f;
                        break;
                }
            }

            return baseResult;
        }
    }
}