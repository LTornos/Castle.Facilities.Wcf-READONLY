﻿// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Facilities.WcfIntegration
{
	using System.ServiceModel;

	using Castle.Facilities.WcfIntegration.Client.Proxy;

	/// <summary>
	/// Helper class for obtaining <see cref = "IContextChannel" /> associated with client-side proxies
	/// </summary>
	public static class WcfContextChannel
	{
		/// <summary>
		///   Obtains <see cref = "IContextChannel" /> for given <paramref name = "target" />
		/// </summary>
		/// <param name = "target"></param>
		/// <returns></returns>
		public static IContextChannel For(object target)
		{
			var channelHolder = target as IWcfChannelHolder;
			return (channelHolder != null) ? channelHolder.Channel as IContextChannel : null;
		}
	}
}