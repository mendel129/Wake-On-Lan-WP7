using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Net.Sockets;
using System.Threading;
using System.IO.IsolatedStorage;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;


namespace WOL
{   
    public partial class MainPage : PhoneApplicationPage
    {
        string directoryName = "/Shared/ShellContent/";
        // Cached Socket object that will be used by each call for the lifetime of this class
        Socket _socket = null;
        // Signaling object used to notify when an asynchronous operation is completed
        static ManualResetEvent _clientDone = new ManualResetEvent(false);
        // Define a timeout in milliseconds for each asynchronous call. If a response is not received within this 
        // timeout period, the call is aborted.
        const int TIMEOUT_MILLISECONDS = 5000;
        // The maximum size of the data buffer to use with the asynchronous socket methods
        const int MAX_BUFFER_SIZE = 2048;

        IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        bool delete = false;
        bool edit = false;
        bool createlivetile = false;
        List<computerset> data = new List<computerset>();
        List<othercomputerset> otherdata = new List<othercomputerset>(); //used for databinding
        bool quickexit = true;
        bool updateto12done = true;//check if marketplace update

        Color themeColor = (Color)Application.Current.Resources["PhoneForegroundColor"];

        bool firstrunindicator = true;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                firstrunindicator = Convert.ToBoolean((string)appSettings["firstrunindicator"]);
                quickexit = Convert.ToBoolean((string)appSettings["quickexit"]);
            }
            catch (Exception e) { }

            try
            {
                updateto12done = Convert.ToBoolean((string)appSettings["updateto12done"]);
            }
            catch (Exception e) { }

            if (firstrunindicator || updateto12done)
            {
                firstrun();
            }
        }
        
        //public List<computerset> GenerateComputerSets()
        //{
        //    List<computerset> temp = new List<computerset>();
        //    temp.Add(new computerset() { Name = "MendelStation", Mac = "00:0E:A6:F8:AC:59" });
        //    temp.Add(new computerset() { Name = "PC-DeSwaef", Mac = "00:24:1D:86:70:AD" });
        //    return temp;
        //}

        public void saveinstorage(List<computerset> data)
        {
            // Write to the Isolated Storage
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("computers.xml", FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<computerset>));
                    using (XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSettings))
                    {
                        serializer.Serialize(xmlWriter, data);
                    }
                }
            }
        
        }

        public List<computerset> readfromstorage()
        {
            List<computerset> tempdata = new List<computerset>();
            try
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("computers.xml", FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<computerset>));
                        tempdata = (List<computerset>)serializer.Deserialize(stream);
                    }
                }
            }
            catch
            {
                //add some code here
            }
            return tempdata;
        }

        private string wakeup(string macAddress)
        {
            Byte[] datagram = new byte[102];

            for (int i = 0; i <= 5; i++)
            {
                datagram[i] = 0xff;
            }

            string[] macDigits = null;
            if (macAddress.Contains("-"))
            {
                macDigits = macAddress.Split('-');
            }
            else
            {
                macDigits = macAddress.Split(':');
            }

            if (macDigits.Length != 6)
            {
                throw new ArgumentException("Incorrect MAC address supplied!");
            }

            int start = 6;
            for (int i = 0; i < 16; i++)
            {
                for (int x = 0; x < 6; x++)
                {
                    datagram[start + i * 6 + x] = (byte)Convert.ToInt32(macDigits[x], 16);
                }
            }

            return Send(datagram);

        }

        public string Send(byte[] payload)//string data)
        {
            string response = "Operation Timeout";

            // We are re-using the _socket object that was initialized in the Connect method
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

                // Set properties on context object
                socketEventArg.RemoteEndPoint = new IPEndPoint(IPAddress.Broadcast, 11000);   //new DnsEndPoint(serverName, portNumber);

                // Inline event handler for the Completed event.
                // Note: This event handler was implemented inline in order to make this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    response = e.SocketError.ToString();

                    // Unblock the UI thread
                    _clientDone.Set();
                });

                // Add the data to be sent into the buffer
                //byte[] payload = data.ToArray();//Encoding.UTF8.GetBytes(data);
                socketEventArg.SetBuffer(payload, 0, payload.Length);

                // Sets the state of the event to nonsignaled, causing threads to block
                _clientDone.Reset();

                // Make an asynchronous Send request over the socket
                _socket.SendToAsync(socketEventArg);

                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS seconds.
                // If no response comes back within this time then proceed
                _clientDone.WaitOne(TIMEOUT_MILLISECONDS);


            }
            else
            {
                response = "Socket is not initialized";
            }

            return response;
        }

        //main actions
        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try{
                computerset temp = data[listBox1.SelectedIndex];//(computerset)listBox1.SelectedItem;
                if (delete==false && edit==false && createlivetile == false) //wakeup!
                {
                    if (listBox1.SelectedIndex > -1)
                    {
                        textBlock1.Text = "Waking up " + temp.Name + " on " + temp.Mac + "\n" + textBlock1.Text;
                        wakeup(temp.Mac);
                        listBox1.SelectedIndex = -1;
                    }
                }
                else if (delete == true && edit == false && createlivetile == false) //remove an item
                {
                    if (MessageBox.Show("Remove " + temp.Name + "?", "remove!", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        //remove tile
                        string tempstring = "action=" + listBox1.SelectedIndex;
                        ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains(tempstring));

                        // If the Tile was found, then delete it.
                        if (TileToFind != null)
                        {
                            TileToFind.Delete();
                        }
                    
                        //listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                        data.RemoveAt(listBox1.SelectedIndex);
                        //otherdata.RemoveAt(listBox1.SelectedIndex);
                        saveinstorage(data);
                        data = null;
                        //otherdata = null;
                        otherdata.Clear();

                        data = readfromstorage(); 
                        porttootherdata();

                        this.listBox1.ItemsSource = null;
                        this.listBox1.ItemsSource = otherdata;//data;
                        disablebuttons("", false);
                        delete = false;
                    }
                }
                else if (delete == false && edit == true && createlivetile == false) //edit
                {
                    NavigationService.Navigate(new Uri("/AddPage.xaml?name=" + temp.Name + "&mac=" + temp.Mac + "&id=" + listBox1.SelectedIndex + "&imageloc=" + temp.Image, UriKind.Relative));
                
                }
                else if (delete == false && edit == false && createlivetile == true) //create livetile
                {
                    //check if already exists
                    bool create=true;
                    string tempstring = "action=" + listBox1.SelectedIndex;
                    ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains(tempstring));

                    // If the Tile was found, then delete it.
                    if (TileToFind != null)
                    {
                        if (MessageBox.Show("Live tile already exists", "delete and renew?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            TileToFind.Delete();
                        }
                        else
                        {
                            create = false;
                            listBox1.SelectedIndex = -1;
                        }
                    }

                    if(create)
                    {
                        string imageloc;
                        if (temp.Image == "/icons/desktop_tile.png" || temp.Image == "/icons/server_tile.png" || temp.Image == "/icons/laptop_tile.png")
                        {
                            imageloc = temp.Image;
                        }
                        else
                        {
                            imageloc = "isostore:" + directoryName + temp.Image;

                        }

                        //createlivetile
                        StandardTileData NewTileData = new StandardTileData
                        {
                            BackgroundImage = new Uri(imageloc, UriKind.RelativeOrAbsolute),
                            Title = temp.Name,
                            BackTitle = temp.Mac,
                            BackContent = temp.Name,
                            //BackBackgroundImage = new Uri("Blue.jpg", UriKind.Relative) 
                        };
                        ShellTile.Create(new Uri("/MainPage.xaml?action=" + listBox1.SelectedIndex, UriKind.Relative), NewTileData); //exits application
                    }
                
                }
            }
            catch(Exception ee){}
        }

        //remove an item
        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            edit = false;
            createlivetile = false;

            if (delete == false && edit == false && createlivetile == false)
            {
                delete = true;
                textBlock1.Text = "you can remove now\n";
                disablebuttons("remove", true);
            }
            else
            {
                delete = false;
                textBlock1.Text = "stop removing\n";
                disablebuttons("", false);
            }
        }

        //add an item
        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddPage.xaml", UriKind.Relative));
        }

        //on loading of app
        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e) { }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs ee)
        {
            data.Clear();
            otherdata.Clear();
            data=readfromstorage();

            int id = 999999;
            try
            {
                id = Convert.ToInt32(NavigationContext.QueryString["action"]);
            }
            catch { }
            if (id < 999999)
            {

                computerset temp = data[id];
                textBlock1.Text = "Waking up " + temp.Name + " on " + temp.Mac + "\n" + textBlock1.Text;
                wakeup(temp.Mac);
                listBox1.SelectedIndex = -1;
                if (quickexit)
                {
                    //exit app
                    NavigationService.GoBack();
                }
            }

            try
            {
                quickexit = Convert.ToBoolean(NavigationContext.QueryString["quickexit"]);
                //appSettings.Add("quickexit", "true");
                appSettings["quickexit"] = quickexit.ToString();
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
            catch { }

            porttootherdata();
            this.listBox1.ItemsSource = null;
            this.listBox1.ItemsSource = otherdata;//data;

            delete = false;
            edit = false;
            createlivetile = false;

            disablebuttons("", false);
        }

        public bool checkstring(string macAddress) 
        {
            bool answer = false;


            string[] macDigits = null;
            if (macAddress.Contains("-"))
            {
                macDigits = macAddress.Split('-');
            }
            else
            {
                macDigits = macAddress.Split(':');
            }

            if (macDigits.Length == 6)
            {
                answer = true;
            }
               

            return answer;
        
        }

        //edit an item
        private void ApplicationBarIconButton_Click_2(object sender, EventArgs e)
        {
            delete = false;
            createlivetile = false;
            if (edit == false && delete == false && createlivetile == false)
            {
                edit = true;
                textBlock1.Text = "start editing\n";
                disablebuttons("edit", true);
            }
            else
            {
                edit = false;
                textBlock1.Text = "stop editing\n";
                disablebuttons("", false);
            }
        }

        //create livetile for an item
        private void ApplicationBarIconButton_Click_3(object sender, EventArgs e)
        {
            //create livetile for item
            edit = false;
            delete = false;
            //remove
            if (createlivetile == false && delete == false && createlivetile == false)
            {
                createlivetile = true;
                textBlock1.Text = "you can create a livetile now\n";
                disablebuttons("favorite", true);
                //((ApplicationBarIconButton)ApplicationBar.Buttons[3]).IconUri = new Uri("/images/light/appbar.favs.rest.png", UriKind.Relative);
            }
            else
            {
                createlivetile = false;
                textBlock1.Text = "stop livetiles\n";
                disablebuttons("", false);
                //((ApplicationBarIconButton)ApplicationBar.Buttons[3]).IconUri = new Uri("/images/dark/appbar.favs.rest.png", UriKind.Relative);
            }
        }

        //opens about
        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/about.xaml", UriKind.Relative));
        }

        //disable other buttons than the function you need
        private void disablebuttons(string function, bool on) 
        {
            if (on)
            {
                if (function == "favorite")
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
                    //((ApplicationBarIconButton)ApplicationBar.Buttons[3]).IsEnabled = false;
                }
                if (function == "remove")
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                    //((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[3]).IsEnabled = false;
                }
                if (function == "edit")
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                    //((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[3]).IsEnabled = false;
                }
            }
            else 
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = true;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[3]).IsEnabled = true;
            }
        
        }

        //opens settings
        private void ApplicationBarMenuItem_Click_1(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml?quickexit=" + quickexit.ToString(), UriKind.Relative));
        }

        //some settings set on first run
        private void firstrun()
        {
            if (firstrunindicator || updateto12done)
            {
                try
                {
                    appSettings.Remove("firstrunindicator");
                    appSettings.Remove("quickexit");
                } catch(Exception e){}

                appSettings.Add("firstrunindicator", "false");
                appSettings.Add("quickexit", "true");
                appSettings.Add("updateto12done", "false");

                List<iconset> iconlist = new List<iconset>();
                iconlist.Add(new iconset() { Imagename = "/icons/desktop_tile.png" });
                iconlist.Add(new iconset() { Imagename = "/icons/laptop_tile.png" });
                iconlist.Add(new iconset() { Imagename = "/icons/server_tile.png" });
                saveiconsinstorage(iconlist);

                try
                {
                    IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
                    if (!string.IsNullOrEmpty(directoryName) && !myIsolatedStorage.DirectoryExists(directoryName))
                    {
                        myIsolatedStorage.CreateDirectory(directoryName);
                    }
                }
                catch (Exception ex)
                {
                    // handle the exception
                }

                IsolatedStorageSettings.ApplicationSettings.Save();
            }

            if (updateto12done) //actions for an update 
            { 
            
            }
        }

        //save the icons
        public void saveiconsinstorage(List<iconset> data)
        {
            // Write to the Isolated Storage
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("icons.xml", FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<iconset>));
                    using (XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSettings))
                    {
                        serializer.Serialize(xmlWriter, data);
                    }
                }
            }
        }

        //navigate to iconpage
        private void ApplicationBarMenuItem_Click_2(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/IconPage.xaml?sender=MainPage", UriKind.Relative));
        }

        private void porttootherdata() 
        {
            //otherdata = null;
            foreach (computerset temp in data) 
            {
                BitmapImage image = new BitmapImage();
                
                if (temp.Image == "/icons/desktop_tile.png" || temp.Image == "/icons/server_tile.png" || temp.Image == "/icons/laptop_tile.png")
                {
                    image = new BitmapImage(new Uri(temp.Image, UriKind.Relative));
                }
                else
                {
                    image = readimage(temp.Image);

                }

                string tempname=temp.Name;
                string tempmac=temp.Mac;

                otherdata.Add(new othercomputerset() { Name = tempname, Mac = tempmac, Image = image });      
            }      
        }

        private BitmapImage readimage(string name)
        {
            BitmapImage bi = new BitmapImage();

            IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
            IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(directoryName+name, FileMode.Open, FileAccess.Read);

            bi.SetSource(fileStream);

            return bi;
        }

        private void deleteandupdate() 
        { 
            
            
        }

        private void ApplicationBarMenuItem_Click_3(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/HelpPage.xaml", UriKind.Relative));
        }
    }
}