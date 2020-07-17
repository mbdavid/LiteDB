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
	/// Sector-based stream
	/// </summary>
	public class SectorStream : Stream
	{
		private readonly Stream _baseStream;
		private readonly long _offset;
		private ulong _currentSector;
		
		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="baseStream">The base stream to read/write from</param>
		/// <param name="sectorSize">The size of the sectors to read/write</param>
		public SectorStream(Stream baseStream, int sectorSize)
			: this(baseStream, sectorSize, 0)
		{
		}

		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="baseStream">The base stream to read/write from</param>
		/// <param name="sectorSize">The size of the sectors to read/write</param>
		/// <param name="offset">Offset to start counting sectors</param>
		public SectorStream(Stream baseStream, int sectorSize, long offset)
		{
			SectorSize = sectorSize;
			_baseStream = baseStream;
			_offset = offset;
		}

		/// <summary>
		/// The size of the sectors
		/// </summary>
		public int SectorSize { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the current stream supports reading.
		/// </summary>
		/// <returns>true if the stream supports reading; otherwise, false.</returns>
		public override bool CanRead
		{
			get { return _baseStream.CanRead; }
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <returns>true if the stream supports seeking; otherwise, false.</returns>
		public override bool CanSeek
		{
			get { return _baseStream.CanSeek; }
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports writing.
		/// </summary>
		/// <returns>true if the stream supports writing; otherwise, false.</returns>
		public override bool CanWrite
		{
			get { return _baseStream.CanWrite; }
		}

		/// <summary>
		/// Gets the length in bytes of the stream.
		/// </summary>
		/// <returns>A long value representing the length of the stream in bytes.</returns>
		public override long Length
		{
			get { return _baseStream.Length - _offset; }
		}

		/// <summary>
		/// Gets or sets the position within the current stream.
		/// </summary>
		/// <returns>The current position within the stream.</returns>
		public override long Position
		{
			get { return _baseStream.Position - _offset; }
			set
			{
				ValidateSizeMultiple(value);

				//base stream gets the non-tweaked value
				_baseStream.Position = value + _offset;
				_currentSector = (ulong)(value / SectorSize);
			}
		}

		/// <summary>
		/// The current sector this stream is at
		/// </summary>
		protected ulong CurrentSector
		{
			get { return _currentSector; }
		}

		/// <summary>
		/// Validates that the size is a multiple of the sector size
		/// </summary>
		private void ValidateSizeMultiple(long value)
		{
			if (value%SectorSize != 0)
				throw new ArgumentException(string.Format("Value needs to be a multiple of {0}", SectorSize));
		}

		/// <summary>
		/// Validates that the size is equal to the sector size
		/// </summary>
		protected void ValidateSize(long value)
		{
			if (value != SectorSize)
				throw new ArgumentException(string.Format("Value needs to be {0}", SectorSize));
		}

		/// <summary>
		/// Validates that the size is equal to the sector size
		/// </summary>
		protected void ValidateSize(int value)
		{
			if (value != SectorSize)
				throw new ArgumentException(string.Format("Value needs to be {0}", SectorSize));
		}

		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		public override void Flush()
		{
			_baseStream.Flush();
		}

		/// <summary>
		/// Sets the position within the current stream.
		/// </summary>
		/// <returns>
		/// The new position within the current stream.
		/// </returns>
		/// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
		/// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
		public override long Seek(long offset, SeekOrigin origin)
		{
			long newPosition;
			switch (origin)
			{
				case SeekOrigin.Begin:
					newPosition = offset;
					break;
				case SeekOrigin.End:
					newPosition = Length - offset;
					break;
				default:
					newPosition = Position + offset;
					break;
			}

			Position = newPosition;

			return newPosition;
		}

		/// <summary>
		/// Sets the length of the current stream.
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes.</param>
		public override void SetLength(long value)
		{
			ValidateSizeMultiple(value);

			_baseStream.SetLength(value);
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

			var ret = _baseStream.Read(buffer, offset, count);
			_currentSector++;
			return ret;
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

			_baseStream.Write(buffer, offset, count);
			_currentSector++;
		}
	}
}