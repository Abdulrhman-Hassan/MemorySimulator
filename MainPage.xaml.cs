using System.Collections.ObjectModel;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MemorySimulator.Core;

namespace MemorySimulator;

public sealed partial class MainPage : Page
{
    
    private readonly MemoryManager _memoryManager = new();

    
    private readonly ObservableCollection<Hole> _pendingHoles = new();
    private readonly ObservableCollection<SegmentInput> _pendingSegments = new();

    
    private readonly List<Color> _processColors = new()
    {
        Color.FromArgb(255, 0, 120, 212),   
        Color.FromArgb(255, 231, 72, 86),    
        Color.FromArgb(255, 0, 178, 148),    
        Color.FromArgb(255, 255, 140, 0),    
        Color.FromArgb(255, 135, 100, 184),  
        Color.FromArgb(255, 0, 153, 188),    
        Color.FromArgb(255, 218, 59, 99),    
        Color.FromArgb(255, 76, 175, 80),    
        Color.FromArgb(255, 255, 193, 7),    
        Color.FromArgb(255, 121, 134, 203),  
    };
    private readonly Dictionary<string, Color> _processColorMap = new();
    private int _nextColorIndex = 0;

    public MainPage()
    {
        InitializeComponent();
        HolesListView.ItemsSource = _pendingHoles;
        SegmentsListView.ItemsSource = _pendingSegments;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshUI();
    }

    

    private void AddHole_Click(object sender, RoutedEventArgs e)
    {
        if (double.IsNaN(HoleStartInput.Value) || double.IsNaN(HoleSizeInput.Value))
        {
            ShowStatus("Please enter both a starting address and size for the hole.", true);
            return;
        }

        int start = (int)HoleStartInput.Value;
        int size = (int)HoleSizeInput.Value;

        if (start < 0)
        {
            ShowStatus("Starting address must be >= 0.", true);
            return;
        }
        if (size <= 0)
        {
            ShowStatus("Hole size must be a positive integer.", true);
            return;
        }

        _pendingHoles.Add(new Hole(start, size));
        HoleStartInput.Value = double.NaN;
        HoleSizeInput.Value = double.NaN;
        ShowStatus($"Hole added: Start={start}, Size={size}.", false);
    }

    private void RemoveHole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Hole hole)
        {
            _pendingHoles.Remove(hole);
        }
    }

    private async void InitializeMemory_Click(object sender, RoutedEventArgs e)
    {
        if (double.IsNaN(TotalMemorySizeInput.Value))
        {
            ShowStatus("Please enter a total memory size.", true);
            return;
        }

        int totalSize = (int)TotalMemorySizeInput.Value;
        var holes = _pendingHoles.ToList();

        
        _memoryManager.Reset();

        var result = _memoryManager.Initialize(totalSize, holes);

        if (!result.Success)
        {
            await ShowErrorDialog("Initialization Error", result.Message!);
            return;
        }

        
        SetSetupFieldsEnabled(false);

        ShowStatus(result.Message!, false);
        _processColorMap.Clear();
        _nextColorIndex = 0;
        RefreshUI();
    }

    

    private void AddSegment_Click(object sender, RoutedEventArgs e)
    {
        string name = SegmentNameInput.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
        {
            ShowStatus("Please enter a segment name.", true);
            return;
        }
        if (double.IsNaN(SegmentSizeInput.Value))
        {
            ShowStatus("Please enter a segment size.", true);
            return;
        }

        int size = (int)SegmentSizeInput.Value;
        if (size <= 0)
        {
            ShowStatus("Segment size must be a positive integer.", true);
            return;
        }

        _pendingSegments.Add(new SegmentInput(name, size));
        SegmentNameInput.Text = "";
        SegmentSizeInput.Value = double.NaN;
        ShowStatus($"Segment '{name}' (size {size}) added.", false);
    }

    private void RemoveSegment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SegmentInput seg)
        {
            _pendingSegments.Remove(seg);
        }
    }

    private async void AllocateProcess_Click(object sender, RoutedEventArgs e)
    {
        if (!_memoryManager.IsInitialized)
        {
            await ShowErrorDialog("Not Initialized", "Please initialize memory before allocating processes.");
            return;
        }

        string processId = ProcessIdInput.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(processId))
        {
            ShowStatus("Please enter a Process ID.", true);
            return;
        }

        if (_pendingSegments.Count == 0)
        {
            ShowStatus("Please add at least one segment.", true);
            return;
        }

        
        var selectedItem = AlgorithmComboBox.SelectedItem as ComboBoxItem;
        var methodTag = selectedItem?.Tag?.ToString() ?? "FirstFit";
        var method = methodTag == "BestFit" ? AllocationMethod.BestFit : AllocationMethod.FirstFit;

        
        var segments = _pendingSegments.Select(s => new Segment(s.Name, s.Size, processId)).ToList();
        var process = new Process(processId, segments);

        
        var result = _memoryManager.AllocateProcess(process, method);

        if (!result.Success)
        {
            await ShowErrorDialog("Allocation Failed", result.Message!);
            return;
        }

        
        if (!_processColorMap.ContainsKey(processId))
        {
            _processColorMap[processId] = _processColors[_nextColorIndex % _processColors.Count];
            _nextColorIndex++;
        }

        
        ProcessIdInput.Text = "";
        _pendingSegments.Clear();

        ShowStatus(result.Message!, false);
        RefreshUI();
    }

    

    private async void DeallocateProcess_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string processId)
        {
            var result = _memoryManager.DeallocateProcess(processId);

            if (!result.Success)
            {
                await ShowErrorDialog("Deallocation Error", result.Message!);
                return;
            }

            _processColorMap.Remove(processId);
            ShowStatus(result.Message!, false);
            RefreshUI();
        }
    }

    

    private void ResetSimulation_Click(object sender, RoutedEventArgs e)
    {
        _memoryManager.Reset();
        _pendingHoles.Clear();
        _pendingSegments.Clear();
        _processColorMap.Clear();
        _nextColorIndex = 0;
        TotalMemorySizeInput.Value = double.NaN;
        ProcessIdInput.Text = "";
        SegmentNameInput.Text = "";
        SegmentSizeInput.Value = double.NaN;
        HoleStartInput.Value = double.NaN;
        HoleSizeInput.Value = double.NaN;
        SetSetupFieldsEnabled(true);
        AlgorithmComboBox.SelectedIndex = 0;
        ShowStatus("Simulation reset.", false);
        RefreshUI();
    }

    private void SetSetupFieldsEnabled(bool enabled)
    {
        TotalMemorySizeInput.IsEnabled = enabled;
        HoleStartInput.IsEnabled = enabled;
        HoleSizeInput.IsEnabled = enabled;
        AlgorithmComboBox.IsEnabled = enabled;
        AddHoleButton.IsEnabled = enabled;
        InitializeMemoryButton.IsEnabled = enabled;
        
        
        HolesListView.IsEnabled = enabled;
    }

    

    private void RefreshUI()
    {
        RefreshMemoryLayout();
        RefreshSegmentTables();
        RefreshSystemTables();
        RefreshActiveProcessesList();
    }

    private void RefreshMemoryLayout()
    {
        MemoryLayoutControl.Items.Clear();

        if (!_memoryManager.IsInitialized)
        {
            var placeholder = new TextBlock
            {
                Text = "Memory not initialized. Set up memory to see the layout.",
                Foreground = new SolidColorBrush(Color.FromArgb(180, 180, 180, 180)),
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Padding = new Thickness(16),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            MemoryLayoutControl.Items.Add(placeholder);
            return;
        }

        var layout = _memoryManager.GetMemoryLayout();
        int totalSize = _memoryManager.TotalMemorySize;
        double maxHeight = 500.0; 
        double minBlockHeight = 28.0; 

        foreach (var block in layout)
        {
            
            double proportion = (double)block.Size / totalSize;
            double height = Math.Max(proportion * maxHeight, minBlockHeight);

            
            SolidColorBrush bgBrush;
            SolidColorBrush fgBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            if (!block.IsAllocated)
            {
                
                bgBrush = new SolidColorBrush(Color.FromArgb(255, 61, 61, 61));
            }
            else if (block.ProcessId == "OS")
            {
                
                bgBrush = new SolidColorBrush(Color.FromArgb(255, 26, 26, 46));
                fgBrush = new SolidColorBrush(Color.FromArgb(180, 180, 180, 180));
            }
            else if (_processColorMap.TryGetValue(block.ProcessId!, out var color))
            {
                bgBrush = new SolidColorBrush(color);
            }
            else
            {
                bgBrush = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));
            }

            
            var blockGrid = new Grid
            {
                Padding = new Thickness(8, 2, 8, 2),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            blockGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            blockGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            blockGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var labelText = new TextBlock
            {
                Text = block.DisplayLabel,
                Foreground = fgBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(labelText, 0);

            var addressText = new TextBlock
            {
                Text = block.AddressRange,
                Foreground = fgBrush,
                FontSize = 11,
                Opacity = 0.8,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };
            Grid.SetColumn(addressText, 1);

            var sizeText = new TextBlock
            {
                Text = $"[{block.Size}]",
                Foreground = fgBrush,
                FontSize = 11,
                Opacity = 0.7,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(sizeText, 2);

            blockGrid.Children.Add(labelText);
            blockGrid.Children.Add(addressText);
            blockGrid.Children.Add(sizeText);

            var border = new Border
            {
                Background = bgBrush,
                Height = height,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 1, 0, 1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Child = blockGrid,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            MemoryLayoutControl.Items.Add(border);
        }
    }

    private void RefreshSegmentTables()
    {
        SegmentTablesControl.Items.Clear();
        var activeIds = _memoryManager.GetActiveProcessIds();

        NoSegmentTablesText.Visibility = activeIds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var processId in activeIds)
        {
            if (!_memoryManager.ActiveProcesses.TryGetValue(processId, out var process))
                continue;

            
            var processColor = _processColorMap.TryGetValue(processId, out var c)
                ? c : Color.FromArgb(255, 150, 150, 150);

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            headerPanel.Children.Add(new Border
            {
                Width = 12, Height = 12, CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(processColor),
                VerticalAlignment = VerticalAlignment.Center
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Process {processId}",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 14
            });

            
            var tableHeader = new Grid
            {
                Padding = new Thickness(8, 4, 8, 4),
            };
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var h1 = new TextBlock { Text = "Segment", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var h2 = new TextBlock { Text = "Base", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var h3 = new TextBlock { Text = "Limit", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            Grid.SetColumn(h1, 0);
            Grid.SetColumn(h2, 1);
            Grid.SetColumn(h3, 2);
            tableHeader.Children.Add(h1);
            tableHeader.Children.Add(h2);
            tableHeader.Children.Add(h3);

            var tablePanel = new StackPanel { Spacing = 0 };
            tablePanel.Children.Add(tableHeader);

            foreach (var seg in process.Segments)
            {
                var row = new Grid { Padding = new Thickness(8, 2, 8, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var c1 = new TextBlock { Text = seg.Name };
                var c2 = new TextBlock { Text = seg.BaseAddress.ToString() };
                var c3 = new TextBlock { Text = seg.Limit.ToString() };
                Grid.SetColumn(c1, 0);
                Grid.SetColumn(c2, 1);
                Grid.SetColumn(c3, 2);
                row.Children.Add(c1);
                row.Children.Add(c2);
                row.Children.Add(c3);
                tablePanel.Children.Add(row);
            }

            var card = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255))
            };

            var cardContent = new StackPanel { Spacing = 6 };
            cardContent.Children.Add(headerPanel);
            cardContent.Children.Add(tablePanel);
            card.Child = cardContent;

            SegmentTablesControl.Items.Add(card);
        }
    }

    private void RefreshSystemTables()
    {
        
        AllocatedPartitionsListView.Items.Clear();
        var allocated = _memoryManager.GetAllocatedPartitionsSnapshot();
        foreach (var seg in allocated)
        {
            var row = new Grid { Padding = new Thickness(8, 4, 8, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var c1 = new TextBlock { Text = seg.ProcessId };
            var c2 = new TextBlock { Text = seg.Name };
            var c3 = new TextBlock { Text = seg.BaseAddress.ToString() };
            var c4 = new TextBlock { Text = seg.Size.ToString() };
            Grid.SetColumn(c1, 0);
            Grid.SetColumn(c2, 1);
            Grid.SetColumn(c3, 2);
            Grid.SetColumn(c4, 3);
            row.Children.Add(c1);
            row.Children.Add(c2);
            row.Children.Add(c3);
            row.Children.Add(c4);

            AllocatedPartitionsListView.Items.Add(row);
        }

        
        FreePartitionsListView.Items.Clear();
        var free = _memoryManager.GetFreePartitionsSnapshot();
        foreach (var hole in free)
        {
            var row = new Grid { Padding = new Thickness(8, 4, 8, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var c1 = new TextBlock { Text = hole.StartingAddress.ToString() };
            var c2 = new TextBlock { Text = hole.Size.ToString() };
            var c3 = new TextBlock { Text = hole.EndAddress.ToString() };
            Grid.SetColumn(c1, 0);
            Grid.SetColumn(c2, 1);
            Grid.SetColumn(c3, 2);
            row.Children.Add(c1);
            row.Children.Add(c2);
            row.Children.Add(c3);

            FreePartitionsListView.Items.Add(row);
        }
    }

    private void RefreshActiveProcessesList()
    {
        var activeIds = _memoryManager.GetActiveProcessIds();
        ActiveProcessesListView.ItemsSource = activeIds;
        NoProcessesText.Visibility = activeIds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    

    private void ShowStatus(string message, bool isError)
    {
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = isError ? InfoBarSeverity.Error : InfoBarSeverity.Success;
        StatusInfoBar.IsOpen = true;
    }

    private async System.Threading.Tasks.Task ShowErrorDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    public record SegmentInput(string Name, int Size);
}
