using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IndiaTango.Models
{
    /// <summary>
    /// Set of useful tools for calculations
    /// </summary>
	public static class Tools
	{
        /// <summary>
        /// Calculates the MD5 hash for a file
        /// </summary>
        /// <param name="filename">The file to calculate from</param>
        /// <returns>The MD5 hash calculated from the file</returns>
		public static string GenerateMD5HashFromFile(string filename)
		{
			var file = new FileStream(filename, FileMode.Open);
			var builder = new StringBuilder();
			MD5 hasher = new MD5CryptoServiceProvider();

		    var data = new byte[file.Length];
		    file.Read(data, 0, data.Length);

		    var buffer = hasher.ComputeHash(data);
			file.Close();

			foreach (Byte b in buffer)
				builder.Append(b.ToString("x2"));

			return builder.ToString();
		}
	}
}
