// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;

namespace Uno.Equality
{
	/// <summary>
	/// <see cref="IEqualityComparer{T}"/> implementation for evaluating dictionaries
	/// </summary>
	public class DictionaryEqualityComparer<TDict, TKey, TValue> : IEqualityComparer<TDict>
		where TDict : IDictionary<TKey, TValue>
	{
		private readonly bool _nullIsEmpty;
		private readonly IEqualityComparer<TValue> _valueComparer;

		/// <summary>
		/// Default instance of the comparer with a default equality comparer for values.
		/// </summary>
		public static IEqualityComparer<TDict> Default { get; } = new DictionaryEqualityComparer<TDict, TKey, TValue>();

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="valueComparer">Comparer to use to comparer an enumerated item</param>
		/// <param name="nullIsEmpty">If null value should be compated as empty collection</param>
		public DictionaryEqualityComparer(IEqualityComparer<TValue> valueComparer = null, bool nullIsEmpty = true)
		{
			_nullIsEmpty = nullIsEmpty;
			_valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
		}

		/// <inheritdoc/>
		public bool Equals(TDict x, TDict y)
		{
			return (Equals(x, y, _valueComparer, _nullIsEmpty));
		}

		/// <summary>
		/// Static (instance-less) equality check
		/// </summary>
		public static bool Equals(TDict x, TDict y, IEqualityComparer<TValue> valueComparer, bool nullIsEmpty = true)
		{
			if (valueComparer == null)
			{
				throw new ArgumentNullException(nameof(valueComparer));
			}

			if (nullIsEmpty)
			{
				if (x == null)
				{
					return y == null || y.Count == 0;
				}

				if (y == null)
				{
					return x.Count == 0;
				}
			}
			else
			{
				if (x == null)
				{
					return y == null;
				}

				if (y == null)
				{
					return false;
				}
			}

			if (x.Count != y.Count)
			{
				return false;
			}

			foreach (var keyValue in x)
			{
				if (!y.TryGetValue(keyValue.Key, out var yValue))
				{
					return false; // key not found: dictionaries are not equal.
				}

				if (!valueComparer.Equals(keyValue.Value, yValue))
				{
					return false; // value for this key is different.
				}
			}

			return true; // no differences found
		}

		/// <inheritdoc/>
		public int GetHashCode(TDict obj)
		{
			return obj?.Count ?? 0;
		}
	}
}
