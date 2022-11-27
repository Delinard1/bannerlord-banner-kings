﻿using System;
using System.Collections.Generic;
using BannerKings.UI.Items;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.SaveSystem;

namespace BannerKings.Managers.Policies
{
    internal class BKGarrisonPolicy : BannerKingsPolicy
    {
        public enum GarrisonPolicy
        {
            Standard,
            Enlistment,
            Dischargement
        }

        public BKGarrisonPolicy(GarrisonPolicy policy, Settlement settlement) : base(settlement, (int) policy)
        {
            Policy = policy;
        }

        [SaveableProperty(3)] public GarrisonPolicy Policy { get; private set; }

        public override string GetIdentifier()
        {
            return "garrison";
        }

        public override string GetHint(int value)
        {
            return value switch
            {
                (int) GarrisonPolicy.Dischargement =>
                    "Discharge a garrison member on a daily basis from duty. Slows down garrison trainning.",
                (int) GarrisonPolicy.Enlistment =>
                    "Increase the quantity of auto recruited garrison soldiers, as well as provide more trainning. Increases adm. costs.",
                _ => "Standard garrison policy, no particular effect."
            };
        }

        public override void OnChange(SelectorVM<BKItemVM> obj)
        {
            if (obj.SelectedItem != null)
            {
                var vm = obj.GetCurrentItem();
                Policy = (GarrisonPolicy) vm.Value;
                Selected = vm.Value;
                BannerKingsConfig.Instance.PolicyManager.UpdateSettlementPolicy(Settlement, this);
            }
        }

        public override IEnumerable<Enum> GetPolicies()
        {
            yield return GarrisonPolicy.Standard;
            yield return GarrisonPolicy.Enlistment;
            yield return GarrisonPolicy.Dischargement;
        }
    }
}