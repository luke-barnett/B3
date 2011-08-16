using System;
using System.IO;

namespace IndiaTango.Models
{
	public class DatasetExporter
	{
		public DatasetExporter(Dataset data)
		{
			if(data == null)
				throw new ArgumentNullException("Dataset cannot be null");
		}

		public void Export(string filePath, ExportFormat format)
		{
			if (String.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException("filePath cannot be null");

			if (format == null)
				throw new ArgumentNullException("Export format cannot be null");

			if (format == ExportFormat.CSV)
			{

			}
			else if (format == ExportFormat.CSV)
			{

			}
			else if (format == ExportFormat.CSV)
			{

			}
		}
	}

	public abstract class ExportFormat
	{
		public abstract string Extension { get; }
		public abstract string Name { get; }

		#region PrivateClasses
		private class CSVFormat : ExportFormat
		{
			public override string Extension { get { return ".csv"; } }

			public override string Name { get { return "Comma Seperated Value File"; } }
		}

		private class TXTFormat : ExportFormat
		{
			public override string Extension { get { return ".txt"; } }

			public override string Name { get { return "Tab Deliminated Text File"; } }
		}

		private class XLSXFormat : ExportFormat
		{
			public override string Extension { get { return ".xlsx"; } }

			public override string Name { get { return "Excel Workbook"; } }
		}
		#endregion

		#region PublicProperties
		public static ExportFormat CSV { get { return new CSVFormat(); } }

		public static ExportFormat TXT { get { return new TXTFormat(); } }

		public static ExportFormat XLSX { get { return new XLSXFormat(); } }
		#endregion

		#region PublicMethods
		public new string ToString()
		{
			return Name + "(*" + Extension + ")";
		}
		#endregion

	}
}