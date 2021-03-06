﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Shoko.Commons.Extensions;
using Shoko.Desktop.Enums;
using Shoko.Desktop.Forms;
using Shoko.Desktop.Utilities;
using Shoko.Desktop.ViewModel;
using Shoko.Desktop.ViewModel.Helpers;
using Shoko.Desktop.ViewModel.Server;
using Shoko.Models.Client;

namespace Shoko.Desktop.UserControls
{
    /// <summary>
    /// Interaction logic for UnrecognisedVideos.xaml
    /// </summary>
    public partial class UnrecognisedVideos : UserControl
    {


        public ICollectionView ViewFiles { get; set; }
        public ObservableCollection<VM_VideoLocal> UnrecognisedFiles { get; set; }

        public ICollectionView ViewSeries { get; set; }
        public ObservableCollection<VM_AnimeSeries_User> AllSeries { get; set; }

        public static readonly DependencyProperty AnyVideosSelectedProperty = DependencyProperty.Register("AnyVideosSelected",
            typeof(bool), typeof(UnrecognisedVideos), new UIPropertyMetadata(false, null));

        public static readonly DependencyProperty OneVideoSelectedProperty = DependencyProperty.Register("OneVideoSelected",
            typeof(bool), typeof(UnrecognisedVideos), new UIPropertyMetadata(false, null));

        public static readonly DependencyProperty OneVideoTypeRangeProperty = DependencyProperty.Register("OneVideoTypeRange",
            typeof(bool), typeof(UnrecognisedVideos), new UIPropertyMetadata(false, null));

        public static readonly DependencyProperty OneVideoTypeSingleProperty = DependencyProperty.Register("OneVideoTypeSingle",
            typeof(bool), typeof(UnrecognisedVideos), new UIPropertyMetadata(false, null));

        public static readonly DependencyProperty MultipleVideosSelectedProperty = DependencyProperty.Register("MultipleVideosSelected",
            typeof(bool), typeof(UnrecognisedVideos), new UIPropertyMetadata(false, null));

        public static readonly DependencyProperty MultipleTypeRangeProperty = DependencyProperty.Register("MultipleTypeRange",
            typeof(bool), typeof(UnrecognisedVideos), new UIPropertyMetadata(false, null));

        public static readonly DependencyProperty MultipleTypeSingleProperty = DependencyProperty.Register("MultipleTypeSingle",
            typeof(bool), typeof(UnrecognisedVideos), new UIPropertyMetadata(false, null));

        public static readonly DependencyProperty FileCountProperty = DependencyProperty.Register("FileCount",
            typeof(int), typeof(UnrecognisedVideos), new UIPropertyMetadata(0, null));

        public int FileCount
        {
            get { return (int)GetValue(FileCountProperty); }
            set { SetValue(FileCountProperty, value); }
        }

        public bool AnyVideosSelected
        {
            get { return (bool)GetValue(AnyVideosSelectedProperty); }
            set { SetValue(AnyVideosSelectedProperty, value); }
        }

        public bool OneVideoSelected
        {
            get { return (bool)GetValue(OneVideoSelectedProperty); }
            set { SetValue(OneVideoSelectedProperty, value); }
        }

        public bool OneVideoTypeRange
        {
            get { return (bool)GetValue(OneVideoTypeRangeProperty); }
            set { SetValue(OneVideoTypeRangeProperty, value); }
        }

        public bool OneVideoTypeSingle
        {
            get { return (bool)GetValue(OneVideoTypeSingleProperty); }
            set { SetValue(OneVideoTypeSingleProperty, value); }
        }

        public bool MultipleVideosSelected
        {
            get { return (bool)GetValue(MultipleVideosSelectedProperty); }
            set { SetValue(MultipleVideosSelectedProperty, value); }
        }

        public bool MultipleTypeRange
        {
            get { return (bool)GetValue(MultipleTypeRangeProperty); }
            set { SetValue(MultipleTypeRangeProperty, value); }
        }

        public bool MultipleTypeSingle
        {
            get { return (bool)GetValue(MultipleTypeSingleProperty); }
            set { SetValue(MultipleTypeSingleProperty, value); }
        }

        public UnrecognisedVideos()
        {
            InitializeComponent();

            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(AppSettings.Culture);

            UnrecognisedFiles = new ObservableCollection<VM_VideoLocal>();
            ViewFiles = CollectionViewSource.GetDefaultView(UnrecognisedFiles);
            ViewFiles.SortDescriptions.Add(new SortDescription("FileName", ListSortDirection.Ascending));
            ViewFiles.Filter = FileSearchFilter;

            AllSeries = new ObservableCollection<VM_AnimeSeries_User>();
            ViewSeries = CollectionViewSource.GetDefaultView(AllSeries);
            ViewSeries.SortDescriptions.Add(new SortDescription("SeriesName", ListSortDirection.Ascending));
            ViewSeries.Filter = SeriesSearchFilter;


            btnRefresh.Click += new RoutedEventHandler(btnRefresh_Click);
            btnConfirm.Click += new RoutedEventHandler(btnConfirm_Click);
            btnAddSeries.Click += new RoutedEventHandler(btnAddSeries_Click);
            btnRescan.Click += new RoutedEventHandler(btnRescan_Click);
            btnRehash.Click += btnRehash_Click;

            txtSeriesSearch.TextChanged += new TextChangedEventHandler(txtSeriesSearch_TextChanged);


            lbSeries.SelectionChanged += new SelectionChangedEventHandler(lbSeries_SelectionChanged);
            cboEpisodes.SelectionChanged += new SelectionChangedEventHandler(cboEpisodes_SelectionChanged);
            dgVideos.SelectionChanged += DgVideos_SelectionChanged;
            txtStartEpNum.TextChanged += new TextChangedEventHandler(txtStartEpNum_TextChanged);
            txtEndEpNumSingle.TextChanged += new TextChangedEventHandler(txtEndEpNumSingle_TextChanged);

            cboMultiType.Items.Add(Shoko.Commons.Properties.Resources.MultiTypeRange);
            cboMultiType.Items.Add(Shoko.Commons.Properties.Resources.MultiTypeSingle);
            cboMultiType.SelectedIndex = 1;

            cboMultiType.SelectionChanged += new SelectionChangedEventHandler(cboMultiType_SelectionChanged);

            SetConfirmDetails();

            OneVideoSelected = dgVideos.SelectedItems.Count == 1;
            MultipleVideosSelected = dgVideos.SelectedItems.Count > 1;

            btnClearSearch.Click += new RoutedEventHandler(btnClearSearch_Click);
            txtFileSearch.TextChanged += new TextChangedEventHandler(txtFileSearch_TextChanged);
            btnLogs.Click += new RoutedEventHandler(btnLogs_Click);

            btnRefreshSeriesList.Click += new RoutedEventHandler(btnRefreshSeriesList_Click);
        }


        private void DgVideos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ccDetail.Content = null;
                ccDetailMultiple.Content = null;

                AnyVideosSelected = dgVideos.SelectedItems.Count > 0;
                OneVideoSelected = dgVideos.SelectedItems.Count == 1;
                MultipleVideosSelected = dgVideos.SelectedItems.Count > 1;

                MultipleTypeRange = cboMultiType.SelectedIndex == 0;
                MultipleTypeSingle = cboMultiType.SelectedIndex == 1;

                Visibility visible = Visibility.Visible;

                // if only one video selected
                if (OneVideoSelected)
                {
                    VM_VideoLocal vid = dgVideos.SelectedItem as VM_VideoLocal;
                    ccDetail.Content = vid;
                    if (vid!=null && !Commons.Extensions.Models.IsHashed(vid))
                        visible = Visibility.Hidden;
                }

                // if only one video selected
                if (MultipleVideosSelected)
                {
                    MultipleVideos mv = new MultipleVideos();
                    mv.SelectedCount = dgVideos.SelectedItems.Count;
                    mv.VideoLocalIDs = new List<int>();
                    mv.VideoLocals = new List<VM_VideoLocal>();

                    foreach (object obj in dgVideos.SelectedItems)
                    {
                        VM_VideoLocal vid = obj as VM_VideoLocal;
                        mv.VideoLocalIDs.Add(vid.VideoLocalID);
                        mv.VideoLocals.Add(vid);
                    }

                    ccDetailMultiple.Content = mv;
                    if (!mv.AllHaveHashes)
                        visible = Visibility.Hidden;
                }
                SetConfirmDetails();
                Confirm1.Visibility = Confirm2.Visibility = visible;
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }

        void btnRefreshSeriesList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!VM_ShokoServer.Instance.ServerOnline) return;

                Cursor = Cursors.Wait;
                RefreshSeries();
                Cursor = Cursors.Arrow;

            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }


        }

        void btnLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string logPath = Path.Combine(appPath, "logs");

                Process.Start(new ProcessStartInfo(logPath));
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }

        void btnRehash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!VM_ShokoServer.Instance.ServerOnline) return;

                Cursor = Cursors.Wait;
                foreach (VM_VideoLocal vid in UnrecognisedFiles.Where(a=>Commons.Extensions.Models.IsHashed(a)))
                {
                    VM_ShokoServer.Instance.ShokoServices.RehashFile(vid.VideoLocalID);
                }
                Cursor = Cursors.Arrow;

                MessageBox.Show(Shoko.Commons.Properties.Resources.Unrecognized_AniDBQueue, Shoko.Commons.Properties.Resources.Complete, MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        void btnRescan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!VM_ShokoServer.Instance.ServerOnline) return;

                Cursor = Cursors.Wait;
                VM_ShokoServer.Instance.ShokoServices.RescanUnlinkedFiles();
                Cursor = Cursors.Arrow;

                MessageBox.Show(Shoko.Commons.Properties.Resources.Unrecognized_AniDBScan, Shoko.Commons.Properties.Resources.Complete, MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        void txtFileSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewFiles.Refresh();
        }

        void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtFileSearch.Text = "";
        }

        void txtEndEpNumSingle_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetConfirmDetails();
        }

        void cboMultiType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetConfirmDetails();
        }

        void btnAddSeries_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NewSeries frm = new NewSeries();
                frm.Owner = GetTopParent();
                frm.Init(0, "");
                bool? result = frm.ShowDialog();
                if (result.HasValue && result.Value == true)
                {
                    RefreshSeries();

                    VM_AnimeSeries_User ser = frm.AnimeSeries;
                    txtSeriesSearch.Text = ser.AniDBAnime.AniDBAnime.FormattedTitle;
                }
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }

        private Window GetTopParent()
        {
            DependencyObject dpParent = Parent;
            do
            {
                dpParent = LogicalTreeHelper.GetParent(dpParent);
            }
            while (dpParent.GetType().BaseType != typeof(Window));

            return dpParent as Window;
        }

        private void EnableDisableControls(bool val)
        {
            lbSeries.IsEnabled = val;
            dgVideos.IsEnabled = val;
            btnAddSeries.IsEnabled = val;
            btnConfirm.IsEnabled = val;
            btnRefresh.IsEnabled = val;
            txtSeriesSearch.IsEnabled = val;
            txtStartEpNum.IsEnabled = val;
            cboEpisodes.IsEnabled = val;
            ccDetail.IsEnabled = val;
            ccDetailMultiple.IsEnabled = val;
        }

        void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window parentWindow = Window.GetWindow(this);

                // if only one video selected
                if (OneVideoSelected)
                {
                    EnableDisableControls(false);

                    VM_VideoLocal vid = dgVideos.SelectedItem as VM_VideoLocal;

                    if (cboMultiType.SelectedIndex == 0)
                    {
                        // single file to multiple episodes
                        // eg a file is a double episode

                        int startEpNum = 0, endEpNum = 0;

                        int.TryParse(txtStartEpNum.Text, out startEpNum);
                        int.TryParse(txtEndEpNumSingle.Text, out endEpNum);

                        string result = "";
                        // make sure the episode range is valid
                        // make sure the last episode number is within the valid range
                        VM_AnimeSeries_User series = lbSeries.SelectedItem as VM_AnimeSeries_User;
                        if (series.LatestRegularEpisodeNumber < endEpNum || startEpNum <= 0 && endEpNum <= 0 && endEpNum <= startEpNum)
                        {
                            // otherwise allow the user to refresh it from anidb
                            MessageBoxResult res = MessageBox.Show(Shoko.Commons.Properties.Resources.MSG_ERR_InvalidEpGetAnime, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                            if (res == MessageBoxResult.Yes)
                            {
                                result = VM_ShokoServer.Instance.ShokoServices.UpdateAnimeData(series.AniDB_ID);
                                if (result.Length > 0)
                                {
                                    MessageBox.Show(result, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                                    EnableDisableControls(true);
                                    return;
                                }
                                else
                                {
                                    // check again
                                    if (series.LatestRegularEpisodeNumber < endEpNum)
                                    {
                                        MessageBox.Show(Shoko.Commons.Properties.Resources.MSG_ERR_InvalidEp, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                        EnableDisableControls(true);
                                        return;
                                    }

                                }
                            }
                            else
                            {
                                EnableDisableControls(true);
                                return;
                            }
                        }

                        result = VM_ShokoServer.Instance.ShokoServices.AssociateSingleFileWithMultipleEpisodes(vid.VideoLocalID, series.AnimeSeriesID, startEpNum, endEpNum);
                        if (result.Length > 0)
                        {
                            MessageBox.Show(result, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            RefreshUnrecognisedFiles();
                        }
                    }
                    else
                    {
                        // single file to a single episode
                        VM_AnimeEpisode_User ep = cboEpisodes.SelectedItem as VM_AnimeEpisode_User;

                        string result = VM_ShokoServer.Instance.ShokoServices.AssociateSingleFile(vid.VideoLocalID, ep.AnimeEpisodeID);
                        if (result.Length > 0)
                        {
                            MessageBox.Show(result, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            RefreshUnrecognisedFiles();
                            //VM_MainListHelper.Instance.UpdateHeirarchy(ep, ((MainWindow)parentWindow).epListMain);
                            VM_MainListHelper.Instance.RefreshHeirarchy(ep);
                        }
                    }
                }

                // if multiple videos selected
                if (MultipleVideosSelected)
                {
                    int startEpNum = 0, endEpNum = 0;

                    int.TryParse(txtStartEpNum.Text, out startEpNum);

                    if (MultipleTypeRange)
                        endEpNum = startEpNum + dgVideos.SelectedItems.Count - 1;
                    else
                        endEpNum = startEpNum;

                    // make sure the last episode number is within the valid range
                    VM_AnimeSeries_User series = lbSeries.SelectedItem as VM_AnimeSeries_User;
                    if (series.LatestRegularEpisodeNumber < endEpNum && startEpNum > 0)
                    {
                        // otherwise allow the user to refresh it from anidb
                        MessageBoxResult res = MessageBox.Show(Shoko.Commons.Properties.Resources.MSG_ERR_InvalidEpGetAnime, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                        if (res == MessageBoxResult.Yes)
                        {
                            string result = VM_ShokoServer.Instance.ShokoServices.UpdateAnimeData(series.AniDB_ID);
                            if (result.Length > 0)
                            {
                                MessageBox.Show(result, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                                EnableDisableControls(true);
                                return;
                            }
                            else
                            {
                                // check again
                                if (series.LatestRegularEpisodeNumber < endEpNum)
                                {
                                    MessageBox.Show(Shoko.Commons.Properties.Resources.MSG_ERR_InvalidEp, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    EnableDisableControls(true);
                                    return;
                                }

                            }
                        }
                        else
                        {
                            EnableDisableControls(true);
                            return;
                        }

                    }

                    // get all the selected videos
                    List<int> vidIDs = new List<int>();
                    foreach (object obj in dgVideos.SelectedItems)
                    {
                        VM_VideoLocal vid = obj as VM_VideoLocal;
                        vidIDs.Add(vid.VideoLocalID);
                    }

                    string msg = VM_ShokoServer.Instance.ShokoServices.AssociateMultipleFiles(vidIDs, series.AnimeSeriesID, startEpNum, MultipleTypeSingle);
                    if (msg.Length > 0)
                    {
                        MessageBox.Show(msg, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        RefreshUnrecognisedFiles();

                        //VM_MainListHelper.Instance.UpdateHeirarchy(ep, ((MainWindow)parentWindow).epListMain);
                        //ep.RefreshFilesForEpisode();
                    }

                }
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
            EnableDisableControls(true);
        }

        void txtStartEpNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetConfirmDetails();
        }

        private void CommandBinding_OpenFolder(object sender, ExecutedRoutedEventArgs e)
        {
            object obj = e.Parameter;
            if (obj == null) return;

            if (obj.GetType() == typeof(VM_VideoLocal))
            {
                VM_VideoLocal vid = obj as VM_VideoLocal;

                if (Commons.Extensions.Models.IsLocalFile(vid) && File.Exists(vid.GetLocalFileSystemFullPath()))
                {
                    Utils.OpenFolderAndSelectFile(vid.GetLocalFileSystemFullPath());
                }
                else
                {
                    MessageBox.Show(Shoko.Commons.Properties.Resources.MSG_ERR_FileNotFound, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CommandBinding_IgnoreFile(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Window parentWindow = Window.GetWindow(this);

                object obj = e.Parameter;
                if (obj == null) return;

                if (obj.GetType() == typeof(VM_VideoLocal))
                {
                    VM_VideoLocal vid = obj as VM_VideoLocal;
                    EnableDisableControls(false);

                    string result = VM_ShokoServer.Instance.ShokoServices.SetIgnoreStatusOnFile(vid.VideoLocalID, true);
                    if (result.Length > 0)
                        MessageBox.Show(result, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        RefreshUnrecognisedFiles();

                }
                if (obj.GetType() == typeof(MultipleVideos))
                {
                    MultipleVideos mv = obj as MultipleVideos;
                    foreach (int id in mv.VideoLocalIDs)
                    {
                        string result = VM_ShokoServer.Instance.ShokoServices.SetIgnoreStatusOnFile(id, true);
                        if (result.Length > 0)
                            MessageBox.Show(result, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    RefreshUnrecognisedFiles();
                }
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }

            EnableDisableControls(true);
        }

        private void CommandBinding_PlayVideo(object sender, ExecutedRoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);

            object obj = e.Parameter;
            if (obj == null) return;

            try
            {
                if (obj.GetType() == typeof(VM_VideoLocal))
                {
                    VM_VideoLocal vid = obj as VM_VideoLocal;
                    bool force = true;
                    if (MainWindow.videoHandler.DefaultPlayer.Player.ToString() !=
                        Enum.GetName(typeof(VideoPlayer), VideoPlayer.WindowsDefault))
                    {
                        if (vid.ResumePosition > 0)
                        {
                            AskResumeVideo ask = new AskResumeVideo(vid.ResumePosition);
                            ask.Owner = Window.GetWindow(this);
                            if (ask.ShowDialog() == true)
                                force = false;
                        }
                    }
                    MainWindow.videoHandler.PlayVideo(vid, force);
                }
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }

        private void CommandBinding_DeleteFile(object sender, ExecutedRoutedEventArgs e)
        {
	        try
	        {
		        Window parentWindow = Window.GetWindow(this);

		        object obj = e.Parameter;
		        if (obj == null) return;

		        if (obj.GetType() == typeof(VM_VideoLocal))
		        {
			        VM_VideoLocal vid = obj as VM_VideoLocal;

			        AskDeleteFile dlg = new AskDeleteFile(string.Format(Shoko.Commons.Properties.Resources.DeleteFile_Title, vid.FileName),
                        Shoko.Commons.Properties.Resources.Unrecognized_ConfirmDelete + "\r\n\r\n" + Shoko.Commons.Properties.Resources.DeleteFile_Confirm,
				        vid.Places);
			        dlg.Owner = Window.GetWindow(this);
			        bool? res = dlg.ShowDialog();
			        if (res.HasValue && res.Value)
			        {
				        string tresult = string.Empty;
				        Cursor = Cursors.Wait;
				        foreach (CL_VideoLocal_Place lv in dlg.Selected)
				        {
					        string result =
						        VM_ShokoServer.Instance.ShokoServices.DeleteVideoLocalPlaceAndFile(
							        lv.VideoLocal_Place_ID);
					        if (result.Length > 0)
						        tresult += result + "\r\n";
				        }
				        if (!string.IsNullOrEmpty(tresult))
					        MessageBox.Show(tresult, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK,
						        MessageBoxImage.Error);
				        RefreshUnrecognisedFiles();
			        }


		        }
		        if (obj.GetType() == typeof(MultipleVideos))
		        {
			        MultipleVideos mv = obj as MultipleVideos;
			        AskDeleteFile dlg = new AskDeleteFile(Shoko.Commons.Properties.Resources.DeleteFile_Multiple,
                        Shoko.Commons.Properties.Resources.Unrecognized_DeleteSelected + "\r\n\r\n" + Shoko.Commons.Properties.Resources.DeleteFile_Confirm,
				        mv.VideoLocals.SelectMany(a => a.Places).ToList());
			        dlg.Owner = Window.GetWindow(this);
			        bool? res = dlg.ShowDialog();
			        if (res.HasValue && res.Value)
			        {
				        string tresult = string.Empty;
				        Cursor = Cursors.Wait;
				        foreach (CL_VideoLocal_Place lv in dlg.Selected)
				        {
					        string result = VM_ShokoServer.Instance.ShokoServices.DeleteVideoLocalPlaceAndFile(lv.VideoLocal_Place_ID);
					        if (result.Length > 0)
						        tresult += result + "\r\n";
				        }
				        if (!string.IsNullOrEmpty(tresult))
					        MessageBox.Show(tresult, Shoko.Commons.Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
				        RefreshUnrecognisedFiles();
			        }
		        }


	        }
	        catch (Exception ex)
	        {
		        Utils.ShowErrorMessage(ex);
		        RefreshUnrecognisedFiles();
	        }
	        finally
	        {
		        Cursor = Cursors.Arrow;
	        }
        }

        private void CommandBinding_RehashFile(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Window parentWindow = Window.GetWindow(this);

                object obj = e.Parameter;
                if (obj == null) return;

                if (obj.GetType() == typeof(VM_VideoLocal))
                {
                    VM_VideoLocal vid = obj as VM_VideoLocal;
                    if (Commons.Extensions.Models.IsLocalFile(vid))
                    {
                        EnableDisableControls(false);

                        VM_ShokoServer.Instance.ShokoServices.RehashFile(vid.VideoLocalID);
                    }
                }
                if (obj.GetType() == typeof(MultipleVideos))
                {
                    MultipleVideos mv = obj as MultipleVideos;
                    foreach(VM_VideoLocal v in mv.VideoLocals)
                    {
                        if (Commons.Extensions.Models.IsLocalFile(v))
                            VM_ShokoServer.Instance.ShokoServices.RehashFile(v.VideoLocalID);
                    }
                }

                MessageBox.Show(Shoko.Commons.Properties.Resources.MSG_INFO_AddedQueueCmds, Shoko.Commons.Properties.Resources.Done, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }

            EnableDisableControls(true);
        }

        private void CommandBinding_RescanFile(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Window parentWindow = Window.GetWindow(this);

                object obj = e.Parameter;
                if (obj == null) return;

                if (obj.GetType() == typeof(VM_VideoLocal))
                {
                    VM_VideoLocal vid = obj as VM_VideoLocal;
                    EnableDisableControls(false);
                    if (Commons.Extensions.Models.IsHashed(vid))
                        VM_ShokoServer.Instance.ShokoServices.RescanFile(vid.VideoLocalID);
                }
                if (obj.GetType() == typeof(MultipleVideos))
                {
                    MultipleVideos mv = obj as MultipleVideos;
                    foreach (VM_VideoLocal v in mv.VideoLocals)
                    {
                        if (Commons.Extensions.Models.IsHashed(v))
                            VM_ShokoServer.Instance.ShokoServices.RescanFile(v.VideoLocalID);
                    }
                }

                MessageBox.Show(Shoko.Commons.Properties.Resources.MSG_INFO_AddedQueueCmds, Shoko.Commons.Properties.Resources.Done, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }

            EnableDisableControls(true);
        }


        void cboEpisodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetConfirmDetails();
        }

        void lbSeries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (lbSeries.Items.Count == 0) return;
                if (lbSeries.SelectedItem == null) return;

                VM_AnimeSeries_User series = lbSeries.SelectedItem as VM_AnimeSeries_User;
                List<VM_AnimeEpisode_User> eps = series.AllEpisodes.OrderBy(a=>a.EpisodeType).ThenBy(a=>a.EpisodeNumber).ToList();

                cboEpisodes.ItemsSource = eps;
                if (cboEpisodes.Items.Count > 0)
                    cboEpisodes.SelectedIndex = 0;

                SetConfirmDetails();
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }

        private void SetConfirmDetails()
        {
            try
            {
                MultipleTypeRange = cboMultiType.SelectedIndex == 0;
                MultipleTypeSingle = cboMultiType.SelectedIndex == 1;

                btnConfirm.Visibility = Visibility.Hidden;
                cboEpisodes.Visibility = Visibility.Visible;

                if (dgVideos.SelectedItems.Count == 0)
                    btnConfirm.Visibility = Visibility.Hidden;

                // evaluate selected single file
                if (OneVideoSelected)
                {
                    if (cboMultiType.SelectedIndex == 0)
                    {
                        // episode range
                        int startEpNum = 0, endEpNum = 0;
                        int.TryParse(txtStartEpNum.Text, out startEpNum);
                        int.TryParse(txtEndEpNumSingle.Text, out endEpNum);

                        if (startEpNum > 0 && endEpNum > 0 && endEpNum > startEpNum)
                            btnConfirm.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        // single episode
                        if (dgVideos.SelectedItem != null && cboEpisodes.SelectedItem != null)
                            btnConfirm.Visibility = Visibility.Visible;
                    }
                }

                // evaluate multiple selected files
                if (MultipleVideosSelected)
                {
                    if (cboMultiType.SelectedIndex == 0)
                    {
                        // episode range
                        int startEpNum = 0;
                        int.TryParse(txtStartEpNum.Text, out startEpNum);
                        if (startEpNum > 0)
                        {
                            btnConfirm.Visibility = Visibility.Visible;
                            int endEpNum = startEpNum + dgVideos.SelectedItems.Count - 1;
                            txtEndEpNum.Text = endEpNum.ToString();
                        }
                    }
                    else
                    {
                        // single episode
                        if (cboEpisodes.SelectedItem != null)
                            btnConfirm.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }


        void txtSeriesSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewSeries.Refresh();
        }

        void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshUnrecognisedFiles();
        }

        public void RefreshUnrecognisedFiles()
        {
            try
            {
                UnrecognisedFiles.Clear();
                if (!VM_ShokoServer.Instance.ServerOnline) return;

                List<VM_VideoLocal> vids = VM_ShokoServer.Instance.ShokoServices.GetUnrecognisedFiles(VM_ShokoServer.Instance.CurrentUser.JMMUserID).CastList<VM_VideoLocal>();
                FileCount = vids.Count;

                foreach (VM_VideoLocal vid in vids)
                {
                    UnrecognisedFiles.Add(vid);
                }
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }

        public void RefreshSeries()
        {
            try
            {
                VM_MainListHelper.Instance.AllAnimeDetailedDictionary = null;
                AllSeries.Clear();
                if (!VM_ShokoServer.Instance.ServerOnline) return;
                foreach (VM_AnimeSeries_User ser in VM_ShokoServer.Instance.ShokoServices.GetAllSeries(VM_ShokoServer.Instance.CurrentUser.JMMUserID).Cast<VM_AnimeSeries_User>())
                {
                    AllSeries.Add(ser);
                }
            }
            catch (Exception ex)
            {
                Utils.ShowErrorMessage(ex);
            }
        }

        private bool SeriesSearchFilter(object obj)
        {
            VM_AnimeSeries_User servm = obj as VM_AnimeSeries_User;
            if (servm == null) return true;

            return GroupSearchFilterHelper.EvaluateSeriesTextSearch(servm, txtSeriesSearch.Text.Replace("'", "`"));
        }

        private bool FileSearchFilter(object obj)
        {
            VM_VideoLocal vid = obj as VM_VideoLocal;
            if (vid == null) return true;
            foreach (CL_VideoLocal_Place n in vid.Places)
            {
                int index = n.FilePath.IndexOf(txtFileSearch.Text.Trim(), 0, StringComparison.InvariantCultureIgnoreCase);
                if (index > -1) return true;
                /*
                index = vid.FileDirectory.IndexOf(txtFileSearch.Text.Trim(), 0, StringComparison.InvariantCultureIgnoreCase);
                if (index > -1) return true;*/
            }

            return false;
        }

    }
}
