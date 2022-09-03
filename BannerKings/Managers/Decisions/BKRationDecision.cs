using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace BannerKings.Managers.Decisions
{
    public class BKRationDecision : BannerKingsDecision
    {
        public BKRationDecision(Settlement settlement, bool enabled) : base(settlement, enabled)
        {
        }

        public override string GetHint()
        {
            return new TextObject("{=Bp5YdLju}Food consumption reduced through enforced rationing. Decreases loyalty, with double effect if settlement is not besieged. Increases adm. costs.").ToString();
        }

        public override string GetIdentifier()
        {
            return "decision_ration";
        }

        public override string GetName()
        {
            return new TextObject("{=8OmoZNnS}Enforce food rationing").ToString();
        }
    }
}