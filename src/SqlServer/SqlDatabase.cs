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
	sealed class SqlDatabase
	{
		private const string connectionString = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=D:\wiki.mdf;Integrated Security=True;";

		public void Load(string file, bool parallel = false)
		{
			const int maxThreads = 8;
			sem = new Semaphore(maxThreads, maxThreads);
			using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
			using (var reader = new StreamReader(stream))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					Utils.UpdateProgress(stream);
					RunCommand(line, parallel);
				}
			}

			for (int i = 0; i < maxThreads; i++)
				sem.WaitOne();
			sem.Dispose();
		}

		private Semaphore sem;

		private void RunCommand(string sql, bool parallel)
		{
			Action cmd = () =>
			{
				try
				{
					using (var connection = new SqlConnection(connectionString))
					{
						connection.Open();
						var command = connection.CreateCommand();
						command.CommandText = sql;
						command.ExecuteNonQuery();
					}
				}
				catch (SqlException e)
				{
					Console.WriteLine("\nSql command failed: " + sql);
					Console.WriteLine(e);
				}
				finally
				{
					sem.Release(1);
				}
			};

			sem.WaitOne();

			if (parallel)
				Task.Run(cmd);
			else
				cmd();
		}
	}
}
