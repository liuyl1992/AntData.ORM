using System;
using System.Configuration;

namespace AntData.ORM.Configuration
{
	public abstract class ElementCollectionBase<T>: ConfigurationElementCollection
		where T : ConfigurationElement, new()
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new T();
		}

		protected abstract object GetElementKey(T element);

		protected sealed override object GetElementKey(ConfigurationElement element)
		{
			return GetElementKey((T)element);
		}

		public new T this[string name]
		{
			get { return (T)BaseGet(name); }
		}

		public  T this[int index]
		{
			get { return (T)BaseGet(index); }
		}
	}
}
