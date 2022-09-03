using TaleWorlds.Localization;

namespace BannerKings.Managers.Institutions.Religions.Leaderships
{
    public class AutocephalousLeadership : DescentralizedLeadership
    {
        public override TextObject GetHint()
        {
            return new TextObject("{=voZOXw2s}Autocephalous religions are organized based on secular kingdoms. Each kingdom will have it's own head of faith, tying the spiritual power to the material sovereigns.");
        }

        public override TextObject GetName()
        {
            return new TextObject("{=H1L1fngv}Autocephalous");
        }
    }
}