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

using System;
using System.IO;

namespace XTSSharp
{
	/// <summary>
	/// Xts sector-based
	/// </summary>
	public class XtsSectorStream : SectorStream
	{
		/// <summary>
		/// The default sector size
		/// </summary>
		public const int DEFAULT_SECTOR_SIZE = 512;

		private readonly byte[] _tempBuffer;
		private readonly Xts _xts;
		private XtsCryptoTransform _decryptor;
		private XtsCryptoTransform _encryptor;

		/// <summary>
		/// Creates a new stream with the default sector size
		/// </summary>
		/// <param name="baseStream">The base stream</param>
		/// <param name="xts">The xts transform</param>
		public XtsSectorStream(Stream baseStream, Xts xts)
			: this(baseStream, xts, DEFAULT_SECTOR_SIZE)
		{
		}

		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="baseStream">The base stream</param>
		/// <param name="xts">The xts transform</param>
		/// <param name="sectorSize">Sector size</param>
		public XtsSectorStream(Stream baseStream, Xts xts, int sectorSize)
			: this(baseStream, xts, sectorSize, 0)
		{
		}

		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="baseStream">The base stream</param>
		/// <param name="xts">The xts transform</param>
		/// <param name="sectorSize">Sector size</param>
		/// <param name="offset">Offset to start counting sectors</param>
		public XtsSectorStream(Stream baseStream, Xts xts, int sectorSize, long offset)
			: base(baseStream, sectorSize, offset)
		{
			_xts = xts;
			_tempBuffer = new byte[sectorSize];
		}

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream"/> and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_encryptor != null)
				_encryptor.Dispose();

			if (_decryptor != null)
				_decryptor.Dispose();
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			ValidateSize(count);

			if (count == 0)
				return;

			//get the current sector
			var currentSector = CurrentSector;

			if (_encryptor == null)
				_encryptor = _xts.CreateEncryptor();

			//encrypt the sector
			int transformedCount = _encryptor.TransformBlock(buffer, offset, count, _tempBuffer, 0, currentSector);

			//Console.WriteLine("Encrypting sector {0}", currentSector);

			//write it to the base stream
			base.Write(_tempBuffer, 0, transformedCount);
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source. </param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		public override int Read(byte[] buffer, int offset, int count)
		{
			ValidateSize(count);

			//get the current sector
			var currentSector = CurrentSector;

			//read the sector from the base stream
			var ret = base.Read(_tempBuffer, 0, count);

			if (ret == 0)
				return 0;

			if (_decryptor == null)
				_decryptor = _xts.CreateDecryptor();

			//decrypt the sector
			var retV = _decryptor.TransformBlock(_tempBuffer, 0, ret, buffer, offset, currentSector);

			//Console.WriteLine("Decrypting sector {0} == {1} bytes", currentSector, retV);

			return retV;
		}
	}
}