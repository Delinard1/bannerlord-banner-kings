﻿using BannerKings.Managers.Institutions.Guilds;
using BannerKings.Managers.Populations;
using BannerKings.UI.Items.UI;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerKings.UI.Panels
{
    public class GuildVM : BannerKingsViewModel
    {
        private readonly Guild guild;
        private MBBindingList<InformationElement> guildInfo;

        public GuildVM(PopulationData data) : base(data, true)
        {
            guild = data.EconomicData.Guild;
            guildInfo = new MBBindingList<InformationElement>();
        }

        [DataSourceProperty]
        public ImageIdentifierVM GuildMaster => new(CharacterCode.CreateFrom(guild.Leader.CharacterObject));

        [DataSourceProperty] public string GuildMasterName => "Guildmaster " + guild.Leader.Name;

        [DataSourceProperty]
        public MBBindingList<InformationElement> GuildInfo
        {
            get => guildInfo;
            set
            {
                if (value != guildInfo)
                {
                    guildInfo = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            GuildInfo.Clear();
            GuildInfo.Add(new InformationElement("Capital:", guild.Capital.ToString(),
                "This guild's financial resources"));
            GuildInfo.Add(new InformationElement("Influence:", guild.Influence.ToString(),
                "Soft power this guild has, allowing them to call in favors and make demands"));
        }

        public new void ExecuteClose()
        {
            UIManager.Instance.CloseUI();
        }
    }
}