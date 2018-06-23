// Copyright (c) DMO Consulting LLC. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Dmo.Extensions.Logging
{
	public class GrpcNetCoreLogger : Grpc.Core.Logging.ILogger
	{
		private readonly ILogger _logger;

		private readonly ILoggerFactory _loggerFactory;
		
		public GrpcNetCoreLogger(ILoggerFactory loggerFactory,ILogger logger = null)
		{
			_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			_logger = logger ?? loggerFactory.CreateLogger("Grpc");
		}

		public void Debug(string message) => _logger.LogDebug(message);
		public void Debug(string format,params object[] formatArgs) => _logger.LogDebug(format,formatArgs);

		public void Error(string message) => _logger.LogDebug(message);
		public void Error(string format,params object[] formatArgs) => _logger.LogError(format,formatArgs);
		public void Error(Exception exception,string message) => _logger.LogError(exception,message);

		public Grpc.Core.Logging.ILogger ForType<T>() => new GrpcNetCoreLogger(_loggerFactory,_loggerFactory.CreateLogger<T>());

		public void Info(string message) => _logger.LogInformation(message);
		public void Info(string format,params object[] formatArgs) => _logger.LogInformation(format,formatArgs);

		public void Warning(string message) => _logger.LogWarning(message);
		public void Warning(string format,params object[] formatArgs) => _logger.LogWarning(format,formatArgs);
		public void Warning(Exception exception,string message) => _logger.LogWarning(exception,message);
	}
}
