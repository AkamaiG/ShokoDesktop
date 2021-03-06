﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Shoko.Commons.Extensions;
using Shoko.Desktop.Utilities;
using Shoko.Desktop.ViewModel;
using Shoko.Desktop.ViewModel.Server;

namespace Shoko.Desktop.UserControls
{
    /// <summary>
    /// Interaction logic for MissingMyListFilesControl.xaml
    /// </summary>
    public partial class MissingMyListFilesControl : UserControl
    {
        BackgroundWorker workerFiles = new BackgroundWorker();
        BackgroundWorker workerDeleteFiles = new BackgroundWorker();

        public ICollectionView ViewFiles { get; set; }
        public ObservableCollection<VM_MissingFile> MissingFilesCollection { get; set; }

        private List<VM_MissingFile> contracts = new List<VM_MissingFile>();

        public static readonly DependencyProperty FileCountProperty = DependencyProperty.Register("FileCount",
            typeof(int), typeof(MissingMyListFilesControl), new UIPropertyMetadata(0, null));

        public int FileCount
        {
            get { return (int)GetValue(FileCountProperty); }
            set { SetValue(FileCountProperty, value); }
        }

        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register("IsLoading",
            typeof(bool), typeof(MissingMyListFilesControl), new UIPropertyMetadata(false, null));

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set
            {
                SetValue(IsLoadingProperty, value);
                IsNotLoading = !IsLoading;
            }
        }

        public static readonly DependencyProperty IsNotLoadingProperty = DependencyProperty.Register("IsNotLoading",
            typeof(bool), typeof(MissingMyListFilesControl), new UIPropertyMetadata(true, null));

        public bool IsNotLoading
        {
            get { return (bool)GetValue(IsNotLoadingProperty); }
            set { SetValue(IsNotLoadingProperty, value); }
        }

        public static readonly DependencyProperty ReadyToRemoveFilesProperty = DependencyProperty.Register("ReadyToRemoveFiles",
            typeof(bool), typeof(MissingMyListFilesControl), new UIPropertyMetadata(false, null));

        public static readonly DependencyProperty StatusMessageProperty = DependencyProperty.Register("StatusMessage",
            typeof(string), typeof(MissingMyListFilesControl), new UIPropertyMetadata("", null));

        public string StatusMessage
        {
            get { return (string)GetValue(StatusMessageProperty); }
            set { SetValue(StatusMessageProperty, value); }
        }

        public bool ReadyToRemoveFiles
        {
            get { return (bool)GetValue(ReadyToRemoveFilesProperty); }
            set { SetValue(ReadyToRemoveFilesProperty, value); }
        }

        public MissingMyListFilesControl()
        {
            InitializeComponent();

            ReadyToRemoveFiles = false;
            IsLoading = false;

            MissingFilesCollection = new ObservableCollection<VM_MissingFile>();
            ViewFiles = CollectionViewSource.GetDefaultView(MissingFilesCollection);
            ViewFiles.SortDescriptions.Add(new SortDescription("AnimeTitle", ListSortDirection.Ascending));
            ViewFiles.SortDescriptions.Add(new SortDescription("EpisodeTypeAndNumber", ListSortDirection.Ascending));

            btnDelete.Click += new RoutedEventHandler(btnDelete_Click);
            btnRefresh.Click += new RoutedEventHandler(btnRefresh_Click);

            workerFiles.DoWork += new DoWorkEventHandler(workerFiles_DoWork);
            workerFiles.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerFiles_RunWorkerCompleted);

            workerDeleteFiles.WorkerReportsProgress = true;
            workerDeleteFiles.DoWork += new DoWorkEventHandler(workerDeleteFiles_DoWork);
            workerDeleteFiles.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerDeleteFiles_RunWorkerCompleted);
            workerDeleteFiles.ProgressChanged += new ProgressChangedEventHandler(workerDeleteFiles_ProgressChanged);
        }

        void workerDeleteFiles_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            StatusMessage = e.UserState.ToString();
        }

        void workerDeleteFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnRefresh.IsEnabled = true;
            btnDelete.IsEnabled = true;

            IsLoading = false;
            ReadyToRemoveFiles = false;
            FileCount = 0;

            StatusMessage = "Complete";
        }

        void workerDeleteFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                List<VM_MissingFile> fls = e.Argument as List<VM_MissingFile>;

                int i = 0;
                foreach (VM_MissingFile mf in fls)
                {
                    i++;
                    string msg = $"Queueing deletion {i} of {fls.Count}";
                    workerDeleteFiles.ReportProgress(0, msg);
                    VM_ShokoServer.Instance.ShokoServices.DeleteFileFromMyList(mf.FileID);
                }
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }

        }

        void workerFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            contracts = e.Result as List<VM_MissingFile>;
            foreach (VM_MissingFile mf in contracts)
                MissingFilesCollection.Add(mf);
            FileCount = contracts.Count;
            ReadyToRemoveFiles = FileCount >= 1;
            btnRefresh.IsEnabled = true;
            IsLoading = false;
            Cursor = Cursors.Arrow;
        }

        void workerFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                List<VM_MissingFile> contractsTemp = VM_ShokoServer.Instance.ShokoServices.GetMyListFilesForRemoval(VM_ShokoServer.Instance.CurrentUser.JMMUserID).CastList<VM_MissingFile>();
                e.Result = contractsTemp;
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }

        void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            IsLoading = true;
            btnRefresh.IsEnabled = false;
            MissingFilesCollection.Clear();
            FileCount = 0;

            StatusMessage = "Loading...";

            workerFiles.RunWorkerAsync();
        }

        void btnDelete_Click(object sender, RoutedEventArgs e)
        {


            MessageBoxResult res = MessageBox.Show(string.Format("Are you sure you want to delete all these files from your AniDB list?"),
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                IsLoading = true;
                btnRefresh.IsEnabled = false;
                btnDelete.IsEnabled = false;

                ReadyToRemoveFiles = false;

                StatusMessage = "Preparing to queue files for removal on server";
                //Thread.Sleep(1500);

                List<VM_MissingFile> mfs = new List<VM_MissingFile>(MissingFilesCollection);

                workerDeleteFiles.RunWorkerAsync(mfs);

                MissingFilesCollection.Clear();
                FileCount = 0;
            }

        }
    }
}
