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

        /// <summary>
        /// Exports a data set to a CSV file.
        /// The file is saved in the same format as the original CSV files.
        /// </summary>
        /// <param name="filePath">The desired path and file name of the file to be saved. No not include an extension.</param>
        /// <param name="format">The format to save the file in.</param>
        /// <param name="includeEmptyLines">Wether to export the file with empty lines or not.</param>
		public void Export(string filePath, ExportFormat format,bool includeEmptyLines)
		{
			if (String.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException("filePath cannot be null");

			if (format == null)
				throw new ArgumentNullException("Export format cannot be null");

            //Strip the existing extension and add the one specified in the method args
            filePath = Path.GetFileNameWithoutExtension(filePath);
            filePath += format.Extension;

			if (format.Equals(ExportFormat.CSV) || format.Equals(ExportFormat.TXT))
			{
				using(StreamWriter writer = File.CreateText(filePath))
				{
					char del = format.Equals(ExportFormat.CSV) ? ',' : ' ';
					string columnHeadings = "dd/mm/yyyy" + del + "hhnn";  //Not a typo
				    int currentSensorIndex = 0;
				    var outputData = new string[Data.Sensors.Count, Data.DataPointCount];
				    DateTime rowDate = Data.StartTimeStamp;

                    foreach (Sensor sensor in Data.Sensors)
                    {
                        //Construct the column headings (Sensor names)
                        columnHeadings += del + sensor.Name;
                       
                        //Fill the array with the data
                        foreach (DataValue value in sensor.CurrentState.Values)
                            outputData[currentSensorIndex, GetArrayRowFromTime(Data.StartTimeStamp, value.Timestamp)] = value.Value.ToString();

                        currentSensorIndex++;
                    }

				    //Strip the last delimiter from the headings and write the line
					writer.WriteLine(columnHeadings);

                    //write the data here...
                    for (int row = 0; row < Data.DataPointCount; row++)
                    {
                        string line = "";

                        for (int col = 0; col < Data.Sensors.Count; col++)
                            line += del + outputData[col, row];

                        if(includeEmptyLines || line.Length != Data.Sensors.Count)
                        {
                            line = rowDate.ToString("dd/MM/yyyy") + del + rowDate.ToString("HH:mm") + line;
                            writer.WriteLine(line);
                        }

                        rowDate = rowDate.AddMinutes(15);
                    }

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
            if (currentDate < startDate)
                throw new ArgumentException("currentDate must be larger than or equal to startDate\nYou supplied startDate=" + startDate.ToString() + " currentDate=" + currentDate.ToString());

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