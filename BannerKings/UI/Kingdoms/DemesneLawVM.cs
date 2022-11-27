using BannerKings.Managers.Titles.Laws;
using BannerKings.UI.Items;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BannerKings.UI.Kingdoms
{
    public class DemesneLawVM : ViewModel
    {
        private BannerKingsSelectorVM<BKItemVM> selector;
        private Action<SelectorVM<BKItemVM>> onChange;
        private string nameText, descriptionText, dateText;
       

        public DemesneLawVM(List<DemesneLaw> options, DemesneLaw law, bool isKing, Action<SelectorVM<BKItemVM>> onChange)
        {
            NameText = GameTexts.FindText("str_bk_demesne_law", law.LawType.ToString()).ToString();
            this.onChange = onChange;
            Selector = new BannerKingsSelectorVM<BKItemVM>(isKing && law.AvailableForVoting, 0, null);

            int selected = 0;
            foreach (DemesneLaw option in options)
            {
                Selector.AddItem(new BKItemVM(option.Index,
                    option.LawType,
                    !option.Equals(law),
                    new TextObject("{=0bet1Am4}{TEXT}\n\n{EXPLANATIONS}")
                        .SetTextVariable("TEXT", option.Description)
                        .SetTextVariable("EXPLANATIONS", option.Effects),
                    option.Name));

                if (option.Equals(law))
                {
                    selected = options.IndexOf(law);
                }
            }

            DescriptionText = law.Description.ToString();
            DateText = law.IssueDate.ToString();

            Selector.SelectedIndex = selected;
            Selector.SetOnChangeAction(onChange);
        }

        public DemesneLaw DemesneLaw { get; private set; }

        [DataSourceProperty]
        public string DateHeaderText => new TextObject("{=SJZmL2Co}Law issued on:").ToString();

      

        [DataSourceProperty]
        public string NameText
        {
            get => nameText;
            set
            {
                if (value != nameText)
                {
                    nameText = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public string DescriptionText
        {
            get => descriptionText;
            set
            {
                if (value != descriptionText)
                {
                    descriptionText = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public string DateText
        {
            get => dateText;
            set
            {
                if (value != dateText)
                {
                    dateText = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public BannerKingsSelectorVM<BKItemVM> Selector
        {
            get => selector;
            set
            {
                if (value != selector)
                {
                    selector = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }
    }
}
