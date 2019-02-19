using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace Rkry8FinalProject
{
    class Resource
    {
        List<String> originalLyricStrings = new List<String>();
        List<String> originalResourceStrings = new List<String>();

        public List<String> lyricString = new List<String>();
        public List<TimeSpan> lyricTime = new List<TimeSpan>();
        public List<String> resourceString = new List<String>();
        public List<TimeSpan> resourceTime = new List<TimeSpan>();

        int lyricLineCount;
        int resourceLineCount;

        public int index;
        public String songName { get; set; }
        public Boolean hasImage{ get; set; }
        public Boolean hasVoice{ get; set; }

        public String lyricFilePath { get; set; }
        public String danceFilePath { get; set; }
        public String voiceFilePath { get; set; }
        public String resourceTimeFilePath { get; set; }
        Image danceImage;

        public Resource(String songName, Image danceImage)
        {
            lyricFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\lrc\" + songName + ".lrc");
            danceFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\resource\" + songName + ".gif");
            voiceFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\resource\" + songName + ".mp3");
            resourceTimeFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\resource\" + songName + ".lrc");
            this.danceImage = danceImage;
        }
        public Boolean loadLyric()
        {
            try
            {
                using (StreamReader sr = new StreamReader(lyricFilePath))
                {
                    int i = 0;
                    while (!sr.EndOfStream)
                    {
                        originalLyricStrings.Add(sr.ReadLine());
                        i++;
                    }
                    lyricLineCount = i;
                    loadResources();
                    if (parseTimeTag("lyric") && parseLyricLines("lyric"))
                        return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }
        public Boolean loadResources()
        {
            try
            {
                using (StreamReader sr = new StreamReader(resourceTimeFilePath))
                {
                    int i = 0;
                    while (!sr.EndOfStream)
                    {
                        originalResourceStrings.Add(sr.ReadLine());
                        i++;
                    }
                    resourceLineCount = i;
                    if(parseTimeTag("resource"))
                        return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }
        public Boolean parseTimeTag(String type)
        {
            try
            {
                if (type.Equals("lyric"))
                {
                    for (int i = 0; i < lyricLineCount; i++)
                    {
                        String str = null;
                        str = originalLyricStrings[i].Substring(1, 8) + "0";//the original lyric file only has two digits for milliseconds so add it back manually                  
                        lyricTime.Add(TimeSpan.ParseExact(str, "mm\\:ss\\.fff", CultureInfo.InvariantCulture));
                    }
                    return true;
                }
                else
                {
                    for (int i = 0; i < resourceLineCount; i++)
                    {
                        String str = null;
                        str = originalResourceStrings[i].Substring(1, 8) + "0";//the original lyric file only has two digits for milliseconds so add it back manually                  
                        resourceTime.Add(TimeSpan.ParseExact(str, "mm\\:ss\\.fff", CultureInfo.InvariantCulture));
                        checkRecourceType(originalResourceStrings[i].Substring(8));//pass the "lyric" without timetag to the method for checking
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }
        private void checkRecourceType(String str)
        {
            if (str.Contains("image"))
            {
                hasImage = true;
                loadDanceImage();
            }
            else if (str.Contains("voice"))
                hasVoice = true;
        }
        public Boolean parseLyricLines(String type)
        {
            try
            {
                if (type.Equals("lyric"))
                {
                    for (int i = 0; i < lyricLineCount; i++)
                    {
                        String str = null;
                        str = originalLyricStrings[i].Substring(10);
                        lyricString.Add(str);
                    }
                    return true;
                }
                else if (type.Equals("resource"))
                {
                    for (int i = 0; i < lyricLineCount; i++)
                    {
                        String str = null;
                        str = originalResourceStrings[i].Substring(10);
                        resourceString.Add(str);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }
        public void loadDanceImage()
        {
            BitmapImage theBitmap = new BitmapImage();
            theBitmap.BeginInit();
            theBitmap.UriSource = new Uri(danceFilePath, UriKind.Absolute);
            theBitmap.DecodePixelWidth = 500;
            theBitmap.EndInit();
            danceImage.Source = theBitmap;
            ImageBehavior.SetAnimatedSource(danceImage, theBitmap);
        }
    }
}
