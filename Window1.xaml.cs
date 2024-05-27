using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using Google.Protobuf.WellKnownTypes;
using static Mysqlx.Expect.Open.Types.Condition.Types;
using MySqlX.XDevAPI.Common;


namespace rm
{

    #region ViewModel
    public class ViewModel : INotifyPropertyChanged
    {
        #region HideShowElementsWithSelectPlaylist

        private Visibility _rectangleVisibility = Visibility.Collapsed; // Начальное состояние скрыто

        public Visibility RectangleVisibility
        {
            get => _rectangleVisibility;
            set
            {
                _rectangleVisibility = value;
                OnPropertyChanged();
            }
        }

        private Playlist _selectedPlaylist;
        public Playlist SelectedPlaylist
        {
            get => _selectedPlaylist;
            set
            {
                _selectedPlaylist = value;
                // Переключение видимости при каждом выборе плейлиста
                RectangleVisibility = RectangleVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged();
            }
        }

        #endregion

        public void SavePlaylists(List<Playlist> playlists)
        {
            string json = JsonSerializer.Serialize(playlists);
            File.WriteAllText("playlist.json", json);
        }

        public List<Playlist> LoadPlaylists()
        {
            string json = File.ReadAllText("playlists.json");
            return JsonSerializer.Deserialize<List<Playlist>>(json);
        }

        private ObservableCollection<Playlist> _playlists;
        public ObservableCollection<Playlist> Playlists
        {
            get => _playlists;
            set
            {
                _playlists = value;
                OnPropertyChanged();
            }
        }

        #region Init
        private bool _isPlaying;

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IconData));
                    IconMargin = IsPlaying ? new Thickness(18, 0, 0, 0) : new Thickness(20, 0, 0, 0);
                    OnPropertyChanged(nameof(IconMargin));
                    IconWidth = IsPlaying ? 20 : 20;
                    IconHeight = IsPlaying ? 20 : 25;
                    OnPropertyChanged(nameof(IconMargin));
                    OnPropertyChanged(nameof(IconWidth));
                    OnPropertyChanged(nameof(IconHeight));
                }
            }
        }
        private Dictionary<string, Track> _tracksDictionary;
        private const string TracksFilePath = "E:/rm/rm/track.json"; // Путь к файлу для сохранения треков

        

        public Dictionary<string, Track> TracksDictionary
        {
            get => _tracksDictionary;
            set
            {
                _tracksDictionary = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region WorkWithTracks
        public ObservableCollection<Track> Items { get; private set; }
        public ICommand OpenFileCommand { get; private set; }
        public ICommand ToggleVisibilityCommand { get; private set; }
        public ICommand TogglePlayCommand { get; }
        public ICommand AddPlaylistCommand { get; private set; }

        public ViewModel()
        {
            _tracksDictionary = new Dictionary<string, Track>();
            Items = new ObservableCollection<Track>();
            _playlists = new ObservableCollection<Playlist>();
            AddPlaylistCommand = new RelayCommand(AddPlaylist);
            LoadTracks();
            OpenFileCommand = new RelayCommand(OpenFileCommandExecute);
            ToggleVisibilityCommand = new RelayCommand(ToggleVisibility);
            TogglePlayCommand = new RelayCommand(ExecuteTogglePlayCommand);
            _iconMargin = new Thickness(20, 0, 0, 0);
            _iconWidth = 20;
            _iconHeight = 25;
            _playPauseIcon = "M 0 0 L 15 0 L 15 30 L 0 30 Z M 15 0 L 30 0 L 30 30 L 15 30 Z";
        }
        private void AddPlaylist()
        {
            Playlist newPlaylist = new Playlist { Name = $"Playlist {Playlists.Count + 1}" };
            Playlists.Add(newPlaylist);
            OnPropertyChanged(nameof(Playlists));
        }

        private void LoadTracks()
        {
            if (File.Exists(TracksFilePath))
            {
                try
                {
                    string json = File.ReadAllText(TracksFilePath);
                    var tracksDictionary = JsonSerializer.Deserialize<Dictionary<string, Track>>(json);
                    if (tracksDictionary != null)
                    {
                        Items.Clear();
                        foreach (var track in tracksDictionary.Values)
                        {
                            Items.Add(track);
                        }
                    }
                } catch (Exception ex)
                {
                    Debug.WriteLine("Error loading tracks: " + ex.Message);
                    // Handle exceptions as needed
                }
            }
        }

        private void OpenFileCommandExecute()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string path = openFileDialog.FileName;
                if (path != null)
                {
                    AddTrack(path);
                }
            }
        }

        public void AddTrack(string path)
        {
            string key = Path.GetFileNameWithoutExtension(path);

            if (!_tracksDictionary.ContainsKey(key))
            {
                var newTrack = new Track
                {
                    Author = "Unknown",
                    TrackName = key,
                    Path = path
                };

                _tracksDictionary.Add(key, newTrack);
                Items.Add(newTrack); // Add to the ObservableCollection as well
                SaveTracks();
                OnPropertyChanged(nameof(Items)); // Notify the change for Items
            } else
            {
                MessageBox.Show("Track already exists.");
            }
        }

        public void SaveTracks()
        {
            try
            {
                string json = JsonSerializer.Serialize(_tracksDictionary);
                File.WriteAllText(TracksFilePath, json);
            } catch (Exception ex)
            {
                Debug.WriteLine("Error saving tracks: " + ex.Message);
                // Здесь можете добавить более подробную обработку ошибок
            }
        }




        ~ViewModel()
        {
            SaveTracks();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //...

        #region Buttons

        private Brush _background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF656565"));
        private Brush _foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEDEDED"));
        private Brush _borderbrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF656565"));
        private string _buttonContent = "↩️";
        public string IconData => IsPlaying ? "m 10 13 L 10 4 H 3 L 3 13 Z V 4 L 14 4 H 14 H 14 V 13 Z" : "M0,0 L0,40 30,20 Z";
        private Thickness _iconMargin = new Thickness(20);
        private double _iconWidth = 20;
        private double _iconHeight = 30;
        private double _columnWidth = 177;
        private string _playPauseIcon;

        private GridLength _firstColumnWidth = new GridLength(95, GridUnitType.Star);
        public GridLength FirstColumnWidth
        {
            get => _firstColumnWidth;
            set
            {
                _firstColumnWidth = value;
                OnPropertyChanged();
            }
        }

        private void ExecuteTogglePlayCommand()
        {
            IsPlaying = !IsPlaying;
            OnPropertyChanged("TriggerAnimation");
        }

        public Thickness IconMargin
        {
            get => _iconMargin;
            set
            {
                if (_iconMargin != value)
                {
                    _iconMargin = value;
                    OnPropertyChanged(nameof(IconMargin));
                }
            }
        }

        public double IconHeight
        {
            get => _iconHeight;
            set
            {
                if (_iconHeight != value)
                {
                    _iconHeight = value;
                    OnPropertyChanged(nameof(IconHeight));
                }
            }
        }

        public double IconWidth
        {
            get => _iconWidth;
            set
            {
                if (_iconWidth != value)
                {
                    _iconWidth = value;
                    OnPropertyChanged(nameof(IconWidth));
                }
            }
        }

        public Brush Foreground
        {
            get => _foreground;
            set
            {
                if (_foreground != value)
                {
                    _foreground = value;
                    OnPropertyChanged();
                }
            }
        }

        public Brush BorderBrush
        {
            get => _borderbrush;
            set
            {
                if (_borderbrush != value)
                {
                    _borderbrush = value;
                    OnPropertyChanged();
                }
            }
        }

        public Brush Background
        {
            get => _background;
            set
            {
                if (_background != value)
                {
                    _background = value;
                    OnPropertyChanged();
                }
            }
        }


        private Visibility _elementsVisibility = Visibility.Visible;

        public Visibility ElementsVisibility
        {
            get => _elementsVisibility;
            set
            {
                if (_elementsVisibility != value)
                {
                    _elementsVisibility = value;
                    OnPropertyChanged();
                }
            }
        }
        public string ButtonContent
        {
            get => _buttonContent;
            set
            {
                if (_buttonContent != value)
                {
                    _buttonContent = value;
                    OnPropertyChanged();
                }
            }
        }



        private void ToggleVisibility()
        {
            ElementsVisibility = ElementsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            var currentColor = ((SolidColorBrush)_background).Color.ToString();
            var newColorHex = currentColor == "#FF656565" ? "#FFEDEDED" : "#FF656565";
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(newColorHex));

            var currentColor2 = ((SolidColorBrush)_foreground).Color.ToString();
            var newColorHex2 = currentColor2 == "#FFEDEDED" ? "#FF656565" : "#FFEDEDED";
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(newColorHex2));

            var currentColor3 = ((SolidColorBrush)_borderbrush).Color.ToString();
            var newColorHex3 = currentColor3 == "#FF656565" ? "#FFEDEDED" : "#FF656565";
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(newColorHex3));

            ButtonContent = ButtonContent == "↩️" ? "↪️" : "↩️";
            FirstColumnWidth = FirstColumnWidth.Value > 0 ? new GridLength(0) : new GridLength(95, GridUnitType.Star);
        }

        #endregion


    }
    #endregion

    #region etc
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
        #endregion
    }
    #endregion
    #region WorkWithWPF
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            Closing += OnWindowClosing;
            this.DataContext = new ViewModel();
        }
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (DataContext is ViewModel vm)
            {
                vm.SaveTracks();
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateClip();
            this.SizeChanged += (s, t) => UpdateClip();  // Обновление Clip при изменении размера окна
        }

        private void AnimatedButton_Click(object sender, RoutedEventArgs e)
        {
            Storyboard storyboard = animatedButton.FindResource("ShrinkAndGrowAnimation") as Storyboard;
            if (storyboard != null)
            {
                storyboard.Begin(animatedButton);
            }
        }

        private void AnimatedButton_Next(object sender, RoutedEventArgs e)
        {
            Storyboard storyboard = AnimateNextButtton.FindResource("GoRightNext") as Storyboard;
            if (storyboard != null)
            {
                storyboard.Begin(AnimateNextButtton);
            }
        }

        private void UpdateClip()
        {
            mainGrid.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, mainGrid.ActualWidth, mainGrid.ActualHeight),
                RadiusX = 20,
                RadiusY = 20
            };
        }

        // Работа с верхней панелью

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void GoFullScreenButton(object sender, RoutedEventArgs e)
        {
            if (this.WindowState != WindowState.Maximized)
            {
                this.WindowState = WindowState.Maximized;
                (sender as Button).Content = "▣";
            } else
            {
                this.WindowState = WindowState.Normal;
                (sender as Button).Content = "▢";
            }
        }

        private void GoFullScreenRectangle()
        {
            if (this.WindowState != WindowState.Maximized)
            {
                this.WindowState = WindowState.Maximized;
            } else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void RollDownWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void DragMoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            if (e.ClickCount == 2)
            {
                GoFullScreenRectangle();
            }
        }

        //  Бар меню


        private void PlayClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel vm)
            {
                vm.IsPlaying = !vm.IsPlaying;
            }
        }
    }
}
#endregion
#region Classes
public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Track
{
    public string Author { get; set; }
    public string TrackName { get; set; }
    public string Path { get; set; }
    // Здесь можете добавить свои свойства, например, Image обложки
}
public class Playlist
{
    public string Name { get; set; }
    public ObservableCollection<Track> Tracks { get; set; }

    public Playlist()
    {
        Tracks = new ObservableCollection<Track>();
    }
}
#endregion