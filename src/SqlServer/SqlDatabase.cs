using Common;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SqlServer
{
	sealed class SqlDatabase : IDisposable
	{
		private const string connectionString = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=C:\Users\dixxi\AppData\Local\Temp\wiki.mdf;Integrated Security=True;";

		private SqlConnection connection;

		public SqlDatabase()
		{
			connection = new SqlConnection(connectionString);
			connection.Open();
		}
		public void RunFile(string file)
		{
			using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
			using (var reader = new StreamReader(stream))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					Utils.UpdateProgress(stream);
					Run(line);
				}
			}
		}

		public void Run(string sql)
		{
			try
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = sql;
					command.CommandTimeout = 60 * 60 * 24 * 4; // 4 days
					command.ExecuteNonQuery();
				}
			}
			catch (SqlException e)
			{
				Console.WriteLine("\nSql command failed: " + sql);
				Console.WriteLine(e);
				throw;
			}
		}

		public void Dispose()
		{
			connection.Close();
		}
	}
}
