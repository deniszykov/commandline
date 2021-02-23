using System;
using System.Collections.Generic;

namespace deniszykov.CommandLine.Builders
{
	internal sealed class ServiceProvider : IServiceProvider
	{
		private readonly IServiceProvider? nextServiceProvider;
		private readonly Dictionary<Type, Func<object>> services;

		public ServiceProvider(IServiceProvider? nextServiceProvider = null)
		{
			this.nextServiceProvider = nextServiceProvider;
			this.services = new Dictionary<Type, Func<object>>();
		}

		public void RegisterInstance(Type serviceType, object service)
		{
			if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
			if (service == null) throw new ArgumentNullException(nameof(service));

			this.services[serviceType] = () => service;
		}

		/*
		public void Register(Type serviceType, Func<object> serviceFactory)
		{
			if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
			if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));

			this.services[serviceType] = serviceFactory;
		}
		public void Register<ServiceT>(Func<ServiceT> serviceFactory) where ServiceT : class
		{
			if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));

			this.services[typeof(ServiceT)] = serviceFactory;
		}*/

		/// <inheritdoc />
		public object? GetService(Type serviceType)
		{
			if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

			if (this.services.TryGetValue(serviceType, out var serviceFactory))
			{
				return serviceFactory();
			}
			return this.nextServiceProvider?.GetService(serviceType);
		}
	}
}
