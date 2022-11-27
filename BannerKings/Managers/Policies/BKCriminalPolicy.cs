﻿using System;
using System.Collections.Generic;
using BannerKings.UI.Items;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.SaveSystem;

namespace BannerKings.Managers.Policies
{
    internal class BKCriminalPolicy : BannerKingsPolicy
    {
        public enum CriminalPolicy
        {
            Enslavement,
            Execution,
            Forgiveness
        }

        public BKCriminalPolicy(CriminalPolicy policy, Settlement settlement) : base(settlement, (int) policy)
        {
            Policy = policy;
        }

        [SaveableProperty(3)] public CriminalPolicy Policy { get; private set; }

        public override string GetHint(int value)
        {
            return value switch
            {
                (int) CriminalPolicy.Enslavement => "Prisoners sold in the settlement will be enslaved and join the population. No particular repercussions.",
                (int) CriminalPolicy.Execution => "Prisoners will suffer the death penalty. No ransom is paid (to non-lord prisoners), but the populace supports this action - if they share your culture. If not, the opposite applies.",
                _ => "Forgive prisoners of war. No ransom is paid (to non-lord prisoners), and soldiers rejoin the population as serfs in a settlement of their culture. The populace supports this, if they do not share your culture. The opposite applies."
            };
        }

        public override void OnChange(SelectorVM<BKItemVM> obj)
        {
            if (obj.SelectedItem == null)
            {
                return;
            }

            var vm = obj.GetCurrentItem();
            Policy = (CriminalPolicy) vm.Value;
            Selected = vm.Value;
            BannerKingsConfig.Instance.PolicyManager.UpdateSettlementPolicy(Settlement, this);
        }

        public override IEnumerable<Enum> GetPolicies()
        {
            yield return CriminalPolicy.Enslavement;
            yield return CriminalPolicy.Execution;
            yield return CriminalPolicy.Forgiveness;
        }

        public override string GetIdentifier()
        {
            return "criminal";
        }
    }
}