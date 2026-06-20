using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using CanIRunIt.Models;
using CanIRunIt.Services;

namespace CanIRunIt
{
    public partial class MainWindow : Window
    {
        private SystemSpecs _systemSpecs = new();
        private DatabaseService _databaseService = new();
        private CompatibilityChecker? _compatibilityChecker;
        private List<CompatibilityResult> _allResults = new();
        private List<CompatibilityResult> _filteredResults = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeApplication();
        }

        private async Task InitializeApplication()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            ErrorOverlay.Visibility = Visibility.Collapsed;

            try
            {
                await Task.Run(() =>
                {
                    // Detect system specs
                    var detector = new SystemDetector();
                    _systemSpecs = detector.DetectSystem();

                    // Load database
                    _databaseService = new DatabaseService();
                    bool loaded = _databaseService.LoadDatabase();

                    if (!loaded)
                    {
                        throw new Exception("DB_MISSING");
                    }

                    // Create compatibility checker
                    _compatibilityChecker = new CompatibilityChecker(_systemSpecs);

                    // Check all software
                    _allResults = new List<CompatibilityResult>();
                    foreach (var software in _databaseService.GetAllSoftware())
                    {
                        var result = _compatibilityChecker.CheckCompatibility(software);
                        _allResults.Add(result);
                    }
                });

                // Update UI on main thread
                UpdateSidebarSpecs();
                PopulateCategories();
                RefreshSoftwareList();
                
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex) when (ex.Message == "DB_MISSING")
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                ErrorOverlay.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                // Handle generic errors (maybe show a different error state)
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSidebarSpecs()
        {
            SidebarCpu.Text = TruncateText(_systemSpecs.CPUName, 25);
            SidebarRam.Text = $"{_systemSpecs.FormattedRAM}";
            SidebarGpu.Text = TruncateText(_systemSpecs.GPUName, 25);
            SidebarCpu.ToolTip = _systemSpecs.CPUName;
            SidebarGpu.ToolTip = _systemSpecs.GPUName;
        }

        private void PopulateCategories()
        {
            var categories = new List<string> { "الكل" };
            if (_databaseService != null)
            {
                categories.AddRange(_databaseService.GetAllCategories());
            }
            CategoryFilter.ItemsSource = categories;
            CategoryFilter.SelectedIndex = 0;
        }

        private void RefreshSoftwareList()
        {
            string searchQuery = SearchBox.Text?.Trim() ?? "";
            string selectedCategory = CategoryFilter.SelectedItem?.ToString() ?? "الكل";

            _filteredResults = _allResults
                .Where(r =>
                    (string.IsNullOrEmpty(searchQuery) ||
                     r.Software.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) &&
                    (selectedCategory == "الكل" ||
                     r.Software.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(r => r.CompatibilityScore)
                .ThenBy(r => r.Software.Name)
                .ToList();

            SoftwareList.ItemsSource = _filteredResults;
            
            // Toggle Placeholder
            if (SearchPlaceholder != null)
            {
                SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshSoftwareList();
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) RefreshSoftwareList();
        }

        private void SoftwareList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoftwareList.SelectedItem is CompatibilityResult result)
            {
                DisplayCompatibilityDetails(result);
            }
        }

        private void DisplayCompatibilityDetails(CompatibilityResult result)
        {
            EmptyState.Visibility = Visibility.Collapsed;
            DetailsView.Visibility = Visibility.Visible;

            DetName.Text = result.Software.Name;
            DetCat.Text = result.Software.Category;
            DetScore.Text = $"{result.CompatibilityScore}%";
            
            // Adjust Badge Colors based on score
            if (result.CompatibilityScore >= 80)
            {
                DetScoreBadge.Background = (Brush)new BrushConverter().ConvertFrom("#DEF7EC");
                DetScore.Foreground = (Brush)new BrushConverter().ConvertFrom("#03543F");
            }
            else if (result.CompatibilityScore >= 50)
            {
                DetScoreBadge.Background = (Brush)new BrushConverter().ConvertFrom("#FDF6B2");
                DetScore.Foreground = (Brush)new BrushConverter().ConvertFrom("#723B13");
            }
            else
            {
                DetScoreBadge.Background = (Brush)new BrushConverter().ConvertFrom("#FDE8E8");
                DetScore.Foreground = (Brush)new BrushConverter().ConvertFrom("#9B1C1C");
            }

            DetStatusIcon.Text = result.StatusEmoji;
            DetStatusText.Text = result.StatusText;
            DetStatusBox.Background = (Brush)new BrushConverter().ConvertFrom(result.StatusColor);
            DetStatusText.Foreground = (Brush)new BrushConverter().ConvertFrom(result.StatusColorText);

            // Populate Components
            ComponentsList.Children.Clear();
            AddComponentRow("معالج (CPU)", result.CPUCheck);
            AddComponentRow("الرام (RAM)", result.RAMCheck);
            AddComponentRow("كرت الشاشة (GPU)", result.GPUCheck);
            AddComponentRow("التخزين (Storage)", result.StorageCheck);
            AddComponentRow("النظام (OS)", result.OSCheck);
        }

        private void AddComponentRow(string title, ComponentCheck check)
        {
            var border = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 15, 0, 15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); // Icon
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Content
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Status text

            // Icon
            var iconBlock = new TextBlock
            {
                Text = check.StatusEmoji,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 0, 0)
            };
            Grid.SetColumn(iconBlock, 0);

            // Content Stack
            var contentStack = new StackPanel();
            
            // Header: Title + Status
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            headerGrid.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"))
            });
            
            // Status Text (Simple)
            var statusText = check.MeetsRecommended ? "Running Great" : (check.MeetsMinimum ? "Playable" : "Upgrade Needed");
            var statusColor = check.MeetsRecommended ? "#166534" : (check.MeetsMinimum ? "#D97706" : "#DC2626");
            
            var statusBlock = new TextBlock
            {
                Text = statusText,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor))
            };
            Grid.SetColumn(statusBlock, 1);
            headerGrid.Children.Add(statusBlock);
            
            contentStack.Children.Add(headerGrid);

            // Specs Grid
            var specsGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            specsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            specsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // User Spec
            var userStack = new StackPanel();
            userStack.Children.Add(new TextBlock { Text = "Your System", FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) });
            userStack.Children.Add(new TextBlock { Text = check.YourSpec, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")), Margin = new Thickness(0,2,0,0) });
            Grid.SetColumn(userStack, 0);

            // Required Spec
            var recStack = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            recStack.Children.Add(new TextBlock { Text = "Required", FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) });
            recStack.Children.Add(new TextBlock { Text = check.MinRequired, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")), Margin = new Thickness(0,2,0,0) });
            Grid.SetColumn(recStack, 1);

            specsGrid.Children.Add(userStack);
            specsGrid.Children.Add(recStack);
            contentStack.Children.Add(specsGrid);

            // Suggestion
            if (!string.IsNullOrEmpty(check.Suggestion))
            {
                var sugBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7ED")),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 10, 0, 0),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEDD5")),
                    BorderThickness = new Thickness(1)
                };
                
                var sugStack = new StackPanel { Orientation = Orientation.Horizontal };
                sugStack.Children.Add(new TextBlock { Text = "💡", Margin = new Thickness(0,0,8,0), FontSize = 12 });
                sugStack.Children.Add(new TextBlock
                {
                    Text = check.Suggestion,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9A3412")),
                    TextWrapping = TextWrapping.Wrap
                });
                sugBorder.Child = sugStack;
                contentStack.Children.Add(sugBorder);
            }

            Grid.SetColumn(contentStack, 1);

            grid.Children.Add(iconBlock);
            grid.Children.Add(contentStack);

            border.Child = grid;
            ComponentsList.Children.Add(border);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await InitializeApplication();
        }

        private async void RetryDb_Click(object sender, RoutedEventArgs e)
        {
            await InitializeApplication();
        }

        private void Nav_Home_Click(object sender, RoutedEventArgs e)
        {
            // Already home
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}