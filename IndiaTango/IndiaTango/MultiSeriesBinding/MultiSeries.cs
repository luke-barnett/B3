using System;
using System.Collections.Specialized;
using System.Windows;
using System.Collections;
using System.Windows.Data;
using Visiblox.Charts;

namespace IndiaTango.MultiSeriesBinding
{
    /// <summary>
    /// Allows a Visiblox chart to be data binded to more than one dataseries
    /// Based on http://www.scottlogic.co.uk/blog/colin/2011/03/mvvm-charting-binding-multiple-series-to-a-visiblox-chart/
    /// Limited to LineSeries
    /// </summary>
    public static class MultiSeries
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(IEnumerable), typeof(MultiSeries),
            new PropertyMetadata("", new PropertyChangedCallback(OnSourcePropertyChanged)));

        public static IEnumerable GetSource(DependencyObject d)
        {
            return (IEnumerable) d.GetValue(SourceProperty);
        }

        public static void SetSource(DependencyObject d, IEnumerable value)
        {
            d.SetValue(SourceProperty, value);
        }

        private static void OnSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Chart targetChart = d as Chart;

            SynchroniseChartWithSource(targetChart);

            IEnumerable Source = GetSource(targetChart);
            INotifyCollectionChanged incc = Source as INotifyCollectionChanged;
            if (incc != null)
            {
                incc.CollectionChanged += (s, e2) => SynchroniseChartWithSource(targetChart);
            }

        }

        private static void SynchroniseChartWithSource(Chart chart)
        {
            chart.Series.Clear();

            IEnumerable Source = GetSource(chart);
            if (Source == null)
                return;

            // iterate over each source series
            foreach (object seriesDataSource in Source)
            {
                // create a visiblox chart series
                var chartSeries = (LineSeries) seriesDataSource;
                chart.Series.Add(chartSeries);
            }
        }
    }
}
