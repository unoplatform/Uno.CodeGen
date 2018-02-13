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
using System.Linq;

namespace Uno.Equality
{
	/// <summary>
	/// <see cref="IEqualityComparer{T}"/> implementation for evaluating sorted collections
	/// </summary>
	public class SortedReadonlyCollectionEqualityComparer<TCollection, T> : IEqualityComparer<TCollection>
		where TCollection : IReadOnlyCollection<T>
	{
		private readonly IEqualityComparer<T> _itemComparer;
		private readonly bool _nullIsEmpty;

		public static IEqualityComparer<TCollection> Default { get; } = new SortedReadonlyCollectionEqualityComparer<TCollection, T>();

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="itemComparer">Comparer to use to comparer an enumerated item</param>
		/// <param name="nullIsEmpty">If null value should be compated as empty collection</param>
		public SortedReadonlyCollectionEqualityComparer(IEqualityComparer<T> itemComparer = null, bool nullIsEmpty = true)
		{
			_itemComparer = itemComparer ?? EqualityComparer<T>.Default;
			_nullIsEmpty = nullIsEmpty;
		}

		/// <inheritdoc/>
		public bool Equals(TCollection x, TCollection y)
		{
			return (Equals(x, y, _itemComparer, _nullIsEmpty));
		}

		/// <summary>
		/// Static (instance-less) equality check
		/// </summary>
		public static bool Equals(TCollection x, TCollection y, IEqualityComparer<T> itemComparer, bool nullIsEmpty = true)
		{
			if (itemComparer == null)
			{
				throw new ArgumentNullException(nameof(itemComparer));
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

			var sequenceEqual = x.SequenceEqual(y, itemComparer);
			return sequenceEqual;
		}

		/// <inheritdoc/>
		public int GetHashCode(TCollection obj)
		{
			return obj?.Count ?? 0;
		}
	}
}