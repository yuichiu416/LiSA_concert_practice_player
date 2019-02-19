using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using MesBox = System.Windows.MessageBox;
using WpfAnimatedGif;

namespace Rkry8FinalProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static EventWaitHandle waitHandle = new ManualResetEvent(initialState: true);
        public String[] filePaths { get; set; }
        public String[] fileNames { get; set; }
        public String[] lyricNames { get; set; }
        private int selectedIndex;
        private Boolean isPaused = false;
        private Boolean isSliding = false;
        private Thread musicThread = null;
        private Int32 waitTime = 300;
        WMPLib.WindowsMediaPlayer player = new WMPLib.WindowsMediaPlayer();
        TagLib.File f;
        TimeSpan totalTimeSpan;
        TimeSpan currentTimeSpan;
        List<TimeSpan> lyricTimeSpan;
        List<String> lyricString;
        List<TimeSpan> resourceTimeSpan;
        List<String> resourceString;

        List<Resources> resources = new List<Resources>();
        Resources lyric;
        int currentLyricLine = 0;
        int currentResourceLine = 0;

        public MainWindow()
        {
            InitializeComponent();
            VolumeText.Text = VolumeSlider.Value.ToString();
            musicThread = new Thread(update);
            musicThread.Start();
            waitHandle.Reset();
        }

        public void parseLyricNames()
        {
            String str;
            try
            {
                var i = 0;
                foreach (String s in fileNames)
                {
                    str = s.Substring(4, s.Length - 8);//the name will be like: 03. song.mp3, ignore "03. " and ".mp3")
                    lyricNames[i] = str;
                    i++;
                }
            }
            catch (Exception ex)
            {
                MesBox.Show(ex.Message);
            }
        }
        public bool handleOpen()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "mp3 files (*.mp3)|*.mp3",
                    RestoreDirectory = true,
                    Multiselect = true
                };
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    filePaths = openFileDialog.FileNames;
                    fileNames = new String[filePaths.Length];
                    lyricNames = new String[filePaths.Length];
                    parseSongNames();
                    addSongsToPlaylist();
                    parseLyricNames();
                    foreach (String lyricName in lyricNames)
                    {
                        Resources lyric = new Resources(lyricName, danceImage);
                        resources.Add(lyric);
                        lyric.loadLyric();
                        if (lyric.hasImage)
                            danceCheckbox.IsEnabled = true;
                        else if (lyric.hasVoice)
                            voiceCheckbox.IsEnabled = true;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                MesBox.Show(ex.Message);
            }
            return false;
        }
        
        private void parseSongNames()
        {
            String[] parsedPath;
            try
            {
                var i = 0;
                foreach (String s in filePaths)
                {
                    parsedPath = s.Split('\\');//the result will be like: 03. song.mp3
                    fileNames[i] = parsedPath[parsedPath.Length - 1];
                    i++;
                }
            }
            catch (Exception ex)
            {
                MesBox.Show(ex.Message);
            }
        }
        private void addSongsToPlaylist()
        {
            foreach (String s in fileNames)
            {
                ListBoxItem playListItem = new ListBoxItem();
                try
                {
                    playListItem.Content = s;
                    PlayListView.Items.Add(playListItem);
                }
                catch (Exception ex)
                {
                    MesBox.Show(ex.Message);
                }
            }
        }

        private void PlayListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedIndex = PlayListView.SelectedIndex;
        }

        private void OpenMp3Files_Click(object sender, RoutedEventArgs e)
        {
            handleOpen();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaused)
                playNewSong();
            else
            {
                player.controls.play();
                isPaused = false;
            }
        }
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            player.controls.pause();
            isPaused = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            player.controls.stop();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            PlayListView.SelectedIndex--;
            if (selectedIndex < 0)
                selectedIndex = PlayListView.SelectedIndex = PlayListView.Items.Count - 1;
            playNewSong();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayListView.SelectedIndex == PlayListView.Items.Count - 1)//if now it's playing the last song
                selectedIndex = PlayListView.SelectedIndex = 0;//switch to the first song
            else
                PlayListView.SelectedIndex++;//if it isn't playing the last song, play the next one
            playNewSong();
        }
        private void playNewSong()
        {
            try
            {
                player.URL = filePaths[selectedIndex];
                lyric = resources[selectedIndex];
                lyricTimeSpan = lyric.lyricTime;
                lyricString = lyric.lyricString;
                resourceTimeSpan = lyric.resourceTime;
                resourceString = lyric.resourceString;
                player.controls.stop();
                player.controls.play();
                pathTextblock.Text = "Path: " + filePaths[selectedIndex];
                initializeTimeTags();
            }
            catch (Exception ex)
            {
                MesBox.Show(ex.Message);
            }
        }
        
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                VolumeText.Text = VolumeSlider.Value.ToString("#");
                if (VolumeText.Text.Equals(""))
                    VolumeText.Text = "0";
                player.settings.volume = (int)VolumeSlider.Value;
            }
            catch (Exception ex)
            {
                MesBox.Show(ex.Message);
            }
        }
        private void initializeTimeTags()
        {

            f = TagLib.File.Create(filePaths[selectedIndex], TagLib.ReadStyle.Average);
            totalTimeSpan = f.Properties.Duration;
            currentTime.Text = "00:00:000";
            totalTime.Text = totalTimeSpan.ToString(@"mm\:ss\:fff");
        }
        private void update()
        {
            var i = 0;
            while (true)
            {
                try
                {
                    if (!isPaused && player.controls.currentPositionString != null)
                    {
                        currentTime.Text = (i++).ToString();
                           Dispatcher.Invoke(() =>
                        {
                            if (!isSliding)
                            {
                                currentTime.Text = player.controls.currentPositionString;
                                totalTime.Text = totalTimeSpan.ToString(@"mm\:ss\:fff");
                                if (totalTimeSpan.TotalSeconds != 0)
                                    MusicSlider.Maximum = totalTimeSpan.TotalSeconds;
                                if (player.controls.currentPositionString.Equals(""))
                                    MusicSlider.Value = 0;
                                else
                                    MusicSlider.Value = player.controls.currentPosition;

                                if (!player.controls.currentPositionString.Equals(""))
                                {
                                    currentTimeSpan = TimeSpan.ParseExact(player.controls.currentPositionString, "mm\\:ss", CultureInfo.InvariantCulture);
                                    if (currentTimeSpan >= (lyricTimeSpan[currentLyricLine] - TimeSpan.FromMilliseconds(1000)))
                                    {
                                        LyricDisplay.Text = lyricString[currentLyricLine++];
                                    }
                                }
                                danceImage.Visibility = Visibility.Collapsed;
                               /*if (currentTimeSpan >= resourceTimeSpan[currentResourceLine] - TimeSpan.FromMilliseconds(1000))
                               {
                                    if (resourceString.Equals("imagestart"))
                                    {
                                        resources[selectedIndex++].loadDanceImage();
                                        danceImage.Visibility = Visibility.Visible;
                                    }
                               
                               }*/

                            }
                        });                    
                    }
                    
                    Thread.Sleep(waitTime);
                }
                catch (Exception ex)
                {
                    MesBox.Show(ex.Message);
                }
                waitHandle.WaitOne();
            }
        }
       
        private void MusicSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            player.controls.pause();
            player.controls.currentPosition = MusicSlider.Value;
            currentTime.Text = player.controls.currentPositionString;
            player.controls.play();
            isSliding = false;
        }

        private void MusicSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isSliding = true;
        }

        private void danceCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            MesBox.Show(MusicSlider.Maximum.ToString());
        }

        private void danceCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void voiceCheckbox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void voiceCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
        }
    }
    
}
