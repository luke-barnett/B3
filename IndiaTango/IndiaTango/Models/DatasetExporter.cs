using System;
using System.Diagnostics;
using System.IO;

namespace IndiaTango.Models
{
	public class DatasetExporter
	{
		public readonly Dataset Data;

		public DatasetExporter(Dataset data)
		{
			if(data == null)
				throw new ArgumentNullException("Dataset cannot be null");

			Data = data;
		}

		public void Export(string filePath, ExportFormat format)
		{
			if (String.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException("filePath cannot be null");

			if (format == null)
				throw new ArgumentNullException("Export format cannot be null");

			if (format.Equals(ExportFormat.CSV))
			{
				using(StreamWriter writer = File.CreateText(filePath))
				{
					const char del = ',';
					string columnHeadings = "dd/mm/yy" + del + "hh:mm" + del;
				    int rowCount = Data.DataPointCount;
                    var outputData = new string[Data.Sensors.Count, rowCount];

					//Construct the column headings (Sensor names)
                    foreach (Sensor sensor in Data.Sensors)
                    {
                        columnHeadings += sensor.Name + del;
                      //  foreach ()
                       // {
                            
                       // }
                    }

				    //Strip the last delimiter
					columnHeadings = columnHeadings.Substring(0, columnHeadings.Length - 1);
					writer.WriteLine(columnHeadings);
					writer.Flush();

                    //write the data here...

                    Debug.WriteLine(filePath);

				    writer.Close();
				}
			}
			else if (format.Equals(ExportFormat.TXT))
			{
				//Do stuff
			}
			else if (format.Equals(ExportFormat.XLSX))
			{
				//Do stuff
			}

			//No more stuff!
		}

        private int GetArrayRowFromTime(DateTime startDate, DateTime currentDate)
        {
            if (startDate == null)
				throw new ArgumentNullException("startDate cannot be null");

            if (currentDate == null)
				throw new ArgumentNullException("currentDate cannot be null");

            if (currentDate <= startDate)
                throw new ArgumentException("currentDate must be larger than startDate");

            return (int)Math.Floor(currentDate.Subtract(startDate).TotalMinutes/15);
        }
	}

	public class ExportFormat
	{
		readonly string _extension;
		readonly string _name;

		#region PrivateConstructor

		private ExportFormat(string extension, string name)
		{
			_extension = extension;
			_name = name;
		}

		#endregion

		#region PublicProperties

		public string Extension { get { return _extension; } }

		public string Name { get { return _name; } }

		public static ExportFormat CSV { get { return new ExportFormat(".csv", "Comma Seperated Value File"); } }

		public static ExportFormat TXT { get { return new ExportFormat(".txt", "Tab Deliminated Text File"); } }

		public static ExportFormat XLSX { get { return new ExportFormat(".xlsx", "Excel Workbook"); } }
		
		#endregion

		#region PublicMethods

		public new string ToString()
		{
			return Name + "(*" + Extension + ")";
		}

        public override bool Equals(object obj)
        {
            return (obj is ExportFormat) && (obj as ExportFormat).Extension.CompareTo(Extension) == 0 &&
                   (obj as ExportFormat).Name.CompareTo(Name) == 0;
        }
		
		#endregion

	}
}