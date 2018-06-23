using System;
using System.Collections.Generic;
using System.Text;

namespace Samples.SimpleServer
{
	public class MagicOnionSettings
	{
		public string GrpcServerHost { get; set; } = "localhost";
		public int GrpcServerPort { get; set; } = 12345;
		public string MaxHeaderListSize { get; set; } = "1000000";

		public class SwaggerSettings
		{
			public string Title { get; set; }
			public string Description { get; set; } = "Swagger Integration";
			public string XmlServiceDefDoc { get; set; }
			public string ApiBasePath { get; set; } = "/";
		};

		public SwaggerSettings Swagger { get; set; }
	}
}
