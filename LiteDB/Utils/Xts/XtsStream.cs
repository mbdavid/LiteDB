// Copyright (c) 2010 Gareth Lennox (garethl@dwakn.com)
// All rights reserved.

// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:

//     * Redistributions of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//       this list of conditions and the following disclaimer in the documentation
//       and/or other materials provided with the distribution.
//     * Neither the name of Gareth Lennox nor the names of its
//       contributors may be used to endorse or promote products derived from this
//       software without specific prior written permission.

// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System.IO;

namespace XTSSharp
{
	/// <summary>
	/// A random access, xts encrypted stream
	/// </summary>
	public class XtsStream : RandomAccessSectorStream
	{
		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="baseStream">The base stream</param>
		/// <param name="xts">Xts implementation to use</param>
		public XtsStream(Stream baseStream, Xts xts)
			: this(baseStream, xts, XtsSectorStream.DEFAULT_SECTOR_SIZE)
		{
		}

		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="baseStream">The base stream</param>
		/// <param name="xts">Xts implementation to use</param>
		/// <param name="sectorSize">Sector size</param>
		public XtsStream(Stream baseStream, Xts xts, int sectorSize)
			: base(new XtsSectorStream(baseStream, xts, sectorSize), true)
		{
		}


		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="baseStream">The base stream</param>
		/// <param name="xts">Xts implementation to use</param>
		/// <param name="sectorSize">Sector size</param>
		/// <param name="offset">Offset to start counting sectors</param>
		public XtsStream(Stream baseStream, Xts xts, int sectorSize, long offset)
			: base(new XtsSectorStream(baseStream, xts, sectorSize, offset), true)
		{
		}
	}
}