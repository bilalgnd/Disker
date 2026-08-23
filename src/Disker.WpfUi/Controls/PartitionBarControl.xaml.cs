using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Disker.Core.Models;

namespace Disker.App.Controls
{
    public partial class PartitionBarControl : UserControl
    {
        public static readonly DependencyProperty DiskProperty =
            DependencyProperty.Register(
                nameof(Disk),
                typeof(PhysicalDiskInfo),
                typeof(PartitionBarControl),
                new PropertyMetadata(null, OnDiskChanged));

        public PhysicalDiskInfo? Disk
        {
            get => (PhysicalDiskInfo?)GetValue(DiskProperty);
            set => SetValue(DiskProperty, value);
        }

        public PartitionBarControl()
        {
            InitializeComponent();
            this.SizeChanged += (s, e) => RenderSegments();
        }

        private static void OnDiskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PartitionBarControl control)
            {
                control.RenderSegments();
            }
        }

        private void RenderSegments()
        {
            SegmentsContainer.Children.Clear();

            if (Disk == null || Disk.SizeBytes == 0 || this.ActualWidth <= 0)
                return;

            double totalWidth = this.ActualWidth;
            ulong totalDiskBytes = Disk.SizeBytes;

            var gradients = new[]
            {
                new LinearGradientBrush(Color.FromRgb(59, 130, 246), Color.FromRgb(37, 99, 235), 0),   // Electric Blue
                new LinearGradientBrush(Color.FromRgb(16, 185, 129), Color.FromRgb(5, 150, 105), 0),   // Emerald Teal
                new LinearGradientBrush(Color.FromRgb(139, 92, 246), Color.FromRgb(109, 40, 217), 0),  // Violet
                new LinearGradientBrush(Color.FromRgb(245, 158, 11), Color.FromRgb(217, 119, 6), 0),   // Amber
                new LinearGradientBrush(Color.FromRgb(236, 72, 153), Color.FromRgb(219, 39, 119), 0),  // Rose Pink
                new LinearGradientBrush(Color.FromRgb(6, 182, 212), Color.FromRgb(8, 145, 178), 0)     // Cyan
            };

            int colorIndex = 0;
            ulong allocatedBytes = 0;

            foreach (var partition in Disk.Partitions)
            {
                allocatedBytes += partition.SizeBytes;
                double ratio = (double)partition.SizeBytes / totalDiskBytes;
                double segmentWidth = Math.Max(ratio * totalWidth, 12);

                var segment = CreateSegmentElement(partition, segmentWidth, gradients[colorIndex % gradients.Length]);
                SegmentsContainer.Children.Add(segment);
                colorIndex++;
            }

            // Ayrılmamış Alan
            if (totalDiskBytes > allocatedBytes && (totalDiskBytes - allocatedBytes) > 1024 * 1024)
            {
                ulong unallocatedBytes = totalDiskBytes - allocatedBytes;
                double ratio = (double)unallocatedBytes / totalDiskBytes;
                double unallocatedWidth = Math.Max(ratio * totalWidth, 10);

                var unallocatedSegment = CreateUnallocatedElement(unallocatedBytes, unallocatedWidth);
                SegmentsContainer.Children.Add(unallocatedSegment);
            }
        }

        private UIElement CreateSegmentElement(PartitionInfo partition, double width, Brush fillBrush)
        {
            var grid = new Grid
            {
                Width = width,
                Height = 18,
                Background = fillBrush,
                Margin = new Thickness(0, 0, 1, 0)
            };

            if (width >= 45)
            {
                string shortLabel = !string.IsNullOrEmpty(partition.DriveLetter)
                    ? partition.DriveLetter
                    : (!string.IsNullOrEmpty(partition.VolumeLabel) ? partition.VolumeLabel : $"P{partition.PartitionNumber}");

                var textBlock = new TextBlock
                {
                    Text = shortLabel,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(2, 0, 2, 0),
                    Opacity = 0.95
                };
                grid.Children.Add(textBlock);
            }

            grid.ToolTip = $"{partition.DisplayName} ({partition.FileSystem})\n" +
                           $"• Toplam Boyut: {partition.SizeFormatted}\n" +
                           $"• Dolu: {partition.UsedSpaceFormatted} (%{partition.UsedPercentage:F1})\n" +
                           $"• Boş: {partition.FreeSpaceFormatted} (%{partition.FreePercentage:F1})";

            return grid;
        }

        private UIElement CreateUnallocatedElement(ulong unallocatedBytes, double width)
        {
            var grid = new Grid
            {
                Width = width,
                Height = 18,
                Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                Margin = new Thickness(0, 0, 1, 0)
            };

            grid.ToolTip = $"Ayrılmamış Alan: {PhysicalDiskInfo.FormatBytes(unallocatedBytes)}";
            return grid;
        }
    }
}
