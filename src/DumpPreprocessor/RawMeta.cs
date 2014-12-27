using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpPreprocessor
{
	/// <summary>
	/// Contains the raw meta data 
	/// </summary>
	class RawMeta
	{
		public IDictionary<int, string> Namespaces { get; set; }
		public string DatabaseName { get; set; }
		public string Generator { get; set;}
	}
}
