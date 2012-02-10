using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace IndiaTango.Models
{
    /// <summary>
    /// Custom datagrid to handle sorting better
    /// </summary>
    public class CustomSortDataGrid : DataGrid
    {
        protected override void OnSorting(DataGridSortingEventArgs eventArgs)
        {
            var column = eventArgs.Column;
            var direction = (column.SortDirection != ListSortDirection.Ascending)
                                              ? ListSortDirection.Ascending
                                              : ListSortDirection.Descending;
            column.SortDirection = direction;
            var lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(ItemsSource);
            var mySort = new MySort(direction, column);
            lcv.CustomSort = mySort;
        }

    }

    public class MySort : IComparer
    {
        public MySort(ListSortDirection direction, DataGridColumn column)
        {
            Direction = direction;
            Column = column;
        }

        public ListSortDirection Direction
        {
            get;
            private set;
        }

        public DataGridColumn Column
        {
            get;
            private set;
        }

        public int Compare(object x, object y)
        {
            var graphableSensorX = (x as GraphableSensor);
            var graphableSensorY = (y as GraphableSensor);

            if (graphableSensorX == null)
                return -1;

            if (graphableSensorY == null)
                return 1;

            switch (Column.Header as string)
            {
                case null:
                    return 0;
                case "Name":
                    return Direction == ListSortDirection.Ascending ? String.CompareOrdinal(graphableSensorX.Sensor.Name, graphableSensorY.Sensor.Name) : String.CompareOrdinal(graphableSensorY.Sensor.Name, graphableSensorX.Sensor.Name);
                case "Sort Index":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.SortIndex.CompareTo(graphableSensorY.Sensor.SortIndex) : graphableSensorY.Sensor.SortIndex.CompareTo(graphableSensorX.Sensor.SortIndex);
                case "Variable":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.Variable.CompareTo(graphableSensorY.Sensor.Variable) : graphableSensorY.Sensor.Variable.CompareTo(graphableSensorX.Sensor.Variable);
                case "Parameter":
                    return Direction == ListSortDirection.Ascending ? String.CompareOrdinal(graphableSensorX.Sensor.SensorType, graphableSensorY.Sensor.SensorType) : String.CompareOrdinal(graphableSensorY.Sensor.SensorType, graphableSensorX.Sensor.SensorType);
                case "Unit":
                    return Direction == ListSortDirection.Ascending ? String.CompareOrdinal(graphableSensorX.Sensor.Unit, graphableSensorY.Sensor.Unit) : String.CompareOrdinal(graphableSensorY.Sensor.Unit, graphableSensorX.Sensor.Unit);
                case "Elevation (m)":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.Elevation.CompareTo(graphableSensorY.Sensor.Elevation) : graphableSensorY.Sensor.Elevation.CompareTo(graphableSensorX.Sensor.Elevation);
                case "Accuracy (+/-)":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.CurrentMetaData.Accuracy.CompareTo(graphableSensorY.Sensor.CurrentMetaData.Accuracy) : graphableSensorY.Sensor.CurrentMetaData.Accuracy.CompareTo(graphableSensorX.Sensor.CurrentMetaData.Accuracy);
                case "Lower Limit":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.LowerLimit.CompareTo(graphableSensorY.Sensor.LowerLimit) : graphableSensorY.Sensor.LowerLimit.CompareTo(graphableSensorX.Sensor.LowerLimit);
                case "Upper Limit":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.UpperLimit.CompareTo(graphableSensorY.Sensor.UpperLimit) : graphableSensorY.Sensor.UpperLimit.CompareTo(graphableSensorX.Sensor.UpperLimit);
                case "Max Rate of Change":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.MaxRateOfChange.CompareTo(graphableSensorY.Sensor.MaxRateOfChange) : graphableSensorY.Sensor.MaxRateOfChange.CompareTo(graphableSensorX.Sensor.MaxRateOfChange);
                case "Serial Number":
                    return Direction == ListSortDirection.Ascending ? String.CompareOrdinal(graphableSensorX.Sensor.CurrentMetaData.SerialNumber, graphableSensorY.Sensor.CurrentMetaData.SerialNumber) : String.CompareOrdinal(graphableSensorY.Sensor.CurrentMetaData.SerialNumber, graphableSensorX.Sensor.CurrentMetaData.SerialNumber);
                case "Manfacturer":
                    return Direction == ListSortDirection.Ascending ? String.CompareOrdinal(graphableSensorX.Sensor.CurrentMetaData.Manufacturer, graphableSensorY.Sensor.CurrentMetaData.Manufacturer) : String.CompareOrdinal(graphableSensorY.Sensor.CurrentMetaData.Manufacturer, graphableSensorX.Sensor.CurrentMetaData.Manufacturer);
                case "Description":
                    return Direction == ListSortDirection.Ascending ? String.CompareOrdinal(graphableSensorX.Sensor.Description, graphableSensorY.Sensor.Description) : String.CompareOrdinal(graphableSensorY.Sensor.Description, graphableSensorX.Sensor.Description);
                case "Summary Type":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.SummaryType.CompareTo(graphableSensorY.Sensor.SummaryType) : graphableSensorY.Sensor.SummaryType.CompareTo(graphableSensorX.Sensor.SummaryType);
                case "Ideal Calibration Frequency (Days)":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.CurrentMetaData.IdealCalibrationFrequency.CompareTo(graphableSensorY.Sensor.CurrentMetaData.IdealCalibrationFrequency) : graphableSensorY.Sensor.CurrentMetaData.IdealCalibrationFrequency.CompareTo(graphableSensorX.Sensor.CurrentMetaData.IdealCalibrationFrequency);
                case "Date of Installation":
                    return Direction == ListSortDirection.Ascending ? graphableSensorX.Sensor.CurrentMetaData.DateOfInstallation.CompareTo(graphableSensorY.Sensor.CurrentMetaData.DateOfInstallation) : graphableSensorY.Sensor.CurrentMetaData.DateOfInstallation.CompareTo(graphableSensorX.Sensor.CurrentMetaData.DateOfInstallation);
            }

            return 0;
        }
    }
}
