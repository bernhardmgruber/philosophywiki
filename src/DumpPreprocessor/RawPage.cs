using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpPreprocessor
{
	/// <summary>
	/// Represents a raw wikipedia page. Corresponds to the <page> element of the dump file
	/// </summary>
	class RawPage
	{
		public string Title { get; set; }
		public int NamespaceId {get; set;}
		public int Id { get; set; }
		public string Text { get; set; }
	}
}
