﻿using System.ComponentModel;
using System.IO;
using Shoko.Commons.Notification;
using Shoko.Models.Enums;
using Shoko.Desktop.ImageDownload;
using Shoko.Desktop.Utilities;
using Shoko.Desktop.ViewModel.Helpers;
using Shoko.Models.Server;
// ReSharper disable InconsistentNaming

namespace Shoko.Desktop.ViewModel.Server
{
    public class VM_MovieDB_Poster : MovieDB_Poster, INotifyPropertyChanged, INotifyPropertyChangedExt
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propname));
        }

        public string FullImagePathPlain
        {
            get
            {
                //strip out the base URL
                int pos = URL.IndexOf('/', 0);
                string fname = URL.Substring(pos + 1, URL.Length - pos - 1);
                fname = fname.Replace("/", @"\");
                string filename = Path.Combine(Utils.GetMovieDBImagePath(), fname);

                return filename;
            }
        }

        public string FullImagePath
        {
            get
            {
                if (!File.Exists(FullImagePathPlain))
                {
                    ImageDownloadRequest req = new ImageDownloadRequest(ImageEntityType.MovieDB_Poster, this, false);
                    MainWindow.imageHelper.DownloadImage(req);
                    if (File.Exists(FullImagePathPlain)) return FullImagePathPlain;
                }

                return FullImagePathPlain;
            }
        }

        public bool IsImageEnabled
        {
            get { return base.Enabled == 1; }
            set { base.Enabled = value ? 1 : 0; }
        }


        public new int Enabled
        {
            get { return base.Enabled; }
            set { this.SetField(()=>base.Enabled,(r)=> base.Enabled = r, value, () => Enabled, () => IsImageEnabled); }
        }
        private bool isImageDefault;
        public bool IsImageDefault
        {
            get { return isImageDefault; }
            set { this.SetField(()=>isImageDefault,value); }
        }

    }
}
