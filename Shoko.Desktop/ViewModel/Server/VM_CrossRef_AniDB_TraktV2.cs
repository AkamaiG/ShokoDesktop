﻿using System.ComponentModel;
using Shoko.Commons.Extensions;
using Shoko.Commons.Notification;
using Shoko.Desktop.ViewModel.Helpers;
using Shoko.Models.Azure;

// ReSharper disable InconsistentNaming

namespace Shoko.Desktop.ViewModel.Server
{
    public class VM_CrossRef_AniDB_TraktV2 : Azure_CrossRef_AniDB_Trakt, INotifyPropertyChanged, INotifyPropertyChangedExt
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propname));
        }

        public string IsAdminApprovedImage => IsAdminApproved == 1 ? @"/Images/16_tick.png" : @"/Images/placeholder.png";

        public new int IsAdminApproved
        {
            get { return base.IsAdminApproved; }
            set { this.SetField(()=>base.IsAdminApproved,(r)=> base.IsAdminApproved = r, value, () => IsAdminApproved, () => IsAdminApprovedBool, () => IsAdminApprovedImage); }
        }

        public bool IsAdminApprovedBool => IsAdminApproved == 1;

        public string ShowURL => this.GetShowURL();

        public string AniDBURL => this.GetAniDBURL();

        public string AniDBStartEpisodeTypeString => this.GetAniDBStartEpisodeTypeString();

        public string AniDBStartEpisodeNumberString => this.GetAniDBStartEpisodeNumberString();

        public string TraktSeasonNumberString => this.GetTraktSeasonNumberString();

        public string TraktStartEpisodeNumberString => this.GetTraktStartEpisodeNumberString();
    }
}
