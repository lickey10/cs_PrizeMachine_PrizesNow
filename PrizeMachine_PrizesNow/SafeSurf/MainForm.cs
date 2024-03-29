using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using Microsoft.Win32;
using SCTVObjects;
using System.Runtime.InteropServices;

namespace SCTV
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1301:AvoidDuplicateAccelerators")]
    public partial class MainForm : Form
    {
        private bool loggedIn = false;
        public static string[] blockedTerms;
        public static string[] foundBlockedTerms;
        public static string[] foundBlockedSites;
        public static string blockedTermsPath = "config\\BlockedTerms.txt";
        public static string foundBlockedTermsPath = "config\\FoundBlockedTerms.txt";
        public static string[] blockedSites;
        public static string blockedSitesPath = "config\\BlockedSites.txt";
        public static string foundBlockedSitesPath = "config\\foundBlockedSites.txt";
        public static string loginInfoPath = "config\\LoginInfo.txt";
        public bool adminLock = false;//locks down browser until unlocked by a parent
        public int loggedInTime = 0;
        public bool checkForms = true;
        public bool MonitorActivity = false; //determines whether safesurf monitors page contents, forms, sites, etc...
        int loginMaxTime = 20;//20 minutes
        TabCtlEx tabControlEx = new TabCtlEx();

        bool showVolumeControl = false;
        bool showAddressBar = true;

        private DateTime startTime;
        private string userName;
        Timer keepRunning_tour_Timer = new Timer();
        //Timer secondsTimer = new Timer();
        Timer documentLoaded_tour_Timer = new Timer();
        Timer documentLoaded_tourList_Timer = new Timer();
        //ExtendedWebBrowser hideMeBrowser;
        ExtendedWebBrowser bitVideoBrowser;
        public ExtendedWebBrowser tourBrowser;
        int keepRunningTimerTicks = 0;
        //string goToUrlString = "";
        string startTourUrlString = "";
        public string documentStringLoaded = "";
        int currentPageNumber = 0;
        int nextPageNumber = 0;
        bool tourIsRunning = false;
        ArrayList tourList = new ArrayList();
        int currentTourIndex = 1;
        string currentPageURL = "";
        string previousPageURL = "";
        RefreshUtilities refreshUtilities;
        string documentString = "";
        bool enteredTheContest = false;
        bool foundPrize = false;
        bool foundQuickPick = false;
        bool foundNewContest = false;
        bool foundSubmit = false;
        int numberOfPrizesEntered = 0;
        int numberOfCashtravaganzaEntered = 0;
        int numberOfUnclaimedEntered = 0;
        int refreshCount = 0;

        public bool LoggedIn
        {
            set
            {
                loggedIn = value;

                if (loggedIn)
                {
                    UpdateLoginToolStripMenuItem.Visible = true;
                    parentalControlsToolStripMenuItem.Visible = true;
                    loginToolStripMenuItem.Visible = false;
                    logoutToolStripMenuItem.Visible = true;
                    logoutToolStripButton.Visible = true;
                    LoginToolStripButton.Visible = false;
                    adminToolStripButton.Visible = true;

                    loginTimer.Enabled = true;
                    loginTimer.Start();
                }
                else
                {
                    UpdateLoginToolStripMenuItem.Visible = false;
                    parentalControlsToolStripMenuItem.Visible = false;
                    loginToolStripMenuItem.Visible = true;
                    logoutToolStripMenuItem.Visible = false;
                    logoutToolStripButton.Visible = false;
                    LoginToolStripButton.Visible = true;
                    adminToolStripButton.Visible = false;
                    tcAdmin.Visible = false;

                    loginTimer.Enabled = false;
                    loginTimer.Stop();
                }
            }

            get
            {
                return loggedIn;
            }
        }

        public Uri URL
        {
            set { _windowManager.ActiveBrowser.Url = value; }
            get { return _windowManager.ActiveBrowser.Url; }
        }

        public bool ShowMenuStrip
        {
            set { this.menuStrip.Visible = value; }
        }

        public FormBorderStyle FormBorder
        {
            set { this.FormBorderStyle = value; }
        }

        public bool ShowLoginButton
        {
            set { LoginToolStripButton.Visible = value; }
        }

        public bool ShowJustinRecordButton
        {
            set { JustinRecordtoolStripButton.Visible = value; }
        }

        public bool ShowVolumeControl
        {
            set 
            {
                showVolumeControl = value;
                volumeControl.Visible = value; 
            }

            get { return showVolumeControl; }
        }

        public bool ShowAddressBar
        {
            set { showAddressBar = value; }

            get { return showAddressBar; }
        }

        public string SetDocumentString
        {
            set
            {
                //documentLoaded_tour(value);
            }
        }

        public string SetTourListDocumentString
        {
            set
            {
                //documentLoaded_tourList(value);
            }
        }
        
        public MainForm()
        {
            InitializeComponent();

            try
            {
                useLatestIE();
                
                keepRunning_tour_Timer.Enabled = true;
                keepRunning_tour_Timer.Interval = 30000;//30 seconds
                keepRunning_tour_Timer.Tick += KeepRunning_tour_Timer_Tick;
                keepRunning_tour_Timer.Stop();

                documentLoaded_tour_Timer.Enabled = true;
                documentLoaded_tour_Timer.Interval = 10000;
                documentLoaded_tour_Timer.Tick += DocumentLoaded_tour_Timer_Tick;
                documentLoaded_tour_Timer.Stop();

                documentLoaded_tourList_Timer.Enabled = true;
                documentLoaded_tourList_Timer.Interval = 10000;
                documentLoaded_tourList_Timer.Tick += DocumentLoaded_tourList_Timer_Tick;
                documentLoaded_tourList_Timer.Stop();

                tabControlEx.Name = "tabControlEx";
                tabControlEx.SelectedIndex = 0;
                tabControlEx.Visible = false;
                tabControlEx.OnClose += new TabCtlEx.OnHeaderCloseDelegate(tabEx_OnClose);
                tabControlEx.VisibleChanged += new System.EventHandler(this.tabControlEx_VisibleChanged);

                this.panel1.Controls.Add(tabControlEx);
                tabControlEx.Dock = DockStyle.Fill;

                _windowManager = new WindowManager(tabControlEx);
                _windowManager.CommandStateChanged += new EventHandler<CommandStateEventArgs>(_windowManager_CommandStateChanged);
                _windowManager.StatusTextChanged += new EventHandler<TextChangedEventArgs>(_windowManager_StatusTextChanged);
                //_windowManager.DocumentCompleted += tour_DocumentCompleted;
                //_windowManager.ActiveBrowser.Navigating += ActiveBrowser_Navigating;
                //_windowManager.ActiveBrowser.ScriptErrorsSuppressed = true;
                _windowManager.ShowAddressBar = showAddressBar;
                
                showAddressBarToolStripMenuItem.Checked = showAddressBar;

                startTime = DateTime.Now;
                userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                initFormsConfigs();


                ////load blocked terms
                //loadBlockedTerms(blockedTermsPath);

                ////load blocked sites
                //loadBlockedSites(blockedSitesPath);

                ////load found blocked terms
                //loadFoundBlockedTerms(foundBlockedTermsPath);

                ////load found blocked sites
                //loadFoundBlockedSites(foundBlockedSitesPath);


                //getDefaultBrowser();
                
            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);
                //Application.Restart();
            }
        }

        private void StartTourTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!tourIsRunning && startTourUrlString.Trim().Length > 0)
                {
                    if (startTourUrlString.Length > 5)
                    {
                        tourIsRunning = true;
                        tourList.RemoveAt(0);
                        //tourBrowser.Url = new Uri(startTourUrlString);
                    }
                    
                    startTourUrlString = "";
                    documentLoaded_tour_Timer.Stop();
                    documentLoaded_tour_Timer.Tag = null;

                    txtRefreshTimer.Text = "0 seconds";
                }
            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);

                //Application.Restart();
            }
        }

        private void DocumentLoaded_tourList_Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                //documentLoaded_tourList_Timer.Stop();
                //documentLoaded_tourList_Timer.Tag = null;

                //bitVideoBrowser.Navigate("javascript: window.external.CallServerSideCode();");
            }
            catch (Exception ex)
            {
                //Application.Restart();
            }
        }

        private void DocumentLoaded_tour_Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                documentLoaded_tour_Timer.Stop();
                documentLoaded_tour_Timer.Tag = null;

                //if (tourBrowser != null && goToURLTimer.Tag == null)
                //{
                //    tourBrowser.Navigate("javascript: window.external.CallServerSideCode();");

                //    keepRunning_tour_Timer.Stop();
                //    keepRunning_tour_Timer.Tag = DateTime.Now;
                //    keepRunning_tour_Timer.Start();
                //}
            }
            catch (Exception ex)
            {
                //Application.Restart();
                string error = ex.Message;
            }            
        }
        
        private void KeepRunning_tour_Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (tourIsRunning)
                {
                    keepRunningTimerTicks++;

                    if (keepRunningTimerTicks >= 1)//30 seconds
                    {
                        //goToURLTimer.Stop();
                        //goToURLTimer.Tag = null;
                        documentLoaded_tourList_Timer.Stop();
                        documentLoaded_tourList_Timer.Tag = null;
                        documentLoaded_tour_Timer.Stop();
                        documentLoaded_tour_Timer.Tag = null;
                        //secondsTimer.Tag = null;
                        
                        string currentPageString = tourBrowser.Url.ToString().Substring(0,tourBrowser.Url.ToString().Length - 1);
                        currentPageString = currentPageString.Substring(currentPageString.LastIndexOf("/") + 1);
                        int.TryParse(currentPageString, out nextPageNumber);
                        nextPageNumber++;//add one to the current page number

                        if (keepRunningTimerTicks < 3)
                        {
                            string nextPageLink = tourBrowser.Url.ToString().Replace("/" + (nextPageNumber - 1).ToString() + "/", "/" + nextPageNumber + "/");

                            //if(nextPageLink != tourBrowser.Url.ToString())
                            //    goToURL(nextPageLink);
                        }
                        else
                        {
                            tourBrowser.Refresh();
                            keepRunningTimerTicks = 0;
                        }

                            //Application.Restart();
                            //tourBrowser.Refresh();//refresh page to get things going again
                            //keepRunning_tour_Timer.Stop();

                        }
                    //else if (keepRunningTimerTicks >= 7)//7 minutes
                    //    Application.Restart();
                }
                else
                {
                    keepRunning_tour_Timer.Stop();
                    keepRunningTimerTicks = 0;
                }
            }
            catch (Exception ex)
            {
                //Tools.WriteToFile(ex);

                //Application.Restart();
            }
        }

        // Starting the app here...
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Open a new browser window

                //hideMeBrowser = _windowManager.New(false);
                //hideMeBrowser.Url = new Uri("https://us.hideproxy.me/index.php");



                bitVideoBrowser = this._windowManager.New();
                bitVideoBrowser.DocumentCompleted += MainBrowser_DocumentCompleted;
                bitVideoBrowser.Url = new Uri("http://www.prizes-now.com/Home/");
                
                //bitVideoBrowser.StartNewWindow += BitVideoBrowser_StartNewWindow;

                try
                {
                    bitVideoBrowser.ObjectForScripting = new GetTourListDocumentString();
                }
                catch (Exception ex)
                {
                    throw;
                }

                refreshUtilities = new RefreshUtilities();
                refreshUtilities.ClickComplete += RefreshUtilities_ClickComplete;
            }
            catch (Exception ex)
            {
                Tools.WriteToFile(ex);
                //Application.Restart();
            }            
        }

        private void RefreshUtilities_ClickComplete(object sender, EventArgs e)
        {
            //if the sender is the quick pick button then find and click the submit button
            
            //if (((HtmlElement)sender).InnerText == "QUICK PICK")//this is the quick pick button - now click the submit button
            //{
            //    findSubmit(bitVideoBrowser.Document);
            //}
        }

        private void BitVideoBrowser_StartNewWindow(object sender, BrowserExtendedNavigatingEventArgs e)
        {
            if (tourBrowser == null || !tourBrowser.Created)
            {
                tourBrowser = this._windowManager.New();
                //tourBrowser.DocumentCompleted += tour_DocumentCompleted;
                tourBrowser.Downloading += TourBrowser_Downloading;
                tourBrowser.DownloadComplete += TourBrowser_DownloadComplete;
                tourBrowser.Navigating += TourBrowser_Navigating;
                tourBrowser.Url = e.Url;
                tourIsRunning = true;

                try
                {
                    tourBrowser.ObjectForScripting = new GetTourDocumentString();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        private void TourBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            lblDownloading.BackColor = Color.Green;
        }

        private void TourBrowser_DownloadComplete(object sender, EventArgs e)
        {
            lblStreaming.BackColor = Color.Red;
        }

        private void TourBrowser_Downloading(object sender, EventArgs e)
        {
            lblStreaming.BackColor = Color.Green;
        }

        private void Window_Error(object sender, HtmlElementErrorEventArgs e)
        {
            //Application.Restart();
        }
        
        private void ActiveBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            //documentString = "";
        }

        private void MainBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                documentString = _windowManager.ActiveBrowser.DocumentText;

                if (bitVideoBrowser.Url != null)
                {
                    if(documentString.Contains("data-target=\"#loginModal\" href=\"\">Login</a>"))//need to login
                    {
                        refreshUtilities.Cancel();
                        txtRefreshTimer.Text = "0 seconds";
                    }
                    else if (bitVideoBrowser.Url.ToString().ToLower().Contains("//www.prizes-now.com/home") || bitVideoBrowser.Url.ToString().ToLower().Contains("www.prizes-now.com/?"))
                    {
                        if (!foundQuickPick)
                        {
                            if (!findQuickPick(bitVideoBrowser.Document))
                            {
                                //if (!findNextContestLink(bitVideoBrowser.DocumentText) && refreshCount < 1)
                                //{
                                //    refreshCount++;
                                //    refreshUtilities.GoToURL(bitVideoBrowser.Url.ToString(), txtRefreshTimer, bitVideoBrowser);
                                //}
                            }
                        }
                        //else if (!foundSubmit)//we have found quickpick but not the submit button
                        //{
                        //    if (!findSubmit(bitVideoBrowser.Document) && refreshCount < 1)
                        //    {

                        //    }
                        //}
                        //else if (!foundNewContest)// we have found both quick pick and submit now we need to look for the next contest link
                        //{
                        //    if (!findNextContestLink(bitVideoBrowser.DocumentText) && refreshCount < 1)
                        //    {
                        //        refreshCount++;
                        //        refreshUtilities.GoToURL(bitVideoBrowser.Url.ToString(), txtRefreshTimer, bitVideoBrowser);
                        //    }
                        //}
                            //else if(!bitVideoBrowser.Url.ToString().ToLower().Contains("/index/") && !foundNewContest) //look for the link for the next contest
                            //    findNextContestLink(bitVideoBrowser.DocumentText);
                     }
                    else if(bitVideoBrowser.Url.ToString().ToLower().Contains("/enter/sweepstakes/qk/"))
                    {
                        findSubmit(bitVideoBrowser.Document);
                    }
                    else if (!bitVideoBrowser.Url.ToString().ToLower().Contains("/account") && bitVideoBrowser.Url.ToString().ToLower().Replace("http://www.prizes-now.com/","").Length > 2 && !foundNewContest) //look for the link for the next contest
                        findNextContestLink(bitVideoBrowser.DocumentText);
                }
            }
            catch (Exception ex)
            {
                throw;
                //Application.Restart();
            }
        }
        
        private bool findQuickPick(HtmlDocument pageDocument)
        {
            if (!foundQuickPick)
            {
                foundNewContest = false;
                foundSubmit = false;

                HtmlElementCollection elc = pageDocument.GetElementsByTagName("button");

                foreach (HtmlElement el in elc)
                {
                    if (el.InnerText != null && el.InnerText == "QUICK PICK")
                    {
                        refreshUtilities.ClickElement(el, txtRefreshTimer);
                        foundQuickPick = true;
                        refreshCount = 0;

                        return true;
                    }
                }

                //elc = pageDocument.GetElementsByTagName("a");

                //foreach (HtmlElement el in elc)
                //{
                //    if (el.GetAttribute("href") == "javascript:quickPicks()")
                //    {
                //        refreshUtilities.ClickElement(el, txtRefreshTimer);
                //        foundQuickPick = true;
                //        refreshCount = 0;

                //        return true;
                //    }
                //}

                //elc = pageDocument.GetElementsByTagName("img");

                //foreach (HtmlElement el in elc)
                //{
                //    if (el.GetAttribute("src").Contains("/images/bonusgame/button_bonusgame_autopick_on.png"))
                //    {
                //        refreshUtilities.ClickElement(el, txtRefreshTimer);
                //        foundQuickPick = true;
                //        refreshCount = 0;

                //        return true;
                //    }
                //}

                //refreshUtilities.GoToURL("javascript:quickPicks()", txtRefreshTimer, bitVideoBrowser);
                //foundQuickPick = true;
                //refreshCount = 0;

                //return true;
            }

            return false;
        }

        private bool findSubmit(HtmlDocument pageDocument)
        {
            HtmlElementCollection elc = pageDocument.GetElementsByTagName("button");

            foreach (HtmlElement el in elc)
            {
                //if(el.OuterHtml != null && el.OuterHtml.Contains("class=\"hidden-xs partnersite-submit subSweep\"") && el.InnerText == null && el.InnerHtml == null)//this is the submit button - click it
                if (el.OuterHtml != null && el.OuterHtml.Contains("submitSweep"))//this is the submit button - click it
                {
                    refreshUtilities.ClickElement(el, txtRefreshTimer);
                    foundSubmit = true;
                    refreshCount = 0;

                    return true;
                }
            }

            //elc = pageDocument.GetElementsByTagName("img");

            //foreach (HtmlElement el in elc)
            //{
            //    if (el.GetAttribute("src").Contains("/images/bonusgame/button_bonusgame_go_on.png"))
            //    {
            //        refreshUtilities.ClickElement(el, txtRefreshTimer);
            //        foundQuickPick = true;
            //        refreshCount = 0;

            //        return true;
            //    }
            //}

            return false;
        }

        private bool findNextContestLink(string pageContent)
        {
            foundQuickPick = false;
            foundSubmit = false;
            //string nextContestLink = findValue(pageContent, "window.location='/DailyDraw/Index", "'");
            //string nextContestLink = findValue(pageContent, "<a href=\"/NextGame/", "\"");

            //if (nextContestLink.Trim().Length > 0)
            //{
            //    nextContestLink = "http://www.prizes-now.com/NextGame/" + nextContestLink;

            foundNewContest = true;

            string nextContestLink = "http://www.prizes-now.com/Home";

            refreshUtilities.GoToURL(nextContestLink, txtRefreshTimer, bitVideoBrowser);

            return true;
            //}
            ////else
            ////    MessageBox.Show("didn't find any more contests");

            //return false;
        }

        private void initFormsConfigs()
        {
            SettingsHelper helper = SettingsHelper.Current;

            checkForms = helper.CheckForms;
        }

        private void useLatestIE()
        {
            try
            {
                string AppName = Application.ProductName;// My.Application.Info.AssemblyName
                int VersionCode = 0;
                string Version = "";
                object ieVersion = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer").GetValue("svcUpdateVersion");

                if (ieVersion == null)
                    ieVersion = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer").GetValue("Version");

                if (ieVersion != null)
                {
                    Version = ieVersion.ToString().Substring(0, ieVersion.ToString().IndexOf("."));
                    switch (Version)
                    {
                        case "7":
                            VersionCode = 7000;
                            break;
                        case "8":
                            VersionCode = 8888;
                            break;
                        case "9":
                            VersionCode = 9999;
                            break;
                        case "10":
                            VersionCode = 10001;
                            break;
                        default:
                            if (int.Parse(Version) >= 11)
                                VersionCode = 11001;
                            else
                                Tools.WriteToFile(Tools.errorFile, "useLatestIE error: IE Version not supported");
                            break;
                    }
                }
                else
                {
                    Tools.WriteToFile(Tools.errorFile, "useLatestIE error: Registry error");
                }

                //'Check if the right emulation is set
                //'if not, Set Emulation to highest level possible on the user machine
                string Root = "HKEY_CURRENT_USER\\";
                string Key = "Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION";
                
                object CurrentSetting = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Key).GetValue(AppName + ".exe");

                if (CurrentSetting == null || int.Parse(CurrentSetting.ToString()) != VersionCode)
                {
                    Microsoft.Win32.Registry.SetValue(Root + Key, AppName + ".exe", VersionCode);
                    Microsoft.Win32.Registry.SetValue(Root + Key, AppName + ".vshost.exe", VersionCode);
                }
            }
            catch (Exception ex)
            {
                Tools.WriteToFile(Tools.errorFile, "useLatestIE error: "+ ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        // Update the status text
        void _windowManager_StatusTextChanged(object sender, TextChangedEventArgs e)
        {
            this.toolStripStatusLabel.Text = e.Text;
        }

        // Enable / disable buttons
        void _windowManager_CommandStateChanged(object sender, CommandStateEventArgs e)
        {
            this.forwardToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Forward) == BrowserCommands.Forward);
            this.backToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Back) == BrowserCommands.Back);
            this.printPreviewToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.PrintPreview) == BrowserCommands.PrintPreview);
            this.printPreviewToolStripMenuItem.Enabled = ((e.BrowserCommands & BrowserCommands.PrintPreview) == BrowserCommands.PrintPreview);
            this.printToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Print) == BrowserCommands.Print);
            this.printToolStripMenuItem.Enabled = ((e.BrowserCommands & BrowserCommands.Print) == BrowserCommands.Print);
            this.homeToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Home) == BrowserCommands.Home);
            this.searchToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Search) == BrowserCommands.Search);
            this.refreshToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Reload) == BrowserCommands.Reload);
            this.stopToolStripButton.Enabled = ((e.BrowserCommands & BrowserCommands.Stop) == BrowserCommands.Stop);
        }

        #region Tools menu
        // Executed when the user clicks on Tools -> Options
        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OptionsForm of = new OptionsForm())
            {
                of.ShowDialog(this);
            }
        }

        // Tools -> Show script errors
        private void scriptErrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptErrorManager.Instance.ShowWindow();
        }

        //login to be able to access/modify blockedTerms file
        private void loginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Login login = new Login())
            {
                login.ShowDialog(this);
                if (login.DialogResult == DialogResult.Yes)
                {
                    LoggedIn = true;
                    adminLock = false;
                }
                else if (login.DialogResult == DialogResult.None)
                    adminLock = true;
                else
                    LoggedIn = false;
            }
        }

        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoggedIn = false;
        }

        private void UpdateLoginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Login login = new Login())
            {
                login.Update = true;
                login.ShowDialog(this);
            }
        }

        private void modifyBlockedTermsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //display terms
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();

            tcAdmin.SelectedTab = tcAdmin.TabPages["tpChangeLoginInfo"];
        }

        private void modifyBlockedSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpBlockedSites"];
        }

        private void foundBlockedTermsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpFoundBlockedTerms"];
        }

        private void foundBlockedSitesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcAdmin.Visible = true;
            tcAdmin.BringToFront();
            tcAdmin.SelectedTab = tcAdmin.TabPages["tpFoundBlockedSites"];
        }
        #endregion

        #region File Menu

        // File -> Print
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Print();
        }

        // File -> Print Preview
        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintPreview();
        }

        // File -> Exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // File -> Open URL
        private void openUrlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenUrlForm ouf = new OpenUrlForm())
            {
                if (ouf.ShowDialog() == DialogResult.OK)
                {
                    ExtendedWebBrowser brw = _windowManager.New(false);
                    brw.Navigate(ouf.Url);
                }
            }
        }

        // File -> Open File
        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = Properties.Resources.OpenFileDialogFilter;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Uri url = new Uri(ofd.FileName);
                    WindowManager.Open(url);
                }
            }
        }
        #endregion

        #region Help Menu

        // Executed when the user clicks on Help -> About
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About();
        }

        /// <summary>
        /// Shows the AboutForm
        /// </summary>
        private void About()
        {
            using (AboutForm af = new AboutForm())
            {
                af.ShowDialog(this);
            }
        }

        #endregion

        /// <summary>
        /// The WindowManager class
        /// </summary>
        public WindowManager _windowManager;

        // This is handy when all the tabs are closed.
        private void tabControlEx_VisibleChanged(object sender, EventArgs e)
        {
            if (tabControlEx.Visible)
            {
                this.panel1.BackColor = SystemColors.Control;
            }
            else
                this.panel1.BackColor = SystemColors.AppWorkspace;
        }

        #region Printing & Print Preview
        private void Print()
        {
            ExtendedWebBrowser brw = _windowManager.ActiveBrowser;
            if (brw != null)
                brw.ShowPrintDialog();
        }

        private void PrintPreview()
        {
            ExtendedWebBrowser brw = _windowManager.ActiveBrowser;
            if (brw != null)
                brw.ShowPrintPreviewDialog();
        }
        #endregion

        #region Toolstrip buttons
        private void openWindowToolStripButton_Click(object sender, EventArgs e)
        {
            ExtendedWebBrowser newBrowser = this._windowManager.New();

            newBrowser.ObjectForScripting = new GetTourDocumentString();
        }

        private void closeToolStripButton_Click(object sender, EventArgs e)
        {
            //closes browser window
            //this._windowManager.Close();

            //closes admin tabPages
            tcAdmin.Visible = false;
        }

        private void tabEx_OnClose(object sender, CloseEventArgs e)
        {
            //this.userControl11.Controls.Remove(this.userControl11.TabPages[e.TabIndex]);

            //closes browser window
            this._windowManager.Close();
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            Print();
        }

        private void printPreviewToolStripButton_Click(object sender, EventArgs e)
        {
            PrintPreview();
        }

        private void backToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.CanGoBack)
                _windowManager.ActiveBrowser.GoBack();
        }

        private void forwardToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.CanGoForward)
                _windowManager.ActiveBrowser.GoForward();
        }

        private void stopToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
            {
                _windowManager.ActiveBrowser.Stop();
            }
            stopToolStripButton.Enabled = false;
        }

        private void refreshToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
            {
                _windowManager.ActiveBrowser.Refresh(WebBrowserRefreshOption.Normal);
            }
        }

        private void homeToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
                _windowManager.ActiveBrowser.GoHome();
        }

        private void searchToolStripButton_Click(object sender, EventArgs e)
        {
            if (_windowManager.ActiveBrowser != null)
                _windowManager.ActiveBrowser.GoSearch();
        }

        #endregion

        public WindowManager WindowManager
        {
            get { return _windowManager; }
        }

        /// <summary>
        /// load blocked terms from file
        /// </summary>
        /// <param name="path"></param>
        public void loadBlockedTerms(string path)
        {
            blockedTerms = File.ReadAllLines(path);

            if (!validateBlockedTerms())
            {
                //decrypt terms
                blockedTerms = Encryption.Decrypt(blockedTerms);
            }

            if (!validateBlockedTerms())
            {
                //log that terms have been tampered with
                log(blockedTermsPath, "Blocked Terms file has been tampered with.  Reinstall SafeSurf");
                //block all pages
                adminLock = true;
            }

            dgBlockedTerms.Dock = DockStyle.Fill;
            dgBlockedTerms.Anchor = AnchorStyles.Right;
            dgBlockedTerms.Anchor = AnchorStyles.Bottom;
            dgBlockedTerms.Anchor = AnchorStyles.Left;
            dgBlockedTerms.Anchor = AnchorStyles.Top;
            dgBlockedTerms.Columns.Add("Terms", "Terms");
            dgBlockedTerms.Refresh();

            foreach (string term in blockedTerms)
            {
                dgBlockedTerms.Rows.Add(new string[] { term });
            }
        }

        private void loadBlockedSites(string path)
        {
            blockedSites = File.ReadAllLines(path);

            if (!validateBlockedSites())
            {
                //decrypt terms
                blockedSites = Encryption.Decrypt(blockedSites);
            }

            if (!validateBlockedSites())
            {
                //log that terms have been tampered with
                log(blockedSitesPath, "Blocked Sites file has been tampered with.  Reinstall SafeSurf");
                //block all pages
                adminLock = true;
            }

            dgBlockedSites.Dock = DockStyle.Fill;
            dgBlockedSites.Anchor = AnchorStyles.Right;
            dgBlockedSites.Anchor = AnchorStyles.Bottom;
            dgBlockedSites.Anchor = AnchorStyles.Left;
            dgBlockedSites.Anchor = AnchorStyles.Top;
            dgBlockedSites.Columns.Add("Sites", "Sites");

            foreach (string site in blockedSites)
            {
                dgBlockedSites.Rows.Add(new string[] { site });
            }
        }

        public void loadFoundBlockedTerms(string path)
        {
            string fBlockedTerms = "";

            if (File.Exists(path))
                foundBlockedTerms = File.ReadAllLines(path);

            if (foundBlockedTerms != null && foundBlockedTerms.Length > 0)
            {
                //if (!validateFoundBlockedTerms())
                //{
                //decrypt terms
                foundBlockedTerms = Encryption.Decrypt(foundBlockedTerms);
                //}

                if (!validateBlockedTerms())
                {
                    //log that terms have been tampered with
                    log(foundBlockedTermsPath, "Found Blocked Terms file has been tampered with.");
                    //block all pages
                    adminLock = true;
                }

                lbFoundBlockedTerms.DataSource = foundBlockedTerms;
            }
        }

        public void loadFoundBlockedSites(string path)
        {
            if (File.Exists(path))
                foundBlockedSites = File.ReadAllLines(path);

            if (foundBlockedSites != null && foundBlockedSites.Length > 0)
            {

                //if (!validateBlockedTerms())
                //{
                //decrypt terms
                foundBlockedSites = Encryption.Decrypt(foundBlockedSites);
                //}

                //if (!validateBlockedTerms())
                //{
                //    //log that terms have been tampered with
                //    log(blockedTermsPath, "Blocked Terms file has been tampered with.  Reinstall SafeSurf");
                //    //block all pages
                //    adminLock = true;
                //}

                lbFoundBlockedSites.DataSource = foundBlockedSites;
            }
        }

        private bool validateBlockedTerms()
        {
            bool isValid = false;

            foreach (string term in blockedTerms)
            {
                if (term.ToLower() == "fuck")
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        private bool validateBlockedSites()
        {
            bool isValid = false;

            foreach (string site in blockedSites)
            {
                if (site.ToLower() == "pussy.org")
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        private bool validateFoundBlockedTerms()
        {
            bool isValid = true;

            //foreach (string term in foundBlockedTerms)
            //{
            //    if (term.ToLower().Contains("fuck"))
            //    {
            //        isValid = true;
            //        break;
            //    }
            //}

            return isValid;
        }

        #region datagridview events
        private void dgBlockedTerms_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            //make sure values are valid
            //DataGridView dg = (DataGridView)sender;

        }

        private void dgBlockedTerms_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                //update blocked terms file
                ArrayList terms = new ArrayList();
                string value = "";
                DataGridView dg = (DataGridView)sender;
                foreach (DataGridViewRow row in dg.Rows)
                {
                    value = Convert.ToString(row.Cells["Terms"].Value);
                    if (value != null && value.Trim().Length > 0)
                        terms.Add(value);
                }

                blockedTerms = (string[])terms.ToArray(typeof(string));

                //encrypt
                blockedTerms = Encryption.Encrypt(blockedTerms);

                //save blockedTerms
                File.WriteAllLines(blockedTermsPath, blockedTerms);
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        private void logHeader(string path)
        {
            if (startTime.CompareTo(File.GetLastWriteTime(path)) == 1)
            {
                StringBuilder content = new StringBuilder();

                content.AppendLine();
                content.AppendLine("User: " + userName + "  Start Time: " + startTime);

                File.AppendAllText(path, Encryption.Encrypt(content.ToString()));
            }
        }

        public void log(string path, string content)
        {
            logHeader(path);

            File.AppendAllText(path, content);
        }

        public void log(string path, string[] content)
        {
            logHeader(path);

            File.WriteAllLines(path, content);
            //File.WriteAllText(path, content);
        }

        private void tcAdmin_VisibleChanged(object sender, EventArgs e)
        {
            closeToolStripButton.Visible = true;
        }

        private void loginTimer_Tick(object sender, EventArgs e)
        {
            loggedInTime++;

            if (loggedInTime > loginMaxTime)
            {
                loginTimer.Enabled = false;
                LoggedIn = false;
            }
        }

        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            string[] loginInfo = { "username:" + txtNewUserName.Text.Trim(), "password:" + txtNewPassword.Text.Trim() };
            loginInfo = Encryption.Encrypt(loginInfo);
            File.WriteAllLines(MainForm.loginInfoPath, loginInfo);
            lblLoginInfoUpdated.Visible = true;
        }

        private void tpChangeLoginInfo_Leave(object sender, EventArgs e)
        {
            lblLoginInfoUpdated.Visible = false;
        }

        private string getDefaultBrowser()
        {
            //original value on classesroot
            //"C:\Program Files\Internet Explorer\IEXPLORE.EXE" -nohome

            string browser = string.Empty;
            RegistryKey key = null;
            try
            {
                key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command",true);

                //trim off quotes
                //browser = key.GetValue(null).ToString().Replace("\"", "");
                //if (!browser.EndsWith(".exe"))
                //{
                //    //get rid of everything after the ".exe"
                //    browser = browser.Substring(0, browser.ToLower().LastIndexOf(".exe") + 4);
                //}

                browser = key.GetValue(null).ToString();
                
                //key.SetValue(null, (string)@browser);

                string safeSurfBrowser = "\""+ Application.ExecutablePath +"\"";

                key.SetValue(null, (string)@safeSurfBrowser);
            }
            finally
            {
                if (key != null) key.Close();
            }
            return browser;
        }

        private void JustinRecordtoolStripButton_Click(object sender, EventArgs e)
        {
            //need to get channel name from url
            string[] urlSegments = _windowManager.ActiveBrowser.Url.Segments;

            if (urlSegments[1].ToLower() != "directory")//this is a channel
            {
                string channelName = urlSegments[1];
                DialogResult result = MessageBox.Show("Are you sure you want to download from " + channelName, "Download " + channelName, MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    //pop up justin downloader and start downloading
                    //SCTVJustinTV.Downloader downloader = new SCTVJustinTV.Downloader(channelName, "12", Application.StartupPath + "\\JustinDownloads\\");
                    //SCTVJustinTV.Downloader downloader = new SCTVJustinTV.Downloader();
                    //downloader.Channel = channelName;
                    //downloader.Show();
                }
            }
            else
                MessageBox.Show("You must be watching the channel you want to record");
        }

        private void toolStripButtonFavorites_Click(object sender, EventArgs e)
        {
            string url = "";

            //check for url
            if (_windowManager.ActiveBrowser != null && _windowManager.ActiveBrowser.Url.PathAndQuery.Length > 0)
            {
                url = _windowManager.ActiveBrowser.Url.PathAndQuery;

                //add to onlineMedia.xml
                //SCTVObjects.MediaHandler.AddOnlineMedia(_windowManager.ActiveBrowser.Url.Host, _windowManager.ActiveBrowser.Url.PathAndQuery, "Online", "Favorites", "", "");
            }
            else
                MessageBox.Show("You must browse to a website to add it to your favorites");
        }

        private void showAddressBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _windowManager.ShowAddressBar = showAddressBarToolStripMenuItem.Checked;

            showAddressBarToolStripMenuItem.Checked = !showAddressBarToolStripMenuItem.Checked;
        }

        private string findValue(string stringToParse, string startPattern, string endPattern)
        {
            return findValue(stringToParse, startPattern, endPattern, false);
        }

        private string findValue(string stringToParse, string startPattern, string endPattern, bool returnSearchPatterns)
        {
            int start = 0;
            int end = 0;
            string foundValue = "";

            try
            {
                start = stringToParse.IndexOf(startPattern);

                if (start > -1)
                {
                    if (!returnSearchPatterns)
                        stringToParse = stringToParse.Substring(start + startPattern.Length);
                    else
                        stringToParse = stringToParse.Substring(start);

                    end = stringToParse.IndexOf(endPattern);

                    if (end > 0)
                    {
                        if (returnSearchPatterns)
                            foundValue = stringToParse.Substring(0, end + endPattern.Length);
                        else
                            foundValue = stringToParse.Substring(0, end);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
                //Tools.WriteToFile(ex);
            }

            return foundValue;
        }

        /// <summary>
        /// submit numbers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            foundSubmit = false;

            findSubmit(bitVideoBrowser.Document);
        }

        private void btnInstaGC_Click(object sender, EventArgs e)
        {
            //bitVideoBrowser.Url = new Uri("https://www.instagc.com/earn/offertoro/clicks");
        }

        /// <summary>
        /// quick pick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFaucetHub_Click(object sender, EventArgs e)
        {
            foundQuickPick = false;
            foundSubmit = false;

            findQuickPick(bitVideoBrowser.Document);
        }

        private void chbAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            if (!chbAutoRefresh.Checked)
            {
                //secondsTimer.Tag = null;
                //secondsTimer.Stop();

                txtRefreshTimer.Text = "0 seconds";
            }
        }

        [ComVisible(true)]
        public class GetTourDocumentString
        {
            public void CallServerSideCode()
            {
                try
                {
                    MainForm currentForm = ((MainForm)Application.OpenForms[0]);

                    var doc = currentForm.tourBrowser.Document;

                    var renderedHtml = doc.GetElementsByTagName("HTML")[0].OuterHtml;

                    currentForm.SetDocumentString = renderedHtml;
                }
                catch (Exception ex)
                {
                    //Application.Restart();
                }
            }
        }

        [ComVisible(true)]
        public class GetTourListDocumentString
        {
            public void CallServerSideCode()
            {
                try
                {
                    MainForm currentForm = ((MainForm)Application.OpenForms[0]);

                    var doc = currentForm.bitVideoBrowser.Document;

                    var renderedHtml = doc.GetElementsByTagName("HTML")[0].OuterHtml;

                    currentForm.SetTourListDocumentString = renderedHtml;
                }
                catch (Exception ex)
                {
                    //Application.Restart();
                }
            }
        }

        private void btnLoadSource_Click(object sender, EventArgs e)
        {
            tourIsRunning = false;
            tourList.Clear();
            int.TryParse(txtStartingTourNum.Text, out currentTourIndex);

            currentTourIndex--;//get the index not the number

            if (currentTourIndex < 1)
                currentTourIndex = 1;

            //bitVideoBrowser.Navigate("javascript: window.external.CallServerSideCode();");
            string tempTourList = "";
            tempTourList = File.ReadAllText(Application.StartupPath + "\\tourlist.html");

            //documentLoaded_tourList(tempTourList);
        }

        private void btnCheckForButton_Click(object sender, EventArgs e)
        {
            tourBrowser.Navigate("javascript: window.external.CallServerSideCode();");
        }
    }
}