using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using MesBox = System.Windows.MessageBox;
using System.Resources;
using Microsoft.VisualBasic;
using System.Windows.Input;

namespace Rkry8FinalProject
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty HotkeyProperty =  
            DependencyProperty.Register(nameof(Hotkey), typeof(Hotkey), typeof(MainWindow),
                            new FrameworkPropertyMetadata(default(Hotkey), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public Hotkey Hotkey
        {
            get => (Hotkey)GetValue(HotkeyProperty);
            set => SetValue(HotkeyProperty, value);
        }
        public String[] filePaths { get; set; }
        public String[] fileNames { get; set; }
        public String[] lyricNames { get; set; }
        private int selectedIndex;
        private Boolean isPaused = false;
        private Boolean isSliding = false;
        private Thread musicThread = null;
        private Int32 waitTime = 100;
        int counter = 0;
        Boolean goToNextSong = false;

        WMPLib.WindowsMediaPlayer player = new WMPLib.WindowsMediaPlayer();
        WMPLib.WindowsMediaPlayer voicePlayer = new WMPLib.WindowsMediaPlayer();
        TagLib.File f;

        TimeSpan totalTimeSpan;
        TimeSpan currentTimeSpan;
        List<TimeSpan> lyricTimeSpan;
        List<String> lyricString;
        List<TimeSpan> resourceTimeSpan;
        List<String> resourceString;
        List<Resource> resources = new List<Resource>();
        Resource lyric;
        int currentLyricLine = 0;
        int currentResourceLine = 0;
        Boolean isLyricEnded = false;
        Boolean isResourcesEnded = false;
        String backgroundFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\resource\background1.jpg");
        Background background;
        ResourceManager res_man;
        CultureInfo cul;
        int backgroundChangeWaitTime = 50;
        public MainWindow()
        {
            InitializeComponent();
            setup();
        }
        private void setup()
        {
            res_man = new ResourceManager("Rkry8FinalProject.Language.Res", typeof(MainWindow).Assembly);
            VolumeText.Text = VolumeSlider.Value.ToString();
            background = new Background(Background);
            background.Load(backgroundFilePath);
            musicThread = new Thread(update);
            musicThread.IsBackground = true;
            musicThread.Start();
            Title = Properties.Settings.Default.Title;
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
        private void OpenMp3Files_Click(object sender, RoutedEventArgs e)
        {
            handleOpen();
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
                    resources.Clear();
                    PlayListView.Items.Clear();
                    filePaths = openFileDialog.FileNames;
                    fileNames = new String[filePaths.Length];
                    lyricNames = new String[filePaths.Length];
                    parseSongNames();
                    addSongsToPlaylist();
                    parseLyricNames();
                    checkResourceCheckbox();
                    foreach (String lyricName in lyricNames)
                    {
                        Resource resource = new Resource(lyricName, danceImage);
                        resources.Add(resource);
                        resource.loadLyric();
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
            checkResourceCheckbox();
            reloadResources();
        }
        private void reloadResources()
        {
            lyric = resources[selectedIndex];
            lyricTimeSpan = lyric.lyricTime;
            lyricString = lyric.lyricString;
            resourceTimeSpan = lyric.resourceTime;
            resourceString = lyric.resourceString;
        }
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayListView.SelectedIndex == -1)
                PlayListView.SelectedIndex = 0;
            if (!isPaused)
                playNewSong();
            else
            {
                playSong();
            }
        }
        private void playSong()
        {
            reloadResources();
            player.controls.stop();
            player.controls.play();
            voicePlayer.controls.stop();
            voicePlayer.controls.play();
            voicePlayer.settings.volume = 100;
            pathTextblock.Text = ": " + filePaths[selectedIndex];
            initializeTimeTags();
            currentLyricLine = 0;
            currentResourceLine = 0;
            checkResourceCheckbox();
            LyricDisplay.Text = "";
            goToNextSong = true;
        }

        private void playNewSong()
        {
            try
            {
                player.URL = filePaths[selectedIndex];
                voicePlayer.URL = resources[selectedIndex].voiceFilePath;
                playSong();
            }
            catch (Exception ex)
            {
                MesBox.Show(ex.Message);
            }
        }
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            player.controls.pause();
            voicePlayer.controls.pause();
            isPaused = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            clear();
        }
        private void clear()
        {
            player.controls.stop();
            voicePlayer.controls.stop();
            goToNextSong = false;
            currentLyricLine = 0;
            currentResourceLine = 0;
            LyricDisplay.Text = "";
            fanLyricDisplay.Text = "";
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PlayListView.SelectedIndex == 0)//if now it's playing the first song
                    selectedIndex = PlayListView.SelectedIndex = PlayListView.Items.Count - 1;//switch to the last song
                else
                    PlayListView.SelectedIndex--;//if it isn't playing the last song, play the next one
                playNewSong();
            }
            catch (Exception ex)
            {
                selectedIndex = PlayListView.SelectedIndex = PlayListView.Items.Count - 1;
                playNewSong();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PlayListView.SelectedIndex == PlayListView.Items.Count - 1)//if now it's playing the last song
                    selectedIndex = PlayListView.SelectedIndex = 0;//switch to the first song
                else
                    PlayListView.SelectedIndex++;//if it isn't playing the last song, play the next one
                playNewSong();
            }
            catch (Exception ex)
            {
                selectedIndex = PlayListView.SelectedIndex = 0;
                playNewSong();
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
            while (true)
            {
                try
                {
                    if (!isPaused && player.controls.currentPositionString != null)//if the player isn't started it doesn't need to run at all
                    {
                        Dispatcher.Invoke(() =>
                        {
                            updateBackground();
                            handleNextSong();
                            if (!isSliding)
                            {
                                setupUI();
                                if (player.controls.currentPositionString.Equals(""))
                                    return;
                                currentTimeSpan = TimeSpan.ParseExact(player.controls.currentPositionString, "mm\\:ss", CultureInfo.InvariantCulture);
                                if (currentTimeSpan >= lyricTimeSpan[resources[selectedIndex].lyricLineCount - 1] - TimeSpan.FromMilliseconds(2000))
                                    return;//if it's the end of the lyric, don't need to check the rest of the loop
                                handleDisplayLyrics();
                                if (!(bool)danceCheckbox.IsChecked && !(bool)voiceCheckbox.IsChecked)
                                    return;//if no special resources, don't need to execute the rest of the method
                                if (currentTimeSpan >= resourceTimeSpan[resources[selectedIndex].resourceLineCount - 1] - TimeSpan.FromMilliseconds(2000))
                                    return;//if it's the end of resource, don't need to continue
                                handleDance();
                                handleVoice();
                            }
                            else//track the slider's value and update the lyrics
                            {
                                currentTimeSpan = TimeSpan.FromSeconds(MusicSlider.Value);
                                handleMusicSlider();                                
                            }
                        });
                    }
                    Thread.Sleep(waitTime);
                }
                catch (Exception ex)
                {
                }
            }
        }
        //*********************************methods being called inside update()*********************************
        private void handleDisplayLyrics()
        {
            if (currentTimeSpan >= (lyricTimeSpan[currentLyricLine] - TimeSpan.FromMilliseconds(1200)))
            {
                LyricDisplay.Text = lyricString[currentLyricLine++];
            }
        }
        private void updateBackground()
        {
                counter++;
            if (counter > backgroundChangeWaitTime)
            {
                counter = 0;//change from background0 to background9.jpg randomly
                background.Load(backgroundFilePath.Substring(0, backgroundFilePath.Length - 5) + new Random().Next(0, 9).ToString() + ".jpg");
            }
        }
        private void setupUI()
        {
            currentTime.Text = player.controls.currentPositionString;
            totalTime.Text = totalTimeSpan.ToString(@"mm\:ss\:fff");
            if (totalTimeSpan.TotalSeconds != 0)
                MusicSlider.Maximum = totalTimeSpan.TotalSeconds;

            if (player.controls.currentPositionString.Equals(""))
                MusicSlider.Value = 0;
            else
                MusicSlider.Value = player.controls.currentPosition;
        }
        private void handleMusicSlider()
        {
            try
            {
                if (currentTimeSpan >= lyricTimeSpan[currentLyricLine] - TimeSpan.FromMilliseconds(1200))
                    currentLyricLine++;
                else
                {
                    if (currentTimeSpan <= (lyricTimeSpan[currentLyricLine] - TimeSpan.FromMilliseconds(1200)))
                        currentLyricLine--;
                }
                if (currentTimeSpan >= resourceTimeSpan[currentResourceLine] - TimeSpan.FromMilliseconds(1200))
                    currentResourceLine++;
                else if (currentTimeSpan <= resourceTimeSpan[currentResourceLine] - TimeSpan.FromMilliseconds(1200))
                    currentResourceLine--;

                if (currentLyricLine > resources[selectedIndex].lyricLineCount - 1)
                    currentLyricLine = 0;
                if (currentResourceLine > resources[selectedIndex].resourceLineCount - 1)
                    currentResourceLine = 0;
            }
            catch (Exception ex)
            {
            }
        }
        private void handleVoice()
        {
            if ((bool)voiceCheckbox.IsChecked)
            {
                voicePlayer.settings.mute = false;
                if (currentTimeSpan >= (resourceTimeSpan[currentResourceLine] - TimeSpan.FromMilliseconds(1200)))
                {
                    fanLyricDisplay.Text = resourceString[currentResourceLine++];
                }
            }
            else
            {
                voicePlayer.settings.mute = true;//if not interested in the voice, mute the player
            }
        }
        private void handleDance()
        {
            if (!(bool)danceCheckbox.IsChecked)
                return;
            if (currentTimeSpan >= (resourceTimeSpan[currentResourceLine] - TimeSpan.FromMilliseconds(2000)))
            {
                if (resourceString[currentResourceLine].Equals("imageStart"))
                {
                    resources[selectedIndex].loadDanceImage();
                    Background.Visibility = Visibility.Hidden;
                    danceImage.Visibility = Visibility.Visible;
                    currentResourceLine++;
                }
                else if (resourceString[currentResourceLine].Equals("imageEnd"))
                {
                    Background.Visibility = Visibility.Visible;
                    danceImage.Visibility = Visibility.Hidden;
                    currentResourceLine++;
                }
                if (currentResourceLine == resources[selectedIndex].resourceLineCount)
                {
                    isResourcesEnded = true;
                    currentResourceLine--;
                }
            }
        }
        private void handleNextSong()
        {
            if (goToNextSong && player.playState == WMPLib.WMPPlayState.wmppsStopped)
                NextButton_Click(null, null);
        }
        //********************************* end methods being called inside update()*********************************
        private void MusicSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                player.controls.pause();
                voicePlayer.controls.pause();
                player.controls.currentPosition = MusicSlider.Value;
                voicePlayer.controls.currentPosition = MusicSlider.Value;
                currentTime.Text = player.controls.currentPositionString;
                player.controls.play();
                voicePlayer.controls.play();
                voicePlayer.settings.mute = true;
                isSliding = false;
                for (int i = 0; i < resources[selectedIndex].lyricLineCount && TimeSpan.FromMilliseconds(MusicSlider.Value * 1000) >= (lyricTimeSpan[i] - TimeSpan.FromMilliseconds(1000)); i++)
                {
                    currentLyricLine = i - 1;
                }
                for (int i = 0; i < resources[selectedIndex].resourceLineCount && TimeSpan.FromMilliseconds(MusicSlider.Value * 1000) >= (resourceTimeSpan[i] - TimeSpan.FromMilliseconds(2000)); i++)
                {
                    currentResourceLine = i - 1;
                }
                if (currentLyricLine < 0)
                    currentLyricLine = 0;
                if (currentResourceLine < 0)
                    currentResourceLine = 0;
            }
            catch (Exception ex)
            {
            }
        }

        private void MusicSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isSliding = true;
            danceImage.Visibility = Visibility.Hidden;
            isLyricEnded = false;
            isResourcesEnded = false;
        }

        private void checkResourceCheckbox()
        {
            try
            {
                if (resources[selectedIndex].hasImage)
                {
                    danceCheckbox.IsEnabled = true;
                    danceCheckbox.IsChecked = true;
                }
                else
                {
                    danceCheckbox.IsEnabled = false;
                    danceCheckbox.IsChecked = false;
                }
                if (resources[selectedIndex].hasVoice)
                {
                    voiceCheckbox.IsEnabled = true;
                    voiceCheckbox.IsChecked = true;
                }
                else
                {
                    voiceCheckbox.IsEnabled = false;
                    voiceCheckbox.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                selectedIndex = 0;
            }
        }

        private void PlayListView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playNewSong();
        }

        private void about_Click(object sender, RoutedEventArgs e)
        {
            MesBox.Show("Banana,\n馬睿宣(Vincent Ma)\nhelped providing lyrics", "Special thanks");
        }

        private void English_Click(object sender, RoutedEventArgs e)
        {
            cul = CultureInfo.CreateSpecificCulture("en");
            changeLanguage();
            Languages.Width = 70;
        }

        private void Chinese_Click(object sender, RoutedEventArgs e)
        {
            cul = CultureInfo.CreateSpecificCulture("zh-tw");
            changeLanguage();
            Languages.Width = 40;
        }

        private void Japanese_Click(object sender, RoutedEventArgs e)
        {
            cul = CultureInfo.CreateSpecificCulture("ja");
            changeLanguage();
            Languages.Width = 40;
        }
        private void changeLanguage()
        {
            try
            {
                About.Header = res_man.GetString("About", cul);
                File.Header = res_man.GetString("File", cul);
                Help.Header = res_man.GetString("Help", cul);
                Languages.Header = res_man.GetString("Languages", cul);
                OpenMp3Files.Header = res_man.GetString("OpenMp3Files", cul);
                Playlist.Text = res_man.GetString("Playlist", cul);
                danceCheckbox.Content = res_man.GetString("Dance", cul);
                voiceCheckbox.Content = res_man.GetString("Voice", cul);
                Volume.Text = res_man.GetString("Volume", cul);
                PlayButton.Content = res_man.GetString("Play", cul);
                PauseButton.Content = res_man.GetString("Pause", cul);
                StopButton.Content = res_man.GetString("Stop", cul);
                PreviousButton.Content = res_man.GetString("Previous", cul);
                NextButton.Content = res_man.GetString("Next", cul);
                Path.Text = res_man.GetString("Path", cul);
            }
            catch (Exception ex)
            {
                MesBox.Show(ex.Message);
            }
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            String input = Interaction.InputBox("Please enter a new name for the application", "Title", "LiSA LiVE fan chant practice player(Demo version)");
            Properties.Settings.Default.Title = input;
            if(!input.Equals(""))
                Properties.Settings.Default.Save();
            Title = input;
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Don't let the event pass further
            // because we don't want standard textbox shortcuts working
            e.Handled = true;
            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;
            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }
            // Pressing delete, backspace or escape without modifiers clears the current value
            if (modifiers == ModifierKeys.None && (key == Key.Delete || key == Key.Back || key == Key.Escape))
            {
                Hotkey = null;
                return;
            }
            // If no actual key was pressed - return
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LeftShift
                || key == Key.RightShift || key == Key.LWin || key == Key.RWin || key == Key.Clear || key == Key.OemClear || key == Key.Apps)
            {
                return;
            }
            // Set values
            Hotkey = new Hotkey(key, modifiers);
            executeShortcut(Hotkey.ToString());
        }
        private void executeShortcut(String shortcut)
        {
            switch (shortcut)
            {
                case "Ctrl + O":
                    OpenMp3Files_Click(null, null);
                    break;
                case "Ctrl + P":
                    if (!isPaused)
                        PlayButton_Click(null, null);
                    break;
                case "Ctrl + A":
                    PauseButton_Click(null, null);             
                    break;
                case "Ctrl + S":
                    StopButton_Click(null, null);
                    break;
                case "Ctrl + Up":
                    PreviousButton_Click(null, null);
                    break;
                case "Ctrl + Down":
                    NextButton_Click(null, null);
                    break;
                case "Up":
                    VolumeSlider.Value++;
                    break;
                case "Down":
                    VolumeSlider.Value--;
                    break;
                case "Left":
                    MusicSlider_PreviewMouseDown(null, null);
                    MusicSlider.Value -= 2;
                    MusicSlider_PreviewMouseUp(null, null);
                    break;
                case "Right":
                    MusicSlider_PreviewMouseDown(null, null);
                    MusicSlider.Value += 2;
                    MusicSlider_PreviewMouseUp(null, null);
                    break;                    
                default:
                    MesBox.Show(shortcut + " isn't a supported shortcut :(\n"
                        + "Currently supported shourcuts are:\n"
                        + "Ctrl + O: open music files\n"
                        + "Ctrl + P: play\n"
                        + "Ctrl + A: pause\n"
                        + "Ctrl + S: stop\n"
                        + "Ctrl + Up: previous song\n"
                        + "Ctrl + Down: next song\n" 
                        + "Up: volume up\n"
                        + "Down: volume down\n"
                        + "Left: backward for 2 seconds\n"
                        + "Right: fast foward for 2 seconds\n");
                    break;
            }
        }
    }
}
