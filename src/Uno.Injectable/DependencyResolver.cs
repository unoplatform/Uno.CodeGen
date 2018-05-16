using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno
{
	/// <summary>
	/// Encapsulates a method that that resolves an dependency given its type and optional name.
	/// </summary>
	/// <param name="type">The type of the dependency.</param>
	/// <param name="name">The optional name of the dependency.</param>
	/// <returns></returns>
	public delegate object DependencyResolver(Type type, string name = null);
}