// --------------------------------------------------------------------------------------------------
// � Copyright 2011 by Matthew Dennis.
// Released under the Microsoft Public License (Ms-PL) http://www.opensource.org/licenses/ms-pl.html
// --------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Munq
{
	internal class TypeRegistry : IDisposable
	{
		// Track whether Dispose has been called.
		private const int INITIAL_SIZE = 257; // a prime number greater than the initial size
		private bool disposed;
		private readonly IDictionary<IRegistrationKey, Registration> typeRegistrations =
			new ConcurrentDictionary<IRegistrationKey, Registration>(Environment.ProcessorCount * 2,
																		INITIAL_SIZE);

		public void Add(Registration reg)
		{
			IRegistrationKey key   = MakeKey(reg.Name, reg.ResolvesTo);
			typeRegistrations[key] = reg;
		}

		public Registration Get(string name, Type type)
		{
			var key = MakeKey(name, type);
		    Registration registration;
		    typeRegistrations.TryGetValue(key, out registration);
		    return registration;
		}

        public IEnumerable<Registration> GetDerived(string name, Type type)
        {
            var regs = typeRegistrations.Values
                       .Where(r => String.Compare(r.Name, name, true) == 0 &&
                                   type.IsAssignableFrom(r.ResolvesTo));
            return regs;
        }

        public IEnumerable<Registration> GetDerived(Type type)
        {
            var regs = typeRegistrations.Values
                       .Where(r => type.IsAssignableFrom(r.ResolvesTo));
            return regs;
        }

        public bool ContainsKey(string name, Type type)
		{
			IRegistrationKey key = MakeKey(name, type);
			return typeRegistrations.Keys.Contains(key);
		}

		public IEnumerable<Registration> All(Type type)
		{
			return typeRegistrations.Values.Where(reg => reg.ResolvesTo == type);
		}

		public void Remove(IRegistration ireg)
		{
			IRegistrationKey key = MakeKey(ireg.Name, ireg.ResolvesTo);
			typeRegistrations.Remove(key);
			ireg.InvalidateInstanceCache();
		}

		private static IRegistrationKey MakeKey(string name, Type type)
		{
			return (name == null ? new UnNamedRegistrationKey(type)
								 : (IRegistrationKey)new NamedRegistrationKey(name, type));
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!disposed)
			{
				// If disposing equals true, dispose all ContainerLifetime instances
				if (disposing)
				{
					foreach (Registration reg in typeRegistrations.Values)
					{
						var instance = reg.Instance as IDisposable;
						if (instance != null)
						{
							instance.Dispose();
							reg.Instance = null;
						}
						reg.InvalidateInstanceCache();
					}
				}
			}
			disposed = true;
		}
		~TypeRegistry() { Dispose(false); }
	}
}
