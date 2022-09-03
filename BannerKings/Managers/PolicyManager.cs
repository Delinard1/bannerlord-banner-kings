﻿using System.Collections.Generic;
using System.Linq;
using BannerKings.Managers.Decisions;
using BannerKings.Managers.Policies;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;
using static BannerKings.Managers.Policies.BKGarrisonPolicy;
using static BannerKings.Managers.Policies.BKMilitiaPolicy;
using static BannerKings.Managers.Policies.BKCriminalPolicy;

namespace BannerKings.Managers
{
    public class PolicyManager
    {
        public PolicyManager(Dictionary<Settlement, List<BannerKingsDecision>> DECISIONS,
            Dictionary<Settlement, List<BannerKingsPolicy>> POLICIES)
        {
            SettlementDecisions = DECISIONS;
            SettlementPolicies = POLICIES;
        }

        [SaveableProperty(1)] private Dictionary<Settlement, List<BannerKingsDecision>> SettlementDecisions { get; set; }

        [SaveableProperty(2)] private Dictionary<Settlement, List<BannerKingsPolicy>> SettlementPolicies { get; set; }

        private IEnumerable<string> TownDecisions
        {
            get
            {
                yield return "decision_ration";
                yield return "decision_militia_encourage";
                yield return "decision_slaves_export";
                yield return "decision_militia_subsidize";
                yield return "decision_tariff_exempt";
                yield return "decision_slaves_tax";
                yield return "decision_mercantilism";
            }
        }

        private IEnumerable<string> CastleDecisions
        {
            get
            {
                yield return "decision_ration";
                yield return "decision_militia_encourage";
                yield return "decision_slaves_export";
                yield return "decision_militia_subsidize";
                yield return "decision_tariff_exempt";
                yield return "decision_slaves_tax";
                yield return "decision_mercantilism";
            }
        }

        private IEnumerable<string> VillageDecisions
        {
            get
            {
                yield return "decision_militia_encourage";
                yield return "decision_militia_subsidize";
            }
        }

        public IEnumerable<string> Policies
        {
            get
            {
                yield return "criminal";
                yield return "garrison";
                yield return "draft";
                yield return "militia";
                yield return "tax";
                yield return "workforce";
            }
        }

        public void InitializeSettlement(Settlement settlement)
        {
            if (!SettlementDecisions.ContainsKey(settlement))
            {
                InitializeDecisions(settlement);
            }

            if (!SettlementPolicies.ContainsKey(settlement))
            {
                InitializePolicies(settlement);
            }
        }

        public bool IsSettlementPoliciesSet(Settlement settlement)
        {
            return SettlementDecisions.ContainsKey(settlement);
        }

        public List<BannerKingsDecision> GetDefaultDecisions(Settlement settlement)
        {
            if (!SettlementDecisions.ContainsKey(settlement))
            {
                InitializeDecisions(settlement);
            }

            return SettlementDecisions[settlement];
        }

        private void InitializeDecisions(Settlement settlement)
        {
            var decisions = new List<BannerKingsDecision>();
            if (settlement.IsVillage)
            {
                decisions.AddRange(VillageDecisions.Select(id => GenerateDecision(settlement, id)));
            }
            else if (settlement.IsCastle)
            {
                decisions.AddRange(CastleDecisions.Select(id => GenerateDecision(settlement, id)));
            }
            else if (settlement.IsTown)
            {
                decisions.AddRange(TownDecisions.Select(id => GenerateDecision(settlement, id)));
            }

            SettlementDecisions.Add(settlement, decisions);
        }

        private void InitializePolicies(Settlement settlement)
        {
            var policies = new List<BannerKingsPolicy>();

            foreach (var id in Policies)
            {
                policies.Add(GeneratePolicy(settlement, id));
            }

            SettlementPolicies.Add(settlement, policies);
        }

        public int GetActiveCostlyDecisionsNumber(Settlement settlement)
        {
            if (SettlementDecisions.ContainsKey(settlement))
            {
                var i = 0;
                foreach (var decision in SettlementDecisions[settlement])
                {
                    if (decision.Enabled)
                    {
                        var id = decision.GetIdentifier();
                        if (id != "decision_mercantilism" && id != "decision_slaves_tax" && id != "decision_tariff_exempt")
                        {
                            i += 1;
                        }
                    }
                }

                return i;
            }

            return 0;
        }

        public bool IsPolicyEnacted(Settlement settlement, string policyType, int value)
        {
            var policy = GetPolicy(settlement, policyType);
            return policy.Selected == value;
        }

        public BannerKingsPolicy GetPolicy(Settlement settlement, string policyType)
        {
            BannerKingsPolicy result = null;
            if (SettlementPolicies.ContainsKey(settlement))
            {
                var policies = SettlementPolicies[settlement];
                var policy = policies.FirstOrDefault(x => x.GetIdentifier() == policyType);
                if (policy != null)
                {
                    result = policy;
                }
            }
            else
            {
                result = GeneratePolicy(settlement, policyType);
                var set = new List<BannerKingsPolicy> {result};
                SettlementPolicies.Add(settlement, set);
            }

            if (result == null)
            {
                result = GeneratePolicy(settlement, policyType);
            }

            return result;
        }

        public BannerKingsDecision GenerateDecision(Settlement settlement, string policyType)
        {
            return policyType switch
            {
                "decision_militia_subsidize" => new BKSubsidizeMilitiaDecision(settlement, false),
                "decision_militia_encourage" => new BKEncourageMilitiaDecision(settlement, false),
                "decision_ration" => new BKRationDecision(settlement, false),
                "decision_tariff_exempt" => new BKExemptTariffDecision(settlement, false),
                "decision_foreigner_ban" => new BKBanForeignersDecision(settlement, false),
                "decision_slaves_tax" => new BKTaxSlavesDecision(settlement, false),
                "decision_mercantilism" => new BKEncourageMercantilism(settlement, false),
                _ => new BKExportSlavesDecision(settlement, true)
            };
        }

        public BannerKingsPolicy GeneratePolicy(Settlement settlement, string policyType)
        {
            return policyType switch
            {
                "garrison" => new BKGarrisonPolicy(GarrisonPolicy.Standard, settlement),
                "militia" => new BKMilitiaPolicy(MilitiaPolicy.Balanced, settlement),
                "tax" => new BKTaxPolicy(BKTaxPolicy.TaxType.Standard, settlement),
                "workforce" => new BKWorkforcePolicy(BKWorkforcePolicy.WorkforcePolicy.None, settlement),
                "draft" => new BKDraftPolicy(BKDraftPolicy.DraftPolicy.Standard, settlement),
                _ => new BKCriminalPolicy(CriminalPolicy.Enslavement, settlement)
            };
        }

        private void AddSettlementPolicy(Settlement settlement)
        {
            SettlementPolicies.Add(settlement, new List<BannerKingsPolicy>());
        }

        private void AddSettlementDecision(Settlement settlement)
        {
            SettlementDecisions.Add(settlement, new List<BannerKingsDecision>());
        }

        public void UpdateSettlementPolicy(Settlement settlement, BannerKingsPolicy policy)
        {
            if (SettlementPolicies.ContainsKey(settlement))
            {
                var policies = SettlementPolicies[settlement];
                var target = policies.FirstOrDefault(x => x.GetIdentifier() == policy.GetIdentifier());
                if (target != null)
                {
                    policies.Remove(target);
                }

                policies.Add(policy);
            }
            else
            {
                AddSettlementPolicy(settlement);
            }
        }

        public bool IsDecisionEnacted(Settlement settlement, string type)
        {
            BannerKingsDecision decision = null;
            if (SettlementDecisions.ContainsKey(settlement))
            {
                decision = SettlementDecisions[settlement].FirstOrDefault(x => x.GetIdentifier() == type);
            }

            return decision?.Enabled ?? false;
        }

        public void UpdateSettlementDecision(Settlement settlement, BannerKingsDecision decision)
        {
            if (SettlementDecisions.ContainsKey(settlement))
            {
                var policies = SettlementDecisions[settlement];
                var target = policies.FirstOrDefault(x => x.GetIdentifier() == decision.GetIdentifier());
                if (target != null)
                {
                    policies.Remove(target);
                }

                policies.Add(decision);
            }
            else
            {
                AddSettlementPolicy(settlement);
            }
        }
    }
}