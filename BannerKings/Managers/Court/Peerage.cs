﻿using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace BannerKings.Managers.Court
{
    public class Peerage
    {

        public Peerage(TextObject name, bool vote, bool election, bool knighthood, bool fiefs, bool council, bool boostsVotes)
        {
            Name = name;
            CanVote = vote;
            CanStartElection = election;
            CanGrantKnighthood = knighthood;
            CanHaveFief = fiefs;
            CanHaveCouncil = council;
            BoostsVotes = boostsVotes;
        }

        public static Peerage GetAdequatePeerage(Clan clan)
        {
            if (clan.IsUnderMercenaryService)
            {
                return new Peerage(new TextObject("{=!}No Peerage"), false, false, false, false, false, false);
            }


            if (clan.Kingdom != null)
            {
                var titles = BannerKingsConfig.Instance.TitleManager.GetAllDeJure(clan);
                if (titles.Any(x => x.type != Titles.TitleType.Lordship) || clan.Fiefs.Count > 0)
                {
                    return new Peerage(new TextObject("{=!}Full Peerage"), true, true, true, true, true, false);
                }
                else
                {
                    return new Peerage(new TextObject("{=!}Lesser Peerage"), true, false, false, false, true, true);
                }
            }

            return new Peerage(new TextObject("{=!}No Peerage"), false, false, false, false, false, false);
        }

        [SaveableProperty(1)] public TextObject Name { get; private set; }
        [SaveableProperty(2)] public bool CanVote { get; private set; }
        [SaveableProperty(3)] public bool CanStartElection { get; private set; }
        [SaveableProperty(4)] public bool CanGrantKnighthood { get; private set; }
        [SaveableProperty(5)] public bool CanHaveFief { get; private set; }
        [SaveableProperty(6)] public bool CanHaveCouncil { get; private set; }
        [SaveableProperty(7)] public bool BoostsVotes { get; private set; }

        public TextObject GetRights() => new TextObject("{=!}Voting Allowed: {VOTING}\nElections Allowed: {ELECTIONS}\nKnighthood Granting: {KNIGHT}\nFiefs Allowed: {FIEFS}\nCouncil Allowed: {COUNCIL}")
            .SetTextVariable("VOTING", GetStr(CanVote))
            .SetTextVariable("ELECTIONS", GetStr(CanStartElection))
            .SetTextVariable("KNIGHT", GetStr(CanGrantKnighthood))
            .SetTextVariable("FIEFS", GetStr(CanHaveFief))
            .SetTextVariable("COUNCIL", GetStr(CanHaveCouncil));

        public TextObject PeerageGrantedText() => new TextObject("{=!}The family {VOTE}, {ELECTION}, {KNIGHTHOOD}, {FIEF} and {COUNCIL}.")
            .SetTextVariable("CLAN", Clan.PlayerClan.Name)
            .SetTextVariable("PEERAGE", Name)
            .SetTextVariable("VOTE", CanVote ? new TextObject("{=!}will be able to vote in elections") : new TextObject("{=!}will not be able to vote on elections"))
            .SetTextVariable("ELECTION", CanStartElection ? new TextObject("{=!}will be able to start elections") : new TextObject("{=!}will not be able to start elections"))
            .SetTextVariable("KNIGHTHOOD", CanGrantKnighthood ? new TextObject("{=!}will be able to grant knighthood") : new TextObject("{=!}will not be able to grant knighthood"))
            .SetTextVariable("FIEF", CanHaveFief ? new TextObject("{=!}will be eligible to be awarded fiefs") : new TextObject("{=!}will not be eligible to be awarded fiefs"))
            .SetTextVariable("COUNCIL", CanHaveCouncil ? new TextObject("{=!}will be able to host a council") : new TextObject("{=!}will not be able to host a council"));

        private TextObject GetStr(bool option) => option ? GameTexts.FindText("str_yes") : GameTexts.FindText("str_no");
    }
}
