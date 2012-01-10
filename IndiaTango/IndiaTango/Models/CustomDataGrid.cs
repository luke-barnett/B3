using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IndiaTango.Models
{
    public class CustomDataGrid : DataGrid
    {
        public static readonly RoutedEvent DeleteRequestedEvent = EventManager.RegisterRoutedEvent("DeleteRequested",
                                                                                                   RoutingStrategy.Bubble,
                                                                                                   typeof(RoutedEventHandler),
                                                                                                   typeof(CustomDataGrid));

        public static readonly RoutedEvent EditRequestedEvent = EventManager.RegisterRoutedEvent("EditRequested",
                                                                                                   RoutingStrategy.Bubble,
                                                                                                   typeof(RoutedEventHandler),
                                                                                                   typeof(CustomDataGrid));

        public event RoutedEventHandler DeleteRequested
        {
            add { AddHandler(DeleteRequestedEvent, value); }
            remove { RemoveHandler(DeleteRequestedEvent, value); }
        }

        public event RoutedEventHandler EditRequested
        {
            add { AddHandler(EditRequestedEvent, value); }
            remove { RemoveHandler(EditRequestedEvent, value); }
        }

        protected override void OnExecutedBeginEdit(System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var cell = e.OriginalSource as DataGridCell;
            if (cell == null || (string) cell.Column.Header == "Timestamp") return;

            var sensor = (string)cell.Column.Header;

            var dataRowView = SelectedItem as DataRowView;
            if (dataRowView == null) return;

            var timestamp = (DateTime)(dataRowView.Row[0]);

            RaiseEditRequestedEvent(timestamp, sensor);
        }

        protected override void OnExecutedDelete(System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            RaiseDeleteRequestedEvent(SelectedItems.Cast<object>().Where(i => i.GetType() == typeof(DataRowView)).Cast<DataRowView>().Select(x => (DateTime) x.Row[0]).ToList());
        }

        private void RaiseDeleteRequestedEvent(List<DateTime> timestamps)
        {
            var eventArgs = new DeleteRequestedEventArgs(DeleteRequestedEvent, timestamps);
            RaiseEvent(eventArgs);
        }

        private void RaiseEditRequestedEvent(DateTime timestamp, string sensorName)
        {
            var eventArgs = new EditRequestedEventArgs(EditRequestedEvent, timestamp, sensorName);
            RaiseEvent(eventArgs);
        }
    }

    public class DeleteRequestedEventArgs : RoutedEventArgs
    {
        public List<DateTime> TimeStamps;

        public DeleteRequestedEventArgs(RoutedEvent routedEvent, List<DateTime> timestamps)
            : base(routedEvent)
        {
            TimeStamps = timestamps;
        }
    }

    public class EditRequestedEventArgs : RoutedEventArgs
    {
        public DateTime TimeStamp;
        public string SensorName;

        public EditRequestedEventArgs(RoutedEvent routedEvent, DateTime timestamp, string sensorName) : base(routedEvent)
        {
            TimeStamp = timestamp;
            SensorName = sensorName;
        }
    }
}
