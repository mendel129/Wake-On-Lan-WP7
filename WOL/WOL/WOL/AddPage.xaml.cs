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
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace WOL
{
    public partial class AddPage : PhoneApplicationPage
    {
        string directoryName = "/Shared/ShellContent/";
        int id=99999999;
        string imagelocation = "/icons/desktop_tile.png";
        string mode = "add";

        public AddPage()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            string name = textBox1.Text;
            string mac = textBox2.Text;

            if (name != "")
            {
                if (mac != "")
                {
                    MainPage tempmainpage = new MainPage();
                    List<computerset> data = new List<computerset>();

                    if (tempmainpage.checkstring(mac) == true)
                    {
                        data = tempmainpage.readfromstorage();

                        bool nameexists = false;
                        bool macexists = false;
                        foreach (computerset zoeken in data)
                        {
                            if (zoeken.Name == name)
                            { nameexists = true; }
                            if (zoeken.Mac == mac)
                            { macexists = true; }
                        }
                        bool add=false;

                        if (mode != "edit")
                        {
                            if (nameexists)
                            {
                                if (MessageBox.Show(name + " already exists", "add or adjust?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                                {
                                    add = true;
                                }
                            }
                            else if (macexists)
                            {
                                if (MessageBox.Show(mac + " already exists", "add or adjust?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                                {
                                    add = true;
                                }
                            }
                            else
                            { 
                                add = true; 
                            }
                        }
                        else
                        { 
                            add = true; 
                        }

                       if(add==true)
                       {
                            if (id != 99999999)
                            {
                                bool existed = false;
                                //remove livetile
                                //remove tile
                                string tempstring = "action=" + id;
                                ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains(tempstring));
                                // If the Tile was found, then delete it.
                                if (TileToFind != null)
                                {
                                    TileToFind.Delete();
                                    existed = true;
                                }


                                data.RemoveAt(id);
                                computerset heelfftemp = new computerset() { Name = name, Mac = mac, Image = imagelocation };
                                data.Insert(id, heelfftemp);



                                tempmainpage.saveinstorage(data);

                                //create livetile if existed
                                if (existed)
                                {
                                    if (MessageBox.Show("will exit app...", "Recreate livetile?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                                    {
                                        string imageloc;
                                        if (heelfftemp.Image == "/icons/desktop_tile.png" || heelfftemp.Image == "/icons/server_tile.png" || heelfftemp.Image == "/icons/laptop_tile.png")
                                        {
                                            imageloc = heelfftemp.Image;
                                        }
                                        else
                                        {
                                            imageloc = "isostore:" + directoryName + heelfftemp.Image;

                                        }

                                        //createlivetile
                                        StandardTileData NewTileData = new StandardTileData
                                        {
                                            BackgroundImage = new Uri(imageloc, UriKind.RelativeOrAbsolute),
                                            Title = heelfftemp.Name,
                                            BackTitle = heelfftemp.Mac,
                                            BackContent = heelfftemp.Name,
                                            //BackBackgroundImage = new Uri("Blue.jpg", UriKind.Relative) 
                                        };
                                        ShellTile.Create(new Uri("/MainPage.xaml?action=" + id, UriKind.Relative), NewTileData); //exits application
                                        
                                        
                                        //StandardTileData NewTileData = new StandardTileData
                                        //{
                                        //    BackgroundImage = new Uri(heelfftemp.Image, UriKind.Relative),
                                        //    Title = heelfftemp.Name,
                                        //    BackTitle = heelfftemp.Mac,
                                        //    BackContent = heelfftemp.Name,
                                        //    //BackBackgroundImage = new Uri("Blue.jpg", UriKind.Relative) 
                                        //};
                                        //ShellTile.Create(new Uri("/MainPage.xaml?action=" + id, UriKind.Relative), NewTileData); //exits application}
                                    }
                                }
                            }
                            else 
                            {
                                data.Add(new computerset() { Name = name, Mac = mac, Image = imagelocation });
                                tempmainpage.saveinstorage(data);
                            }

                            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                       }
                      }
                    else
                    { 
                        errortext.Visibility = Visibility.Visible; errortext.Text = "wrong macaddress provided"; 
                    }
                }
                else
                { 
                    errortext.Visibility = Visibility.Visible; errortext.Text = "fill in macaddress"; 
                }
            }
            else
            { 
                errortext.Visibility = Visibility.Visible; errortext.Text = "fill in computername"; 
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void image1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/IconPage.xaml?sender=AddPage&name="+textBox1.Text+"&mac="+textBox2.Text+"&mode="+mode+"&id="+id, UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs ee)
        {
            //coming from main with edit
            try
            {
                textBox1.Text = NavigationContext.QueryString["name"];
                textBox2.Text = NavigationContext.QueryString["mac"];
                imagelocation = NavigationContext.QueryString["imageloc"];
                //if (imagelocation == "")
                //    imagelocation = "/icons/desktop_tile.png";

                //image1.Source = new BitmapImage(new Uri(imagelocation, UriKind.Relative)); ;

                id = Convert.ToInt32(NavigationContext.QueryString["id"]);
                PageTitle.Text = "edit it";
                mode = "edit";
            }
            catch { }
            
            //coming from iconchooser
            try
            {
                imagelocation = this.NavigationContext.QueryString["chosen"];
                textBox1.Text = this.NavigationContext.QueryString["name"];
                textBox2.Text = this.NavigationContext.QueryString["mac"];
                mode = this.NavigationContext.QueryString["mode"];
                id = Convert.ToInt32(this.NavigationContext.QueryString["id"]);
            }
            catch (Exception e) { }

             if (imagelocation == "/icons/desktop_tile.png" || imagelocation == "/icons/server_tile.png" || imagelocation == "/icons/laptop_tile.png")
             {
                 ImageSource imgSource = new BitmapImage(new Uri(imagelocation, UriKind.Relative));
                 image1.Source = imgSource;
             }
             else 
             {
                 BitmapImage bitmapimage = new BitmapImage();
                 bitmapimage = readimage(imagelocation);
                 image1.Source = bitmapimage;
             }
            

        }

        public BitmapImage readimage(string name)
        {
            BitmapImage bi = new BitmapImage();

            IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
            IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(directoryName+name, FileMode.Open, FileAccess.Read);

            bi.SetSource(fileStream);

            return bi;
        }
    }
}