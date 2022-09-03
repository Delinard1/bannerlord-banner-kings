using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace BannerKings.Managers.Decisions
{
    public class BKEncourageMilitiaDecision : BannerKingsDecision
    {
        public BKEncourageMilitiaDecision(Settlement settlement, bool enabled) : base(settlement, enabled)
        {
        }

        public override string GetHint()
        {
            return new TextObject("{=xngDTGdf}Encourage able-bodied men to join the active militia force").ToString();
        }

        public override string GetIdentifier()
        {
            return "decision_militia_encourage";
        }

        public override string GetName()
        {
            return new TextObject("{=sOQrvQc9}Encourage militia").ToString();
        }
    }
}