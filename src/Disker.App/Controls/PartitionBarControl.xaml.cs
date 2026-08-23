using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Disker.Core.Models;
using Windows.UI;

namespace Disker.App.Controls
{
    public sealed partial class PartitionBarControl : UserControl
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
            this.InitializeComponent();
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

            double totalWidth = this.ActualWidth - 2; // Border padding payı
            ulong totalDiskBytes = Disk.SizeBytes;

            var colors = new[]
            {
                Color.FromArgb(255, 59, 130, 246),  // Blue
                Color.FromArgb(255, 16, 185, 129),  // Emerald
                Color.FromArgb(255, 168, 85, 247),  // Purple
                Color.FromArgb(255, 245, 158, 11),  // Amber
                Color.FromArgb(255, 236, 72, 153),  // Pink
                Color.FromArgb(255, 99, 102, 241)   // Indigo
            };

            int colorIndex = 0;
            ulong allocatedBytes = 0;

            foreach (var partition in Disk.Partitions)
            {
                allocatedBytes += partition.SizeBytes;
                double ratio = (double)partition.SizeBytes / totalDiskBytes;
                double segmentWidth = Math.Max(ratio * totalWidth, 24); // Minimum 24px

                var segment = CreateSegmentElement(partition, segmentWidth, colors[colorIndex % colors.Length]);
                SegmentsContainer.Children.Add(segment);
                colorIndex++;
            }

            // Ayrılmamış Alan (Unallocated Space) varsa
            if (totalDiskBytes > allocatedBytes && (totalDiskBytes - allocatedBytes) > 1024 * 1024)
            {
                ulong unallocatedBytes = totalDiskBytes - allocatedBytes;
                double ratio = (double)unallocatedBytes / totalDiskBytes;
                double unallocatedWidth = Math.Max(ratio * totalWidth, 20);

                var unallocatedSegment = CreateUnallocatedElement(unallocatedBytes, unallocatedWidth);
                SegmentsContainer.Children.Add(unallocatedSegment);
            }
        }

        private UIElement CreateSegmentElement(PartitionInfo partition, double width, Color baseColor)
        {
            var grid = new Grid
            {
                Width = width,
                Height = 34,
                Background = new SolidColorBrush(baseColor),
                Margin = new Thickness(0, 0, 1, 0)
            };

            var textBlock = new TextBlock
            {
                Text = !string.IsNullOrEmpty(partition.DriveLetter) 
                    ? $"{partition.DriveLetter} ({partition.SizeFormatted})"
                    : (!string.IsNullOrEmpty(partition.VolumeLabel) ? partition.VolumeLabel : partition.SizeFormatted),
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 4, 0)
            };

            grid.Children.Add(textBlock);

            ToolTipService.SetToolTip(grid, 
                $"{partition.DisplayName}\n" +
                $"Boyut: {partition.SizeFormatted}\n" +
                $"Dosya Sistemi: {partition.FileSystem}\n" +
                $"Kullanılan: {partition.UsedPercentage:F1}% ({PhysicalDiskInfo.FormatBytes(partition.UsedSpaceBytes)})\n" +
                $"Tür: {partition.PartitionType}");

            return grid;
        }

        private UIElement CreateUnallocatedElement(ulong unallocatedBytes, double width)
        {
            var grid = new Grid
            {
                Width = width,
                Height = 34,
                Background = new SolidColorBrush(Color.FromArgb(255, 75, 85, 99)), // Slate Gray
                Margin = new Thickness(0, 0, 1, 0)
            };

            var textBlock = new TextBlock
            {
                Text = $"Ayrılmamış ({PhysicalDiskInfo.FormatBytes(unallocatedBytes)})",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 209, 213, 219)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 4, 0)
            };

            grid.Children.Add(textBlock);
            ToolTipService.SetToolTip(grid, $"Ayrılmamış Alan: {PhysicalDiskInfo.FormatBytes(unallocatedBytes)}");

            return grid;
        }
    }
}
