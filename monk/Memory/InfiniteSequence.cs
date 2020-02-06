﻿/**
 *  Monk
 *  Copyright (C) Timothy Baxendale
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
**/
using System.Collections;
using System.Collections.Generic;

namespace Monk.Memory
{
    /// <summary>
    /// Represents a sequence that theoretically has no limit
    /// </summary>
    public abstract class InfiniteSequence<T> : IEnumerable<T>
    {
        public virtual int Length { get; } = int.MaxValue;
        public virtual int Position { get; set; }

        public abstract T Peek();
        public abstract T Next();

        public virtual int Read(T[] buffer, int offset, int count)
        {
            for(; offset < count; ++offset)
                buffer[offset] = Next();
            return count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            while(true) yield return Next();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
