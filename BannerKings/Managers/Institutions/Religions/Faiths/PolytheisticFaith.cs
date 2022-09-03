﻿using System.Collections.Generic;
using BannerKings.Managers.Institutions.Religions.Faiths.Rites;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace BannerKings.Managers.Institutions.Religions.Faiths
{
    public abstract class PolytheisticFaith : Faith
    {
        public void Initialize(Divinity mainGod, List<Divinity> pantheon, Dictionary<TraitObject, bool> traits, FaithGroup faithGroup, List<Rite> rites = null)
        {
            base.Initialize(mainGod, traits, faithGroup);
            this.pantheon = pantheon;
        }
    }
}