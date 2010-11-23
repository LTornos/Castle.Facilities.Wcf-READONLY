// Copyright 2004-2010 Castle Project - http://www.castleproject.org/
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

namespace Castle.Facilities.WcfIntegration.Proxy
{
	using System;
	using System.Linq;
	using System.Runtime.Remoting;
	using System.ServiceModel;
	using Castle.Core;
	using Castle.DynamicProxy;
	using Castle.Facilities.WcfIntegration.Async;
	using Castle.Facilities.WcfIntegration.Async.TypeSystem;
	using Castle.Facilities.WcfIntegration.Internal;
	using Castle.MicroKernel;
	using Castle.MicroKernel.Context;
	using Castle.MicroKernel.Proxy;
	using Castle.Windsor.Proxy;

	public class WcfProxyFactory : AbstractProxyFactory
	{
		private readonly ProxyGenerator generator;
		private readonly WcfClientExtension clients;
		private AsyncType asyncType;
		private readonly WcfProxyGenerationHook wcfProxyGenerationHook;

		public WcfProxyFactory(ProxyGenerator generator, WcfClientExtension clients)
		{
			this.generator = generator;
			this.clients = clients;
			wcfProxyGenerationHook = new WcfProxyGenerationHook(null);
		}

		public override object Create(IProxyFactoryExtension customFactory, IKernel kernel, ComponentModel model,
									  CreationContext context, params object[] constructorArguments)
		{
			throw new NotSupportedException();
		}

		public override object Create(IKernel kernel, object instance, ComponentModel model, 
									  CreationContext context, params object[] constructorArguments)
		{
			var channelHolder = instance as IWcfChannelHolder;

			if (channelHolder == null)
			{
				throw new ArgumentException(string.Format("Given instance is not an {0}", typeof(IWcfChannelHolder)), "instance");
			}

			if (channelHolder.RealProxy == null)
			{
				return channelHolder.Channel;
			}

			var isDuplex = IsDuplex(channelHolder.RealProxy);
			var proxyOptions = ProxyUtil.ObtainProxyOptions(model, true);
			var generationOptions = CreateProxyGenerationOptions(model.Service, proxyOptions, kernel, context);
			var additionalInterfaces = GetInterfaces(model.Service, proxyOptions, isDuplex);
			var interceptors = GetInterceptors(kernel, model, channelHolder, context);

			return generator.CreateInterfaceProxyWithTarget(typeof(IWcfChannelHolder),
				additionalInterfaces, channelHolder, generationOptions, interceptors);
		}

		public override bool RequiresTargetInstance(IKernel kernel, ComponentModel model)
		{
			return true;
		}

		protected static bool IsDuplex(object realProxy)
		{
			var typeInfo = (IRemotingTypeInfo)realProxy;
			return typeInfo.CanCastTo(typeof(IDuplexContextChannel), null);
		}

		protected virtual Type[] GetInterfaces(Type service, ProxyOptions proxyOptions, bool isDuplex)
		{
			// TODO: this should be static and happen in IContributeComponentModelConstruction preferably
			var additionalInterfaces = proxyOptions.AdditionalInterfaces ?? Type.EmptyTypes;
			Array.Resize(ref additionalInterfaces, additionalInterfaces.Length + (isDuplex ? 4 : 3));
			int index = additionalInterfaces.Length;
			additionalInterfaces[--index] = service;
			additionalInterfaces[--index] = typeof(IServiceChannel);
			additionalInterfaces[--index] = typeof(IClientChannel);

			if (isDuplex)
				additionalInterfaces[--index] = typeof(IDuplexContextChannel);

			return additionalInterfaces;
		}

		private IInterceptor[] GetInterceptors(IKernel kernel, ComponentModel model,IWcfChannelHolder channelHolder, CreationContext context)
		{
			var interceptors = ObtainInterceptors(kernel, model, context);

			// TODO: this should be static and happen in IContributeComponentModelConstruction preferably
			var clientModel = (IWcfClientModel)model.ExtendedProperties[WcfConstants.ClientModelKey];
			Array.Resize(ref interceptors, interceptors.Length + (clientModel.WantsAsyncCapability ? 2 : 1));
			int index = interceptors.Length;

			interceptors[--index] = new WcfRemotingInterceptor(clients, channelHolder);

			if (clientModel.WantsAsyncCapability)
			{
				var getAsyncType = WcfUtils.SafeInitialize(ref asyncType,
					() => AsyncType.GetAsyncType(model.Service));
				interceptors[--index] = new WcfRemotingAsyncInterceptor(getAsyncType, clients, channelHolder);
			}

			return interceptors;
		}

		private ProxyGenerationOptions CreateProxyGenerationOptions(Type service, ProxyOptions proxyOptions, IKernel kernel, CreationContext context)
		{
			if (proxyOptions.MixIns != null && proxyOptions.MixIns.Count() > 0)
			{
				throw new NotImplementedException(
					"Support for mixins is not yet implemented. How about contributing a patch?");
			}

			var userProvidedSelector = (proxyOptions.Selector != null) ? proxyOptions.Selector.Resolve(kernel, context) : null;

			var proxyGenOptions = new ProxyGenerationOptions(wcfProxyGenerationHook)
			{
				Selector = new WcfInterceptorSelector(service, userProvidedSelector)
			};

			return proxyGenOptions;
		}
	}
}
