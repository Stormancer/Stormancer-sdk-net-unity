﻿#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2012 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;


namespace MsgPack
{
#if !WINDOWS_PHONE
	/// <summary>
	///		Implements basic (maybe naive) implementation for common Set&lt;T&gt; operation.
	/// </summary>
	internal static class SetOperation
	{
		public static bool IsProperSubsetOf<T>( ICollection<T> set, IEnumerable<T> other )
		{
			#region CONTRACT
			Contract.Assert( set != null );

			if ( other == null )
			{
				throw new ArgumentNullException( "other" );
			}
			#endregion CONTRACT

			var asCollection = other as ICollection<T>;
			if ( asCollection != null )
			{
				if ( set.Count == 0 )
				{
					return 0 < asCollection.Count;
				}

				if ( asCollection.Count <= set.Count )
				{
					return false;
				}
			}

			int otherCount;
			if ( !IsSubsetOfCore( set, other, out otherCount ) )
			{
				return false;
			}

			return set.Count < otherCount;
		}


		public static bool IsSubsetOf<T>( ICollection<T> set, IEnumerable<T> other )
		{
			#region CONTRACT
			Contract.Assert( set != null );

			if ( other == null )
			{
				throw new ArgumentNullException( "other" );
			}
			#endregion CONTRACT

			if ( set.Count == 0 )
			{
				return true;
			}

			var asCollection = other as ICollection<T>;
			if ( asCollection != null && asCollection.Count < set.Count )
			{
				return false;
			}

			int checkedCount;
			return IsSubsetOfCore( set, other, out checkedCount );
		}


		private static bool IsSubsetOfCore<T>( ICollection<T> set, IEnumerable<T> other, out int otherCount )
		{
			otherCount = 0;

			// Other must be set to handle duplicated items.
			// e.x., [1,2,3] is proper subset of [1,2,3,4,1] but not [1,1,1,1,1]
			var asSet = other as HashSet<T>;
			if ( asSet == null )
			{
				asSet = new HashSet<T>( other );
			}

			int matchCount = 0;

			foreach ( var item in asSet )
			{
				otherCount++;

				if ( set.Contains( item ) )
				{
					matchCount++;
				}
			}

			// At least, other contains all items in set, but might be equal.
			return set.Count <= matchCount;
		}


		public static bool IsProperSupersetOf<T>( ICollection<T> set, IEnumerable<T> other )
		{
			#region CONTRACT
			Contract.Assert( set != null );

			if ( other == null )
			{
				throw new ArgumentNullException( "other" );
			}
			#endregion CONTRACT

			var asCollection = other as ICollection<T>;
			if ( asCollection != null )
			{
				if ( asCollection.Count == 0 )
				{
					return 0 < set.Count;
				}
			}

			int checkedCount;
			if ( !IsSupersetOfCore( set, other, out checkedCount ) )
			{
				return false;
			}

			return checkedCount < set.Count;
		}


		public static bool IsSupersetOf<T>( ICollection<T> set, IEnumerable<T> other )
		{
			#region CONTRACT
			Contract.Assert( set != null );

			if ( other == null )
			{
				throw new ArgumentNullException( "other" );
			}
			#endregion CONTRACT

			var asCollection = other as ICollection<T>;
			if ( asCollection != null && asCollection.Count < set.Count )
			{
				if ( asCollection.Count == 0 )
				{
					return true;
				}

				if ( set.Count <= asCollection.Count )
				{
					return false;
				}
			}

			int checkedCount;
			return IsSupersetOfCore( set, other, out checkedCount );
		}

		private static bool IsSupersetOfCore<T>( ICollection<T> set, IEnumerable<T> other, out int otherCount )
		{
			otherCount = 0;

			// Other must be set to handle duplicated items.
			// e.x., [1,2,3] is proper superset of [1,2] and [1,2,1]
			var asSet = other as HashSet<T>;
			if ( asSet == null )
			{
				asSet = new HashSet<T>( other );
			}

			foreach ( var item in asSet )
			{
				otherCount++;

				if ( !set.Contains( item ) )
				{
					return false;
				}
			}

			// At least, set contains all items in other, but might be equal.
			return true;
		}

		public static bool Overlaps<T>( ICollection<T> set, IEnumerable<T> other )
		{
			#region CONTRACT
			Contract.Assert( set != null );

			if ( other == null )
			{
				throw new ArgumentNullException( "other" );
			}
			#endregion CONTRACT

			if ( set.Count == 0 )
			{
				return false;
			}

			return other.Any( item => set.Contains( item ) );
		}


		public static bool SetEquals<T>( ICollection<T> set, IEnumerable<T> other )
		{
			#region CONTRACT
			Contract.Assert( set != null );

			if ( other == null )
			{
				throw new ArgumentNullException( "other" );
			}
			#endregion CONTRACT

			if ( set.Count == 0 )
			{
				var asCollection = other as ICollection<T>;
				if ( asCollection != null )
				{
					return asCollection.Count == 0;
				}
			}

			// Cannot use other.All() here because it always returns true for empty source.
			var asSet = other as HashSet<T> ?? new HashSet<T>( other );
			int matchCount = 0;
			foreach ( var item in asSet )
			{
				if ( !set.Contains( item ) )
				{
					return false;
				}
				else
				{
					matchCount++;
				}
			}

			return matchCount == set.Count;
		}
	}
#endif
}
