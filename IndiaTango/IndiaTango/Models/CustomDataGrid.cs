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

        public event RoutedEventHandler DeleteRequested
        {
            add { AddHandler(DeleteRequestedEvent, value); }
            remove { RemoveHandler(DeleteRequestedEvent, value); }
        }

        protected override void OnExecutedBeginEdit(System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            CancelEdit();
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
}
