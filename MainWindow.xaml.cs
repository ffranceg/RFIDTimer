using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.OleDb;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ThingMagic;
using System.Xml.Linq;
using static ThingMagic.Gen2.Untraceable;
using System.Data.SQLite;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.ObjectModel;
using System.Windows.Markup;
using System.Management.Instrumentation;

namespace RFIDTimer
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields

        // Define a reader variable
        Reader objReader = null;

        // Tag database object
        TagDatabase tagdb = new TagDatabase();
        TagReadRecordBindingList _tagList = new TagReadRecordBindingList();
        static Hashtable SeenTags = new Hashtable(); //controllo doppi EPG

        // This flag is used to synchronize all "reader disconnection" exception messages
        static bool isReaderConnected = false;
        static bool isRaceStarted = false;
        private bool isAsyncReadGoingOn = false; // Cache async read progress state
        private bool isSyncReadGoingOn = false;  // Cache sync read progress state

        bool enableUnique = false;
        bool enableFailData = false;

        // Define a region variable
        Reader.Region regionToSet = new Reader.Region();

        // To re read all the tags from the database and clear the memory
        DispatcherTimer dispatchtimer = null;
        System.Timers.Timer myTimer = new System.Timers.Timer();

        DispatcherTimer dtRaceTimeClock = new DispatcherTimer();
        public static Stopwatch raceTimeClock = new Stopwatch();
        public static DateTime TimeStartRace;
        public static DateTime TimeStopRace;

        // Delegates 
        delegate void del();
        private delegate void EmptyDelegate();

        //MACROS
        public int MAXVALUE = 65535;
        public int DEFUALTTIME = 500;
        public int TIMEFORCONDITION = 1000;

        // Stores Detected comport
        List<string> masterPortList = new List<string>();

        // sounds
        SoundPlayer beepOK = new SoundPlayer("beep-ok.wav");
        SoundPlayer beepWrong = new SoundPlayer("beep-ok.wav");

        //private string CNS_Access = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "\\Crono.accdb;Persist Security Info=True;";
        //public string CNS_SQLite = "Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "\\Data\\RFIDTimer.db;Version=3;";

        public static MainWindow mainw = null;

        List<EventModel> eventsDetails = new List<EventModel>();

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        public MainWindow()
        {
            try
            {
                string[] titleVer = (this.GetType().Assembly.GetName().Version.ToString()).Split('.').ToArray();
                Title = "RFID Timer " + titleVer[0] + "." + titleVer[1] + "." + titleVer[2] + "." + titleVer[3];

                // Ending the session when Application abruptly shuts down implicitly or explicitly
                SystemEvents.SessionEnding += (o, e) =>
                {
                    // releasing the reader resource.
                    if (null != objReader)
                    {
                        objReader.Destroy();
                        objReader = null;
                    }
                    // Closing application programatically
                    Environment.Exit(1);
                };

                // WPF draws the screen at a continuous pace of 60 frames per second rate by default. 
                // So if we are using lots of graphics and images, application will eventually take a 
                // lots CPU utilization because of these frame rates. Hence reducing the frame-rates to 
                // 10 frames per second
                Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 10 });

                InitializeComponent();
                GenerateColmnsForDataGrid();
                InitializeReaderUriBox();
                mainw = this;
                
                dispatchtimer = new DispatcherTimer();
                dispatchtimer.Interval = TimeSpan.FromMilliseconds(50);
                dispatchtimer.Tick += new EventHandler(dispatchtimer_Tick);

                dtRaceTimeClock.Tick += new EventHandler(RaceTime_Tick);
                dtRaceTimeClock.Interval = new TimeSpan(0, 0, 0, 0, 10);

            }
            catch (Exception bonjEX)
            {
                Mouse.SetCursor(Cursors.Arrow);
                if (-1 != bonjEX.Message.IndexOf("80040154 Class not registered"))
                {
                    //if (rdbtnNetworkConnection.IsChecked == true)
                    //  btnRefreshReadersList.Visibility = System.Windows.Visibility.Collapsed;

                    //do nothing
                }
                else
                {
                    if (bonjEX.Message.Contains("0x80004005"))
                    {
                        //if (rdbtnNetworkConnection.IsChecked == true)
                        //  btnRefreshReadersList.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        MessageBox.Show(bonjEX.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Connect button event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //isReaderConnected
        //isRaceStarted
        //if (isAsyncReadGoingOn || isSyncReadGoingOn)
        //

        private void btnConnectAction_Click(object sender, RoutedEventArgs e)
        {
            if (isReaderConnected)
            { //inizio controlli per disconnect
                if (isRaceStarted || isAsyncReadGoingOn || isSyncReadGoingOn)
                {
                    string msg = "Race or Read is in progress , Do you want to stop the reading and disconnect the reader?";
                    switch (MessageBox.Show(msg, "RFIDTimer", MessageBoxButton.OKCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.OK:
                        {
                            btnRead_Click(sender, e);
                            closeConnectURA(sender, e);
                            break;
                        }
                        case MessageBoxResult.Cancel:
                        {
                            break;
                        }
                    }
                }
                else
                {
                    //disconnect reader
                    closeConnectURA(sender, e);
                }
            }
            else
            {
                //connect reader
                openConnectURA(sender, e);
            }
        }

        /// <summary>
        /// Open Connection to Reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openConnectURA(object sender, RoutedEventArgs e)
        {
            if (!isReaderConnected)
            {
                Mouse.SetCursor(Cursors.AppStarting);
                tagdb.Clear();
                try
                {
                    if (!ValidatePortNumber(cmbReaderAddr.Text))
                    {
                        throw new IOException();
                    }
                    if (cmbReaderAddr.Text == "")
                    {
                        throw new IOException();
                    }
                    // Creates a Reader Object for operations on the Reader.
                    string readerUri = cmbReaderAddr.Text;
                    //Regular Expression to get the com port number from comport name .
                    //for Ex: If The Comport name is "USB Serial Port (COM19)" by using this 
                    // regular expression will get com port number as "COM19".
                    MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                    foreach (Match m in mc)
                    {
                        if (!string.IsNullOrWhiteSpace(m.ToString()))
                            readerUri = m.ToString();
                    }
                    objReader = Reader.Create(string.Concat("tmr:///", readerUri));

                    objReader.Connect();

                    //Uncomment this line to add default transport listener.
                    //objReader.Transport += objReader.SimpleTransportListener;

                    //Show the status
                    lblshowStatus.Content = "Connected";
                    isReaderConnected = true;
                    imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png", UriKind.RelativeOrAbsolute));
                    imgbtnConnect.Source = new BitmapImage(new Uri(@"..\Icons\switch-on.png", UriKind.RelativeOrAbsolute));
                    lblReaderUri.Content = objReader.ParamGet("/reader/version/model");

                    // Create a simplereadplan which uses the antenna list created above
                    int[] antennaList = { 1 };
                    SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);

                    // Set the created readplan
                    objReader.ParamSet("/reader/read/plan", plan);
                    objReader.ParamSet("/reader/radio/readPower", 500);
                    objReader.ParamSet("/reader/radio/writePower", 500);

                    if (objReader is SerialReader)
                    {
                        SerialReader reader = objReader as SerialReader;
                        objReader.ParamSet("/reader/stats/enable", Reader.Stat.StatsFlag.TEMPERATURE);
                        objReader.StatsListener += new EventHandler<StatsReportEventArgs>(PrintTemperature);
                    }
                    // Create reader information
                    mainw.TextBox1.AppendText(DateTime.Now.ToString("G") + " Hardware " + objReader.ParamGet("/reader/version/hardware") + Environment.NewLine);
                    mainw.TextBox1.AppendText(DateTime.Now.ToString("G") + " Serial " + objReader.ParamGet("/reader/version/serial") + Environment.NewLine);
                    mainw.TextBox1.AppendText(DateTime.Now.ToString("G") + " Model " + objReader.ParamGet("/reader/version/model") + Environment.NewLine);

                    lblPowerdBm.Content = objReader.ParamGet("/reader/radio/readPower");
                    //lblPowerdBm.Content = objReader.ParamGet("/reader/radio/writePower");

                    dgTagResults.CancelEdit();
                    dgTagResults.ItemsSource = null;
                    dgTagResults.ItemsSource = tagdb.TagList;
                    btnClearTagReads.IsEnabled = true;


                    // Create and add tag listener
                    //objReader.TagRead += new EventHandler<TagReadDataEventArgs>(PrintDistinctTagRead);
                    objReader.TagRead += new EventHandler<TagReadDataEventArgs>(PrintTagRead);
                    objReader.ReadException += new EventHandler<ReaderExceptionEventArgs>(r_ReadException);

                }
                catch (Exception ex)
                {
                    string error = "Connect failed to saved reader URI [" + "uri" + "]. Please make sure reader "
                        + " is connected and try again. Or, connect to desired reader first then load any saved "
                        + "configuration to it";
                    Mouse.SetCursor(Cursors.Arrow);
                    //btnConnect.IsEnabled = true;
                    lblshowStatus.Content = "Disconnected";
                    if (ex is IOException)
                    {
                        MessageBox.Show("Application needs a valid Reader Address of type COMx", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (ex is ReaderException)
                    {
                        if (-1 != ex.Message.IndexOf("target machine actively refused"))
                        {
                            MessageBox.Show("Error connecting to reader: " + "Connection attempt failed...", "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (ex is FAULT_BL_INVALID_IMAGE_CRC_Exception || ex is FAULT_BL_INVALID_APP_END_ADDR_Exception)
                        {
                            MessageBox.Show("Error connecting to reader: " + ex.Message + ". Please update the module firmware.", "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show("Error connecting to reader: " + ex.Message, "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (ex is UnauthorizedAccessException)
                    {
                        MessageBox.Show("Access to " + "COM port" + " denied. Please check if another " + "program is accessing this port", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        if (-1 != ex.Message.IndexOf("target machine actively refused"))
                        {
                            MessageBox.Show("Error connecting to reader: " + "Connection attempt failed...",
                                "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show("Error connecting to reader: " + ex.Message, "Reader Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    if (null != objReader)
                    {
                        objReader.Destroy();
                        objReader = null;
                    }
                }
            }
            else
            {
                closeConnectURA(sender, e);
            }
        }

        /// <summary>
        /// Close Connection to Reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeConnectURA(object sender, RoutedEventArgs e)
        {
            if (isReaderConnected)
            {
                //SaveConfigurationForWizardFlow();
                //tmrLogStopReader();

                if (objReader != null)
                {
                    objReader.TagRead -= PrintTagRead;
                    objReader.ReadException -= r_ReadException;
                    objReader.Destroy();
                    objReader = null;
                    //Show the status
                    lblshowStatus.Content = "Disconneted";
                    lblReaderUri.Content = "N/D";
                    isReaderConnected = false;
                    imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedRed.png", UriKind.RelativeOrAbsolute));
                    imgbtnConnect.Source = new BitmapImage(new Uri(@"..\Icons\switch-off.png", UriKind.RelativeOrAbsolute));
                }
            }
            else
            {
                lblshowStatus.Content = "URA is not Connected";
            }
        }

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            if (isReaderConnected)
            {
                if (isAsyncReadGoingOn)
                {
                    objReader.StopReading();
                    isAsyncReadGoingOn = false;
                    imgRFIDActive.Source = new BitmapImage(new Uri(@"..\Icons\rfid_off.png", UriKind.RelativeOrAbsolute));
                    lblbtnRead.Content = "Switch on RFID";
                    dispatchtimer.Stop();
                    TextBox1.AppendText(DateTime.Now.ToString("G") + "---------- Stop Reading" + Environment.NewLine);
                }
                else
                {
                    if (isRaceStarted)
                    {
                        // Search for tags in the background
                        imgRFIDActive.Source = new BitmapImage(new Uri(@"..\Icons\rfid_on.png", UriKind.RelativeOrAbsolute));
                        lblbtnRead.Content = "Switch off RFID";
                        TextBox1.AppendText(DateTime.Now.ToString("G") + "---------- Start Reading" + Environment.NewLine);

                        // Start timer to render data on the grid and calculate read rate
                        dispatchtimer.Start();

                        isAsyncReadGoingOn = true;
                        objReader.StartReading();
                    }
                    else
                    {
                        //MessageBox.Show("Start Race First","RFIDTimer Alert");
                    }
                }
            }
            else
            {
                imgRFIDActive.Source = new BitmapImage(new Uri(@"..\Icons\rfid_off.png", UriKind.RelativeOrAbsolute));
                TextBox1.AppendText(DateTime.Now.ToString("G") + "---------- Readear is not connect" + Environment.NewLine);
            }
        }

        /// <summary>
        /// Function that processes the Tag Data produced by StartReading();
        /// </summary>
        /// <param name="read"></param>
        void PrintTagRead(Object sender, TagReadDataEventArgs e)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate ()
            {
                // Enable the read/stop-reading button when URA is able to connect 
                // to the reader or URA is able to get the tags.
                btnRead.IsEnabled = true;

                lock (SeenTags.SyncRoot)
                {
                    try
                    {
                        TagReadData t = e.TagReadData;
                        string epc = t.EpcString;
                        if (!SeenTags.ContainsKey(epc))
                        {
                            SeenTags.Add(epc, null);
                            tagdb.Add(e.TagReadData);
                            insertDBTime(epc, DateTime.Now, DateTime.Now - MainWindow.TimeStartRace);
                            dgTagResults.Items.Refresh();
                            //TextBox1.AppendText(DateTime.Now.ToString("G") + " Background read: " + e.TagReadData + Environment.NewLine);
                            beepOK.Play();
                        }
                        else
                        {
                            //TextBox1.AppendText(DateTime.Now.ToString("G") + " NO read: " + e.TagReadData + Environment.NewLine);
                            //beepWrong.Play();
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        TextBox1.AppendText(DateTime.Now.ToString("G") + " Error read: " + e.TagReadData + Environment.NewLine);
                    }
                }

                //
                //If warning is there, remove it
                if (null != lblWarning.Text)
                {
                    string temperature = lblReaderTemperature.Content.ToString().TrimEnd('C', '°');
                    if (lblWarning.Text.ToString() != "")
                    {
                        if (int.Parse(temperature) < 85)
                        {
                            lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate ()
                            {
                                //GUIturnoffWarning();
                            }));
                        }
                    }
                }
            }));
            Dispatcher.BeginInvoke(new ThreadStart(delegate ()
            {
                txtTotalTagReads.Content = tagdb.TotalTagCount.ToString();
                totalUniqueTagsReadTextBox.Content = tagdb.UniqueTagCount.ToString();
            }
            ));
        }

        private void PrintTemperature(object sender, StatsReportEventArgs e)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate ()
            {
                lblReaderTemperature.Content = e.StatsReport.STATS.TEMPERATURE.ToString() + "°C";
                if (e.StatsReport.STATS.TEMPERATURE > 80) { imgTemperature.Source = new BitmapImage(new Uri(@"..\Icons\temp-high.png", UriKind.RelativeOrAbsolute)); }
                else if (e.StatsReport.STATS.TEMPERATURE > 455) { imgTemperature.Source = new BitmapImage(new Uri(@"..\Icons\temp-warn.png", UriKind.RelativeOrAbsolute)); }
                else { imgTemperature.Source = new BitmapImage(new Uri(@"..\Icons\temp-low.png", UriKind.RelativeOrAbsolute)); }
            }
          ));
        }

        private void r_ReadException(object sender, ReaderExceptionEventArgs e)
        {
            Reader r = (Reader)sender;
            TextBox1.AppendText(DateTime.Now.ToString("G") + "Exception reader uri " + (string)r.ParamGet("/reader/uri"));
            TextBox1.AppendText(DateTime.Now.ToString("G") + "Error: " + e.ReaderException.Message);
        }

        private static void errorHandler(Reader r)
        {
            SerialReader sr = r as SerialReader;
            ReaderException re = r.lastReportedException;
            switch (re.Message)
            {
                case "The reader received a valid command with an unsupported or invalid parameter":
                case "Unimplemented feature.":
                    r.StopReading();
                    r.Destroy();
                    break;
                case "The operation has timed out.":
                    sr.TMR_flush();
                    r.Destroy();
                    break;
                default:
                    r.Destroy();
                    break;
            }
        }

        private void dgTagResults_LostFocus(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// Clear all the ui controls and database related to tag reads 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearReads_Click(object sender, RoutedEventArgs e)
        {
            string msg = "Do you want to delete all result?";
            switch (MessageBox.Show(msg, "RFIDTimer Helper", MessageBoxButton.OKCancel, MessageBoxImage.Question))
            {
                case MessageBoxResult.OK:
                    {
                        SeenTags.Clear();
                        ClearReads();
                        break;
                    }
                case MessageBoxResult.Cancel:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// Clear all the ui controls and database related to tag reads 
        /// </summary>
        private void ClearReads()
        {
            Thread st = new Thread(delegate ()
            {
                this.Dispatcher.BeginInvoke(new ThreadStart(delegate ()
                {
                    lock (tagdb)
                    {
                        tagdb.Clear();
                        tagdb.Repaint();
                    }
                }
                ));
                Dispatcher.Invoke(new del(delegate ()
                {
                    try
                    {
                        txtTotalTagReads.Content = "0";
                        totalUniqueTagsReadTextBox.Content = "0";
                        if (objReader == null)
                        {
                            lblReaderTemperature.Content = "0" + "°C";
                        }
                    }
                    catch { }
                }));

            });
            st.Start();
        }

        public TagReadRecordBindingList TagList
        {
            get { return _tagList; }
            set { _tagList = value; }
        }

        /// <summary>
        /// Refresh the data grid for every dispatcher interval set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void dispatchtimer_Tick(Object sender, EventArgs args)
        {
            try
            {
                // Causes a control bound to the BindingSource to reread all the items 
                // in the list and refresh their displayed values.
                //TagResults.tagagingColourCache.Clear();
                tagdb.Repaint();
                // Forces an immediate garbage collection from generation zero through
                // a specified generation.            
                // GC.Collect(1);
                // Retrieves the number of bytes currently thought to be allocated. If 
                // the forceFullCollection parameter is true, this method waits a short
                // interval before returning while the system collects garbage and finalizes
                // objects.            
                //long totalmem1 = GC.GetTotalMemory(true);
            }
            catch { }
        }

        /// <summary>
        /// Warns the user when trying to close the app, when async read is going on 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // If URA is still reading, notify user
                if (isAsyncReadGoingOn || isSyncReadGoingOn)
                {
                    string msg = "Read is in progress , Do you want to close the application?";
                    switch (MessageBox.Show(msg, "Universal Reader Assistant Message",
                        MessageBoxButton.OKCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.OK:
                            {
                                btnRead_Click(null, null);
                                closeConnectURA(sender, new RoutedEventArgs());
                                Environment.Exit(1);
                                break;
                            }
                        case MessageBoxResult.Cancel:
                            {
                                e.Cancel = true;
                                break;
                            }
                    }
                }
                else
                {
                    closeConnectURA(sender, new RoutedEventArgs());
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {

            };
        }

        private void btnStartRace_Click(object sender, RoutedEventArgs e)
        {
            if (raceTimeClock.IsRunning)
            {
                //Stop Chrono Race
                raceTimeClock.Stop();
                dtRaceTimeClock.Stop(); //stop eventhandler
                TimeStopRace = DateTime.Now;
                imgStartRace.Source = new BitmapImage(new Uri(@"..\Icons\start_flag_yellow.png", UriKind.RelativeOrAbsolute));
                lblStartRace.Content = "Start Race";
                isRaceStarted = false;
                btnRead_Click(sender, e);
            }
            else
            {
                //Start Chrono Race
                if (TimeStopRace > TimeStartRace)
                {
                    MessageBox.Show("Please reset time before start Race", "RFIDTimer Helper");
                }
                else
                {
                    TimeStopRace = DateTime.Now;
                    TimeStartRace = DateTime.Now;
                    raceTimeClock.Start();
                    dtRaceTimeClock.Start(); //start eventhandler
                    imgStartRace.Source = new BitmapImage(new Uri(@"..\Icons\finish-line.png", UriKind.RelativeOrAbsolute));
                    lblStartRace.Content = "Stop Race";
                    isRaceStarted = true;
                    lblRaceTimehh.Foreground = Brushes.Green;
                    lblRaceTimemm.Foreground = Brushes.Green;
                    lblRaceTimess.Foreground = Brushes.Green;
                    lblRaceTimedc.Foreground = Brushes.Green;
                    TextBox1.AppendText(DateTime.Now.ToString("G") + " StartRace: " + String.Format("{0:dd/MM/yyyy hh mm ss fff}", TimeStartRace) + Environment.NewLine);
                }
            }
        }

        private void btnResetRace_Click(object sender, RoutedEventArgs e)
        {
            if (!raceTimeClock.IsRunning)
            {
                int columnCount = dgTagResults.Columns.Count;
                string msg1;
                if (columnCount > 0)
                    { msg1 = "Do you want to Reset Time and delete the Race result?"; }
                else
                    { msg1 = "Do you want to Reset Time?"; }

                switch (MessageBox.Show(msg1, "Universal Reader Assistant Message",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question))
                {
                    case MessageBoxResult.OK:
                        {
                            //Reset Chrono Racerace
                            TimeStopRace = new DateTime();
                            TimeStartRace = new DateTime();
                            raceTimeClock.Reset();
                            ClearReads();
                            lblRaceTimehh.Content = "00";
                            lblRaceTimemm.Content = "00";
                            lblRaceTimess.Content = "00";
                            lblRaceTimedc.Content = "00";
                            lblRaceTimehh.Foreground = Brushes.Red;
                            lblRaceTimemm.Foreground = Brushes.Red;
                            lblRaceTimess.Foreground = Brushes.Red;
                            lblRaceTimedc.Foreground = Brushes.Red;
                            break;
                        }
                    case MessageBoxResult.Cancel:
                        {
                            break;
                        }
                }

            }
        }

        private void RaceTime_Tick(object sender, EventArgs e)
        {
            TimeSpan ts = raceTimeClock.Elapsed;
            //string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            lblRaceTimehh.Content = String.Format("{0:00}", ts.Hours);
            lblRaceTimemm.Content = String.Format("{0:00}", ts.Minutes);
            lblRaceTimess.Content = String.Format("{0:00}", ts.Seconds);
            lblRaceTimedc.Content = String.Format("{0:00}", ts.Milliseconds / 10);
        }


        #region SaveTagResults
        ///<summary>
        ///Save the datagrid data to text file
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>        
        private void saveData_Click(object sender, RoutedEventArgs e)
        {
            string strDestinationFile = string.Empty;
            try
            {
                //if (null != tcTagResults.SelectedItem)
                {
                    SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                    saveFileDialog1.Filter = "CSV Files (*.csv)|*.csv";
                    //string tabHeader = ((TextBlock)((TabItem)tcTagResults.SelectedItem).Header).Text;
                    //if (tabHeader.Equals("Tag Results"))
                    {
                        strDestinationFile = "RFIDTimer_tagResults"
                            + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + @".csv";
                        saveFileDialog1.FileName = strDestinationFile;
                        if ((bool)saveFileDialog1.ShowDialog())
                        {
                            strDestinationFile = saveFileDialog1.FileName;
                            TagReadRecord rda;
                            // True, if any row is selected and only selected row is saved else 
                            // false and entire data grid is saved
                            bool flagSelectiveDataSave = false;
                            for (int rowCount = 0; rowCount <= dgTagResults.Items.Count - 1; rowCount++)
                            {
                                rda = (TagReadRecord)dgTagResults.Items.GetItemAt(rowCount);
                                if (rda.Checked)
                                {
                                    flagSelectiveDataSave = true;
                                    break;
                                }
                            }
                            TextWriter tw = new StreamWriter(strDestinationFile);
                            StringBuilder sb = new StringBuilder();
                            //writing the header
                            sb.Append("Pos, ");
                            int columnCount = dgTagResults.Columns.Count;
                            for (int count = 1; count < columnCount; count++)
                            {
                                if (dgTagResults.Columns[count].Visibility == Visibility.Visible)
                                {
                                    string colHeader = dgTagResults.Columns[count].Header.ToString();
                                    {
                                        if (count == columnCount - 1)
                                        {
                                            sb.Append(colHeader);
                                        }
                                        else
                                        {
                                            sb.Append(colHeader + ", ");
                                        }
                                    }
                                }
                            }
                            tw.WriteLine(sb.ToString());
                            if (flagSelectiveDataSave)
                            {
                                //writing the data
                                rda = null;
                                for (int rowCount = 0; rowCount <= dgTagResults.Items.Count - 1; rowCount++)
                                {
                                    rda = (TagReadRecord)dgTagResults.Items.GetItemAt(rowCount);
                                    if (rda.Checked)
                                    {
                                        textWrite(tw, rda, rowCount + 1);
                                    }
                                }
                            }
                            else
                            {
                                //writing the data
                                rda = null;
                                for (int rowCount = 0; rowCount <= dgTagResults.Items.Count - 1; rowCount++)
                                {
                                    rda = (TagReadRecord)dgTagResults.Items.GetItemAt(rowCount);
                                    textWrite(tw, rda, rowCount + 1);
                                }
                            }
                            tw.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// For readability sake in the text file.
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="rda"></param>
        private void textWrite(TextWriter tw, TagReadRecord rda, int rowNumber)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(rowNumber + ", ");
            sb.Append(rda.EPC + ", ");
            sb.Append(rda.FirstTimeStamp + ", ");
            sb.Append(rda.TimeStamp.ToString("dd-MM-yyyy HH:mm:ss:fff") + ", ");
            sb.Append(rda.RSSI + ", ");
            sb.Append(rda.ReadCount + ", ");
            //sb.Append(rda.TagType + ", ");
            //sb.Append(rda.Antenna + ", ");
            //sb.Append(rda.Protocol + ", ");
            //sb.Append(rda.Frequency + ", ");
            //sb.Append(rda.Phase + ", ");
            //sb.Append(rda.GPIO);
            tw.Write(sb.ToString());
            tw.WriteLine();
        }

        #endregion SaveTagResults

        /// <summary>
        /// Populate reader uri box with the port numbers
        /// </summary>
        private void InitializeReaderUriBox()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                List<string> portNames = GetComPortNames();
                cmbReaderAddr.ItemsSource = "";
                cmbReaderAddr.ItemsSource = portNames;
                if (portNames.Count > 0)
                {
                    cmbReaderAddr.Text = portNames[0];
                }
                Mouse.SetCursor(Cursors.Arrow);
            }
            catch (Exception bonjEX)
            {
                Mouse.SetCursor(Cursors.Arrow);
                throw bonjEX;
            }
        }

        /// <summary>
        /// Returns the COM port names as list
        /// </summary>
        private List<string> GetComPortNames()
        {
            List<string> portNames = new List<string>();
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0"))
            {
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if ((queryObj != null) && (queryObj["Name"] != null))
                    {
                        try
                        {
                            if (queryObj["Name"].ToString().Contains("(COM"))
                            {
                                //log.Info("Device detected on Port: " + queryObj["Name"].ToString());
                                if (queryObj["Name"].ToString().Contains("Serial Port"))
                                {
                                    string portNumber = Regex.Match((queryObj["Name"].ToString()), @"(?<=\().+?(?=\))").Value.ToString();
                                    using (Reader r = Reader.Create("tmr:///" + portNumber))
                                    {
                                        //log.Info("Detected device Has Generic Name so conneting to retrive the Model name and Serail number for Port: " + queryObj["Name"].ToString());
                                        SerialReader serialReader = r as SerialReader;
                                        int baud = 115200;
                                        #region Reduce the timeout to quickly complete the search for non TM Devices
                                        r.ParamSet("/reader/transportTimeout", 100);
                                        r.ParamSet("/reader/commandTimeout", 100);
                                        #endregion
                                        serialReader.OpenSerialPort(portNumber, ref baud);
                                        string strSerialNumber, strModel = serialReader.model;
                                        try
                                        {
                                            strSerialNumber = serialReader.CmdGetSerialNumber();
                                        }
                                        catch (Exception)
                                        {
                                            strSerialNumber = "";
                                        }
                                        string serialNumber = strSerialNumber;
                                        if (serialNumber == "" || serialNumber == null)
                                        {
                                            portNames.Add(strModel + " (" + portNumber + ")");
                                        }
                                        else
                                        {
                                            portNames.Add(strModel + "-" + serialNumber + " (" + portNumber + ")");
                                        }
                                    }
                                }
                                else
                                {
                                    string portNumber = Regex.Match((queryObj["Name"].ToString()), @"(?<=\().+?(?=\))").Value.ToString();
                                    using (Reader r = Reader.Create("tmr:///" + portNumber))
                                    {
                                        //log.Info("Detected device Has Generic Name so conneting to retrive the Serial number for Port: " + queryObj["Name"].ToString());
                                        SerialReader serialReader = r as SerialReader;
                                        int baud = 115200;
                                        #region Reduce the timeout to quickly complete the search for non TM Devices
                                        r.ParamSet("/reader/transportTimeout", 100);
                                        r.ParamSet("/reader/commandTimeout", 100);
                                        #endregion
                                        serialReader.OpenSerialPort(portNumber, ref baud);
                                        string strSerialNumber, strModel = serialReader.model;
                                        try
                                        {
                                            strSerialNumber = serialReader.CmdGetSerialNumber();
                                        }
                                        catch (Exception)
                                        {
                                            strSerialNumber = "";
                                        }
                                        string serialNumber = strSerialNumber;
                                        if (serialNumber == "" || serialNumber == null)
                                        {
                                            portNames.Add(queryObj["Name"].ToString());
                                        }
                                        else
                                        {
                                            portNames.Add(queryObj["Description"].ToString() + "-" + serialNumber + " (" + portNumber + ")");
                                        }
                                    }

                                }
                            }
                        }
                        catch (Exception)
                        {
                            //Reader is throwing error for connect so we are not going show in the reader name list..
                            //log.Info("Detected device" + queryObj["Name"].ToString() + " has Generic Name and Failed to connect.");
                        }
                    }
                }
            }

            #region Store to main list
            for (int i = 0; i < masterPortList.Count; i++)
            {
                string a = masterPortList[i];
                if (!(portNames.Contains(masterPortList[i])))
                {
                    masterPortList.Remove(masterPortList[i]);
                }
            }
            for (int i = 0; i < portNames.Count; i++)
            {
                string a = portNames[i];
                if (!(masterPortList.Contains(portNames[i])))
                {
                    masterPortList.Add(portNames[i]);
                }
            }
            #endregion

            return portNames;
        }

        private void btnRefreshReadersList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                InitializeReaderUriBox();
                Mouse.SetCursor(Cursors.Arrow);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Check for valid port numbers
        /// </summary>
        /// <param name="portNumber"></param>
        /// <returns></returns>
        private bool ValidatePortNumber(string portNumber)
        {
            List<string> portNames = new List<string>();
            List<string> portValues = new List<string>();
            //converting comport number from small letter to capital letter.Eg:com18 to COM18.
            MatchCollection mc1 = Regex.Matches(portNumber, @"(?<=\().+?(?=\))");
            foreach (Match m1 in mc1)
            {
                if (m1.ToString().ToUpperInvariant().Contains("COM"))
                {
                    portNumber = m1.ToString();
                }
            }
            // getting the list of comports value and name which device manager shows
            portNames = masterPortList;
            for (int i = 0; i < portNames.Count; i++)
            {
                MatchCollection mc = Regex.Matches(portNames[i], @"(?<=\().+?(?=\))");
                foreach (Match m in mc)
                {
                    portValues.Add(m.ToString());
                }
            }
            if ((portNames.Contains(cmbReaderAddr.Text)) || (portValues.Contains(portNumber)))
            {
                //Specified port number exist
                return true;
            }
            else
            {
                //Specified port number doesn't exist
                return false;
            }
        }

        /// <summary>
        /// Generate columns for datagrid
        /// </summary>
        public void GenerateColmnsForDataGrid()
        {
            // dgTagResults
            dgTagResults.AutoGenerateColumns = false;
            serialNoColumn.Binding = new Binding("SerialNumber");
            serialNoColumn.Header = "#";
            serialNoColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);

            epcColumn.Binding = new Binding("EPC");
            epcColumn.Header = "EPC";
            epcColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            firstStampColumn.Binding = new Binding("FirstTimeStamp");
            //firstStampColumn.Binding.StringFormat = "{0:HH:mm:ss.fff}";
            firstStampColumn.Binding.StringFormat = "{0:c}";
            firstStampColumn.Header = "First Time (ms)";
            firstStampColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            timeStampColumn.Binding = new Binding("TimeStamp");
            timeStampColumn.Binding.StringFormat = "{0:HH:mm:ss.fff}";
            timeStampColumn.Header = "Time Stamp (ms)";
            timeStampColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            rssiColumn.Binding = new Binding("RSSI");
            rssiColumn.Header = "RSSI (dBm)";
            rssiColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            readCountColumn.Binding = new Binding("ReadCount");
            readCountColumn.Header = "Read Count";
            readCountColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            antennaColumn.Binding = new Binding("Antenna");
            antennaColumn.Header = "Antenna";
            antennaColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            protocolColumn.Binding = new Binding("Protocol");
            protocolColumn.Header = "Protocol";
            protocolColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            frequencyColumn.Binding = new Binding("Frequency");
            frequencyColumn.Header = "Frequency (kHz)";
            frequencyColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            phaseColumn.Binding = new Binding("Phase");
            phaseColumn.Header = "Phase";
            phaseColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dataColumn.Binding = new Binding("Data");
            dataColumn.Header = "Data";
            dataColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dataSecureColumn.Binding = new Binding("Data");
            dataSecureColumn.Header = "Secure ID";
            dataSecureColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            epcColumnInAscii.Binding = new Binding("EPCInASCII");
            epcColumnInAscii.Header = "EPC (ASCII)";
            epcColumnInAscii.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            epcColumnInReverseBase36.Binding = new Binding("EPCInReverseBase36");
            epcColumnInReverseBase36.Header = "EPC (ReverseBase36)";
            epcColumnInReverseBase36.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dataColumnInAscii.Binding = new Binding("DataInASCII");
            dataColumnInAscii.Header = "Data (ASCII)";
            dataColumnInAscii.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            GPIOColumn.Binding = new Binding("GPIO");
            GPIOColumn.Header = "GPIO Status";
            GPIOColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            tagTypeColumn.Binding = new Binding("TagType");
            tagTypeColumn.Header = "Tag Type";
            tagTypeColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dgTagResults.ItemsSource = TagList;

            StartTimeColumn.Binding.StringFormat = "{0:HH:mm:ss.fff}";
            EndTimeColumn.Binding.StringFormat = "{0:HH:mm:ss.fff}";
            ElapsedTimeColumn.Binding.StringFormat = "{0:HH:mm:ss.fff}";

            //refreshEventRunner(); //viene chiamato dalla combobox e da insertDBTime()
            refreshEvents();
            refreshRunners();
            refreshCategories();
            refreshRaceNumber();
        }

        public void refreshEvents()
        {
            try
            {
                DataTable dtEvents = DBData.GetAllEvent();
                dgEventList.ItemsSource = dtEvents.AsDataView();
                // fill ComboBox
                eventsDetails = Utilities.ConvertDataTabletoList<EventModel>(dtEvents);
                cmbRaceSelect.ItemsSource = eventsDetails;

                // fill DataGrid
                dgEventList.SelectionChanged += (s, e) =>
                {
                    var item = dgEventList.SelectedItem as DataRowView;
                    if (null == item) return;
                    try
                    {
                        PnlIDEvent.Text = item.Row[0].ToString();
                        PnlEventDate.SelectedDate = (DateTime)item.Row[1];
                        PnlEventDesc.Text = item.Row[2].ToString();
                        PnlLenght.Text = item.Row[3].ToString();
                        PnlType.Text = item.Row[4].ToString();
                        PnlShortCircuit.IsChecked = (Boolean)item.Row[5];
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbRaceSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                tbxeventDesc.Text = ((sender as ComboBox).SelectedItem as EventModel).DescEvent.ToString();
                lblRaceTimelbl.Content= tbxeventDesc.Text;
                tbxeventLenght.Text = ((sender as ComboBox).SelectedItem as EventModel).LenghtEv.ToString();
                tbxEventType.Text = ((sender as ComboBox).SelectedItem as EventModel).TypeEv.ToString();
                tbxEventShort.IsChecked = ((sender as ComboBox).SelectedItem as EventModel).ShortCirc;

                //if ((Boolean)((sender as ComboBox).SelectedItem as EventModel).ShortCirc)
                if (((sender as ComboBox).SelectedItem as EventModel).TypeEv.ToString() == "Staffetta")
                {
                        tiRelayRace.Visibility = Visibility.Visible;
                }
                else 
                { 
                    tiRelayRace.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            refreshEventRunner();
            refreshRunners();

        }

        private void dgRrunnersOnChecked(object sender, RoutedEventArgs e)
        {
            var ch = sender as CheckBox;
            var row = dgRrunners.ItemContainerGenerator.ContainerFromItem(ch) as DataGridRow;
            //SelEventColumn
            CheckBox checkBox = (CheckBox)e.OriginalSource;
            bool ischecked = checkBox.IsChecked;
            if (ischecked)
            {
                row.Background = Brushes.Gray;
            }
            else
            {
                row.Background = Brushes.White;
            }
            //MessageBox.Show("okkkkkk", "Info", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void refreshEventRunner()
        {
            try
            {
                var query = @"select R.IDRun,R.Name,R.BirthYear,R.Sex,R.Email,E.RaceNumber,T.IDTime,T.EPC,T.StartTime,T.EndTime,T.ElapsedTime,T.Modified
                              from Runners R
                              inner join EventRunner E on R.IDRun = E.Runner_id 
                              left join Timings T on E.Event_id=T.Event_id and E.Runner_id = T.Runner_id 
                              WHERE E.Event_id=@Event_id";
                var args = new Dictionary<string, object>
                {
                    {"@Event_id", cmbRaceSelect.SelectedValue}
                };
                DataTable dt = DBData.ExecuteRead(query, args);
                dgEventRunner.ItemsSource = dt.AsDataView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void refreshRunners()
        {
            try
            {
                int idevent = 1;
                try
                {
                    idevent = Int32.Parse(cmbRaceSelect.SelectedValue.ToString());
                }
                catch (FormatException)
                {
                    
                }
                DataTable dtRunners = DBData.GetAllRunnerByEventId(idevent);
                dgRrunners.ItemsSource = dtRunners.AsDataView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void refreshCategories()
        {
            try
            {
                var query = @"SELECT * from Categories";
                var args = new Dictionary<string, object>
                {
                    {"@", null}
                };
                DataTable dt = DBData.ExecuteRead(query, args);
                dgCategories.ItemsSource = dt.AsDataView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void refreshRaceNumber()
        {
            try
            {
                var query = @"SELECT * from NumRaceEpc";
                var args = new Dictionary<string, object>
                {
                    {"@", null}
                };
                DataTable dt = DBData.ExecuteRead(query, args);
                dgRaceNumber.ItemsSource = dt.AsDataView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //da rivedere
        public void insertDBTime(String epc, DateTime timeNow, TimeSpan difftime) 
        {
            //TODO
            //FirstTimeStamp
            //DateTime localDate = DateTime.Now;
            //TimeSpan difftime = (RawRead.Time - MainWindow.TimeStartRace);

            try
            {
                string SqlString = @"INSERT INTO Tempi(IDTempo, TempoPartenza, TempoArrivo, TempoTrascorso, Manifestazione) SELECT Pettorale,@TimeStart,@TimeEnd,@TimeElapsed," + cmbRaceSelect.SelectedValue.ToString() + " as Manifestazione  FROM Pettorali WHERE Pettorali.EPC = '" + epc + "'";
                using (OleDbConnection cn = new OleDbConnection())
                {
                    using (OleDbCommand cmd = new OleDbCommand(SqlString, cn))
                    {
                        cmd.CommandType = CommandType.Text;
                        //cmd.Parameters.Add("@TimeStart", TimeStartRace.ToOADate());
                        //cmd.Parameters.Add("@TimeEnd", timeNow.ToOADate());
                        //cmd.Parameters.AddWithValue("@TimeElapsed", difftime.TotalMilliseconds);
                        //cmd.Parameters.AddWithValue("@Manifestazione", cmbRaceSelect.SelectedValue.ToString());
                        //cmd.Parameters.AddWithValue("@epc", epc);
                        cn.Open();
                        int xresult = cmd.ExecuteNonQuery();
                        if (xresult != 1) MessageBox.Show(xresult.ToString());
                        cn.Close();
                        refreshEventRunner();
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgTagResults_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            //TextBox1.AppendText(DateTime.Now.ToString("G") + " info: dgTagResults_LoadingRow" + Environment.NewLine);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //System.Windows.Data.CollectionViewSource dettaglioEventiViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("dettaglioEventiViewSource")));
            //dettaglioEventiViewSource.View.MoveCurrentToFirst();
            // Carica i dati nella tabella Iscritti. Se necessario, è possibile modificare questo codice.
            //System.Windows.Data.CollectionViewSource iscrittiViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("iscrittiViewSource")));
            //iscrittiViewSource.View.MoveCurrentToFirst();
        }

        private void Btn_SavedgEvent_Click(object sender, RoutedEventArgs e)
        {
            //TODO
            DataGrid datagrid = ((Button)sender).CommandParameter as DataGrid;
            var selectedRow = datagrid.SelectedItem;
            var selectedIndex = datagrid.SelectedIndex;

        }

        private void Btn_Save_Form_Event_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(PnlIDEvent.Text))
            {
                var events = new EventModel
                {
                    IDEvent = Convert.ToInt32(PnlIDEvent.Text),
                    DateEvent = (DateTime)PnlEventDate.SelectedDate,
                    DescEvent = Convert.ToString(PnlEventDesc.Text),
                    LenghtEv = Convert.ToInt32(PnlLenght.Text),
                    TypeEv = Convert.ToString(PnlType.Text),
                    ShortCirc = (Boolean)PnlShortCircuit.IsChecked
                };
                DBData.EditEvent(events);
                refreshEvents();
            } else
            {
                MessageBox.Show("Select an Event first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
