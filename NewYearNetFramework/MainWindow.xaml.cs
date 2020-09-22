using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows.Navigation;
using System.Windows.Media.Imaging;

namespace NewYearNetFramework
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        private readonly RegistryKey currentUser = Registry.CurrentUser;
        private readonly string system32 = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\System32\";
        private string imagePath, musicPath = "";
        private bool isPaused = false;
        private int counter = 0;
        private string[] musicNames = { };
        private TimeSpan dateToNewYear;
        private readonly MediaPlayer player = new MediaPlayer();
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly DateTime nextYear = new DateTime(DateTime.Now.Year + 1, 1, 1);
        #endregion
        #region Methods
        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory($@"{system32}\NewYearHelper\");
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            player.MediaEnded += Player_MediaEnded;
            Timer_Tick(null, null);
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
        }
        private void StartAudio()
        {
            if (counter == musicNames.Length) counter = 0;
            player.Open(new Uri($@"{musicNames[counter]}", UriKind.Absolute));
            player.Play();
            counter += 1;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                musicPath = currentUser.OpenSubKey(@"SOFTWARE\NewYearTime").GetValue("MusicFolderPath").ToString();
                imagePath = $@"{currentUser.OpenSubKey(@"SOFTWARE\NewYearTime", true).GetValue("ImagePath")}";
                if (imagePath != "background.jpg" && !File.Exists(imagePath))
                    imagePath = "background.jpg";
                if (musicPath != "")
                    musicNames = Directory.GetFiles(musicPath);
                application.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), imagePath)));
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Настоятельно рекомендуем прочитать помощника (находится в правом верхнем углу). Он поможет узнать горячие клавиши, расскажет как поставить свою музыку, свое изображение и т.д", "Помощник", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                currentUser.CreateSubKey(@"SOFTWARE\NewYearTime", RegistryKeyPermissionCheck.ReadWriteSubTree).
                    SetValue("MusicFolderPath", $"");
                DefaultBackground(null, null);
            }
            #region Help text
            File.WriteAllText($@"{system32}\NewYearHelper\NewYearHelper.txt", "\t\t\t\t\tCправочник по всем горячим клавишам\n\n" +
                "1. У программы есть контекстное меню, для того чтобы его вызвать нужно нажать ПКМ в любом месте программы,\n\nкроме строчки " +
                "с заголовком. Данное меню содержит всю основную функциальность, в нём вы сможете остановить\n\nпесню, " +
                "изменить скорость воспроизведения, выбрать папку откуда считывать музыку, выбрать изображение или поставить стандтарное, " +
                "выбрать громкость и выйти.\n\n2. " +
                "Но некоторые из этих функций можно воспроизвести путём нажатия некоторых клавиш на клавиатуре, а именно\n\n" +
                "2.1 Кнопка M - почти что инвертирует текущее состояние звука (выключает звук, если он включен, и включает на стандартную громкость, если он выключен)\n\n" +
                "2.2 Кнопка K - приостанавливает воспроизведение файла или запускает его, в зависимости от текущего состояния\n\n" +
                "2.3 Кнопка + - Добавляет к текущей громкости 5% процентов (стандратная - 50%, максимальная 100%)\n\n" +
                "2.4 Кнопка - - Вычитает из текущей громкости 5% процентов");
            #endregion
            if (musicNames.Length != 0)
                StartAudio();
        }
        private void Player_MediaEnded(object sender, EventArgs e) => StartAudio();
        private void Timer_Tick(object sender, EventArgs e)
        {
            dateToNewYear = nextYear - DateTime.Now;
            newYearDateLabel.Content = $"{(dateToNewYear.Days < 10 ? $"0{dateToNewYear.Days}" : dateToNewYear.Days.ToString())} : " +
                $"{(dateToNewYear.Hours < 10 ? $"0{dateToNewYear.Hours}" : dateToNewYear.Hours.ToString())} : " +
                $"{(dateToNewYear.Minutes < 10 ? $"0{dateToNewYear.Minutes}" : dateToNewYear.Minutes.ToString())} : " +
                $"{(dateToNewYear.Seconds < 10 ? $"0{dateToNewYear.Seconds}" : dateToNewYear.Seconds.ToString())}";
        }
        private void Close(object sender, RoutedEventArgs e) => Close();
        private void SoundInvert(object sender, RoutedEventArgs e)
        {
            if (!isPaused)
                player.Pause();
            else
                player.Play();
            isPaused = !isPaused;
        }
        private void MenuItemsChecker(object sender = null, double volume = 0)
        {
            // Uncheck all elements in volume context menu
            foreach (MenuItem i in Volume.Items)
            {
                i.IsChecked = false;
            }
            if (sender != null)
                (sender as MenuItem).IsChecked = true;
            else
            {
                foreach (MenuItem i in Volume.Items)
                {
                    if (i.ToString().Contains(volume.ToString()))
                    {
                        i.IsChecked = true;
                        break;
                    }
                }
            }
        }
        private void ChangeVolume(object sender, RoutedEventArgs e)
        {
            MenuItemsChecker(sender);
            player.Volume = Convert.ToDouble(Regex.Match(sender.ToString(), "Header:(.*?)% ", RegexOptions.Compiled).Groups[1].Value);
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.M:
                    player.Volume = player.Volume == 0 ? 0.5 : 0;
                    MenuItemsChecker(volume: player.Volume * 100);
                    break;
                case Key.K:
                    if (!isPaused)
                        player.Pause();
                    else
                        player.Play();
                    isPaused = !isPaused;
                    StopSound.IsChecked = !StopSound.IsChecked;
                    break;
                case Key.OemPlus:
                case Key.Add:
                    player.Volume += 0.05;
                    if (Math.Round(player.Volume * 100 % 10, 2) == 0)
                        MenuItemsChecker(volume: player.Volume * 100);
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    player.Volume -= 0.05;
                    if (Math.Round(player.Volume * 100 % 10, 2) == 0 || Math.Round(player.Volume * 100 % 10, 2) == 10)
                        MenuItemsChecker(volume: player.Volume * 100);
                    break;
            }
        }
        private void Image_MouseDown(object sender, MouseEventArgs e) => System.Diagnostics.Process.Start($@"{system32}\NewYearHelper\NewYearHelper.txt");
        private void MusicChange(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    musicNames = Directory.GetFiles(fbd.SelectedPath);
                    currentUser.OpenSubKey(@"SOFTWARE\NewYearTime", true).SetValue("MusicFolderPath", fbd.SelectedPath);
                    StopSound.IsChecked = false;
                    counter = 0;
                    player.Play();
                    StartAudio();
                }
            }
        }
        private void ChangePicture(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png",
                Title = "Выберите изображение"
            };
            if (fileDialog.ShowDialog() == true)
            {
                application.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), $@"{fileDialog.FileName}")));
                currentUser.OpenSubKey(@"SOFTWARE\NewYearTime", true).SetValue("ImagePath", $@"{fileDialog.FileName}");
            }
        }
        private void TopMost(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
        }
        private void DefaultBackground(object sender, RoutedEventArgs e)
        {
            
            application.Background = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "background.jpg")));
            currentUser.OpenSubKey(@"SOFTWARE\NewYearTime", true).SetValue("ImagePath", "background.jpg");
        }
        #endregion
    }
}
