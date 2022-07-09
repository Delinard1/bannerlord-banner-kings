﻿using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace BannerKings.Managers.Titles
{
    public class TitleAction
    {
        public bool Possible { get; set; }
        public TextObject Reason { get; set; }
        public float Gold { get; set; }
        public float Influence { get; set; }
        public float Renown { get; set; }
        public ActionType Type { get; private set; }
        public FeudalTitle Title { get; private set; }
        public List<FeudalTitle> Vassals { get; private set; }
        public Hero ActionTaker { get; private set; }

        public TitleAction(ActionType type, FeudalTitle title, Hero actionTaker)
        {
            Type = type;
            Title = title;
            ActionTaker = actionTaker;
            Vassals = new List<FeudalTitle>();
        }

        public void SetTile(FeudalTitle title) => Title = title;

        public void SetVassals(List<FeudalTitle> vassals) => Vassals = vassals;

        public void TakeAction(Hero receiver)
        {
            if (!Possible) return;

            if (Type == ActionType.Usurp)
                BannerKingsConfig.Instance.TitleManager.UsurpTitle(this.Title.deJure, this);
            else if (Type == ActionType.Claim)
                BannerKingsConfig.Instance.TitleManager.AddOngoingClaim(this);
            else if (Type == ActionType.Revoke)
                BannerKingsConfig.Instance.TitleManager.RevokeTitle(this);
            else if (Type == ActionType.Found)
                BannerKingsConfig.Instance.TitleManager.FoundKingdom(this);
            else BannerKingsConfig.Instance.TitleManager.GrantTitle(receiver, this.ActionTaker, this.Title, this.Influence);
        }
    }

    public enum ActionType
    {
        Usurp,
        Revoke,
        Grant,
        Destroy,
        Create,
        Claim,
        Found
    }
}
