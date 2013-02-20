using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Threading;
using System.IO.IsolatedStorage;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Phone.Tasks;
using System.Windows.Resources;
using Microsoft.Phone.Shell;

using System.Net.NetworkInformation;
//using Microsoft.Phone.Net.NetworkInformation.NetworkInterface;

namespace WOL
{
    public partial class IconPage : PhoneApplicationPage
    {
        List<iconset> iconlist = new List<iconset>();
        List<imagelist> images = new List<imagelist>();
        string mode = "";
        string name = "";
        string mac = "";
        string _sender = "";
        string id = "9999999999999999999999999999999";
        string selected = "";
        bool delete = false;
        string directoryName = "/Shared/ShellContent/";

        public IconPage()
        {
            InitializeComponent();
        }

        //add picture
        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (zuneconnected())
            {
                MessageBox.Show("cannot open imagelibrary, connected to zune?");
            }
            else
            {
                try
                {

                    PhotoChooserTask photoChooserTask = new PhotoChooserTask();
                    photoChooserTask.Completed += PhotoChooserTaskCompleted;
                    photoChooserTask.Show();
                }
                catch (Exception ee)
                {
                    MessageBox.Show("cannot open imagelibrary, connected to zune?");
                }
            }
        }

        //delete
        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            if (delete == false)
            { 
                delete = true;
                disablebuttons(/*"delete",*/ true);
            }
            else 
            { 
                delete = false;
                disablebuttons(/*"delete",*/ false);
            }
        }

        //cancel
        private void ApplicationBarIconButton_Click_2(object sender, EventArgs e)
        {
            //NavigationService.GoBack();
            NavigationService.Navigate(new Uri("/" + _sender + ".xaml", UriKind.RelativeOrAbsolute));
        }

        //select item - main actions
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (delete == false)
                {
                    NavigationService.Navigate(new Uri("/" + _sender + ".xaml?chosen=" + iconlist[iconlistbox.SelectedIndex].Imagename + "&name=" + name + "&mac=" + mac + "&mode=" + mode + "&id=" + id, UriKind.Relative));
                }
                else
                {
                    if (iconlistbox.SelectedIndex < 3 && iconlistbox.SelectedIndex > -1)
                    {
                        MessageBox.Show("cannot delete default icons");
                    }
                    else
                    {
                        deleteitem(iconlistbox.SelectedIndex);

                        reload();
                    }
                }
            }
            catch (Exception ee) { }
        }

        //on load
        private void PhoneApplicationPage_Loaded_1(object sender, RoutedEventArgs e)
        {
            //zuneconnected();
            reload();
        }

        private void reload() 
        {
            this.iconlistbox.SelectedIndex = -1;
            this.iconlistbox.ItemsSource = null;
            iconlist.Clear();
            images.Clear();
            iconlist = readfromstorage();
            int index = 0;

            foreach (iconset temp in iconlist)
            {
                if (index < 3)
                {
                    var bitmapImage = new BitmapImage(new Uri(temp.Imagename, UriKind.Relative));
                    images.Add(new imagelist() { getImage = bitmapImage });
                }
                else
                {
                    BitmapImage bitmapimage = new BitmapImage();
                    bitmapimage = readimage(temp.Imagename);
                    images.Add(new imagelist() { getImage = bitmapimage });
                }

                index++;
            }
            this.iconlistbox.ItemsSource = images;
        }

        //on navigate to
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs ee)
        {
            try
            {
                mode = this.NavigationContext.QueryString["mode"];
                id = this.NavigationContext.QueryString["id"];
            }
            catch (Exception e) { }

            try
            {
                name = this.NavigationContext.QueryString["name"];
                mac = this.NavigationContext.QueryString["mac"];
            }
            catch (Exception e) { }

            try
            {
                _sender = this.NavigationContext.QueryString["sender"];
            }
            catch (Exception e) { }
        }

        private void PhotoChooserTaskCompleted(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                string name = e.OriginalFileName;
                // Load original image and invalidate bitmap so it gets newly rendered
                var bitmapImage = new BitmapImage();
                bitmapImage.SetSource(e.ChosenPhoto);

                string filename = name.Substring(name.LastIndexOf("\\")+1);

                //add the new image and save it to isolatedstorage
                images.Add(new imagelist() { getImage = bitmapImage });
                iconlist.Add(new iconset() { Imagename = filename });

                //reset databinding listbox
                this.iconlistbox.ItemsSource = null;
                this.iconlistbox.ItemsSource = images;//urilist;//iconlist;


                saveimage(filename, bitmapImage);
                saveiconsinstorage(iconlist);

            }
        }

        public List<iconset> readfromstorage()
        {
            List<iconset> tempdata = new List<iconset>();
            try
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("icons.xml", FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<iconset>));
                        tempdata = (List<iconset>)serializer.Deserialize(stream);
                    }
                }
            }
            catch
            {
                //add some code here
            }
            return tempdata;
        }

        public void saveimage(string tempJPEG, BitmapImage bitmapImage)
        {
            // Create a filename for JPEG file in isolated storage.
            //String tempJPEG = "logo.jpg";

            // Create virtual store and file stream. Check for duplicate tempJPEG files.
            IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();

            if (myIsolatedStorage.FileExists(directoryName+tempJPEG))
            {
                myIsolatedStorage.DeleteFile(directoryName+tempJPEG);
            }

            IsolatedStorageFileStream fileStream = myIsolatedStorage.CreateFile(directoryName+tempJPEG);

           // StreamResourceInfo sri = null;
            //Uri uri = new Uri(tempJPEG, UriKind.Relative);
            //sri = Application.GetResourceStream(uri);

            BitmapImage bitmap = new BitmapImage();
            //bitmap.SetSource(sri.Stream);
            bitmap = bitmapImage;
            WriteableBitmap wb = new WriteableBitmap(bitmap);

            // Encode WriteableBitmap object to a JPEG stream.
            Extensions.SaveJpeg(wb, fileStream, wb.PixelWidth, wb.PixelHeight, 0, 85);

            //wb.SaveJpeg(fileStream, wb.PixelWidth, wb.PixelHeight, 0, 85);
            fileStream.Close();
        }

        public BitmapImage readimage(string name) {
            BitmapImage bi = new BitmapImage();

            IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
            IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(directoryName+name, FileMode.Open, FileAccess.Read);

            bi.SetSource(fileStream);
            //this.tempimagedemo.Height = bi.PixelHeight;
            //this.tempimagedemo.Width = bi.PixelWidth;

           // this.tempimagedemo.Source = bi;

            return bi;
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

        //delete item
        private void deleteitem(int i) 
        {
            string imagename = iconlist[i].Imagename;
            bool safetodelete = false;

            List<computerset> tempset = new List<computerset>();
            List<computerset> othertempset = new List<computerset>();
            tempset = readcomputersfromstorage();
            othertempset = readcomputersfromstorage();
            // List<iconset> iconlist = new List<iconset>();
            int index = 0;
            foreach (computerset temp in tempset)
            {
                if (temp.Image == imagename)
                {
                    if (MessageBox.Show("sure to delete?", "image in use!", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                          othertempset.RemoveAt(index);
                        temp.Image = "/icons/desktop_tile.png";
                        othertempset.Insert(index, temp);
                        safetodelete = true;
                    }
                }
                else
                { 
                    safetodelete=true;
                }
                index++;
            }
            savecomputersinstorage(othertempset);



            if (safetodelete)
            {
                //MessageBox.Show("please recreate live tiles");

                iconlist.RemoveAt(i);
                images.RemoveAt(i);




                //iconlist.Clear();
                //images.Clear();
                //iconlist = null;
                //images = null;
                //this.iconlistbox.ItemsSource = iconlist;
                //this.iconlistbox.UpdateLayout();

                IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();

                try
                {
                    if (myIsolatedStorage.FileExists(directoryName + imagename))
                    {
                        try
                        {
                            myIsolatedStorage.DeleteFile(directoryName + imagename);
                        }
                        catch (Exception ee) { }
                    }
                    //File.Delete(directoryName + imagename);
                }
                catch (Exception e) { }
                saveiconsinstorage(iconlist);
            }
        }

        private void disablebuttons(/*string function,*/ bool on)
        {
            if (on)
            {
                //if (function == "remove")
                //{
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                    //((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
                //}
            }
            else
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = true;
            }

        }

        private bool zuneconnected() 
        {
            bool connectedtozune = false;
            //string type=Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType;
            string type = Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType.ToString();
            if (type == "Ethernet")
            {

                connectedtozune = true;
            }

            return connectedtozune;
        
        }


        public List<computerset> readcomputersfromstorage()
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

        public void savecomputersinstorage(List<computerset> data)
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
    }



}