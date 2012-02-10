using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IndiaTango.Models
{
    /// <summary>
    /// Custom datagrid to handle fire events on selection and delete
    /// </summary>
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
            
        }

        protected override void OnSelectedCellsChanged(SelectedCellsChangedEventArgs e)
        {
            foreach (var dataGridCellInfo in e.AddedCells.Where(dataGridCellInfo => (string) dataGridCellInfo.Column.Header == "Timestamp"))
            {
                SelectedCells.Remove(dataGridCellInfo);
            }

            base.OnSelectedCellsChanged(e);
        }

        protected override void OnExecutedDelete(System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            RaiseDeleteRequestedEvent();
        }

        private void RaiseDeleteRequestedEvent()
        {
            var eventArgs = new RoutedEventArgs(DeleteRequestedEvent);
            RaiseEvent(eventArgs);
        }

    }
}
