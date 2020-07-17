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
using System.Security.Cryptography;

namespace XTSSharp
{
	/// <summary>
	/// The actual Xts cryptography transform
	/// </summary>
	/// <remarks>
	/// The reason that it doesn't implement ICryptoTransform, as the interface is different.
	/// 
	/// Most of the logic was taken from the LibTomCrypt project - http://libtom.org and 
	/// converted to C#
	/// </remarks>
	public class XtsCryptoTransform : IDisposable
	{
		private readonly byte[] _cc = new byte[16];
		private readonly bool _decrypting;
		private readonly ICryptoTransform _key1;
		private readonly ICryptoTransform _key2;

		private readonly byte[] _pp = new byte[16];
		private readonly byte[] _t = new byte[16];
		private readonly byte[] _tweak = new byte[16];

		/// <summary>
		/// Creates a new transform
		/// </summary>
		/// <param name="key1">Transform 1</param>
		/// <param name="key2">Transform 2</param>
		/// <param name="decrypting">Is this a decryption transform?</param>
		public XtsCryptoTransform(ICryptoTransform key1, ICryptoTransform key2, bool decrypting)
		{
			if (key1 == null)
				throw new ArgumentNullException("key1");

			if (key2 == null)
				throw new ArgumentNullException("key2");

			_key1 = key1;
			_key2 = key2;
			_decrypting = decrypting;
		}

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
			_key1.Dispose();
			_key2.Dispose();
		}

		#endregion

		/// <summary>
		/// Transforms a single block.
		/// </summary>
		/// <param name="inputBuffer"> The input for which to compute the transform.</param>
		/// <param name="inputOffset">The offset into the input byte array from which to begin using data.</param>
		/// <param name="inputCount">The number of bytes in the input byte array to use as data.</param>
		/// <param name="outputBuffer">The output to which to write the transform.</param>
		/// <param name="outputOffset">The offset into the output byte array from which to begin writing data.</param>
		/// <param name="sector">The sector number of the block</param>
		/// <returns>The number of bytes written.</returns>
		public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset, ulong sector)
		{
			FillArrayFromSector(_tweak, sector);

			int lim;

			/* get number of blocks */
			var m = inputCount >> 4;
			var mo = inputCount & 15;

			/* encrypt the tweak */
			_key2.TransformBlock(_tweak, 0, _tweak.Length, _t, 0);

			/* for i = 0 to m-2 do */
			if (mo == 0)
				lim = m;
			else
				lim = m - 1;

			for (var i = 0; i < lim; i++)
			{
				TweakCrypt(inputBuffer, inputOffset, outputBuffer, outputOffset, _t);
				inputOffset += 16;
				outputOffset += 16;
			}

			/* if ptlen not divide 16 then */
			if (mo > 0)
			{
				if (_decrypting)
				{
					Buffer.BlockCopy(_t, 0, _cc, 0, 16);
					MultiplyByX(_cc);

					/* CC = tweak encrypt block m-1 */
					TweakCrypt(inputBuffer, inputOffset, _pp, 0, _cc);

					/* Cm = first ptlen % 16 bytes of CC */
					int i;
					for (i = 0; i < mo; i++)
					{
						_cc[i] = inputBuffer[16 + i + inputOffset];
						outputBuffer[16 + i + outputOffset] = _pp[i];
					}

					for (; i < 16; i++)
					{
						_cc[i] = _pp[i];
					}

					/* Cm-1 = Tweak encrypt PP */
					TweakCrypt(_cc, 0, outputBuffer, outputOffset, _t);
				}
				else
				{
					/* CC = tweak encrypt block m-1 */
					TweakCrypt(inputBuffer, inputOffset, _cc, 0, _t);

					/* Cm = first ptlen % 16 bytes of CC */
					int i;
					for (i = 0; i < mo; i++)
					{
						_pp[i] = inputBuffer[16 + i + inputOffset];
						outputBuffer[16 + i + outputOffset] = _cc[i];
					}

					for (; i < 16; i++)
					{
						_pp[i] = _cc[i];
					}

					/* Cm-1 = Tweak encrypt PP */
					TweakCrypt(_pp, 0, outputBuffer, outputOffset, _t);
				}
			}

			return inputCount;
		}

		/// <summary>
		/// Fills a byte array from a sector number
		/// </summary>
		/// <param name="value">The destination</param>
		/// <param name="sector">The sector number</param>
		private static void FillArrayFromSector(byte[] value, ulong sector)
		{
			value[7] = (byte) ((sector >> 56) & 255);
			value[6] = (byte) ((sector >> 48) & 255);
			value[5] = (byte) ((sector >> 40) & 255);
			value[4] = (byte) ((sector >> 32) & 255);
			value[3] = (byte) ((sector >> 24) & 255);
			value[2] = (byte) ((sector >> 16) & 255);
			value[1] = (byte) ((sector >> 8) & 255);
			value[0] = (byte) (sector & 255);
		}

		/// <summary>
		/// Performs the Xts TweakCrypt operation
		/// </summary>
		private void TweakCrypt(byte[] inputBuffer, int inputOffset, byte[] outputBuffer, int outputOffset, byte[] t)
		{
			for (var x = 0; x < 16; x++)
			{
				outputBuffer[x + outputOffset] = (byte) (inputBuffer[x + inputOffset] ^ t[x]);
			}

			_key1.TransformBlock(outputBuffer, outputOffset, 16, outputBuffer, outputOffset);

			for (var x = 0; x < 16; x++)
			{
				outputBuffer[x + outputOffset] = (byte) (outputBuffer[x + outputOffset] ^ t[x]);
			}

			MultiplyByX(t);
		}

		/// <summary>
		/// Multiply by x
		/// </summary>
		/// <param name="i">The value to multiply by x (LFSR shift)</param>
		private static void MultiplyByX(byte[] i)
		{
			byte t = 0, tt = 0;

			for (var x = 0; x < 16; x++)
			{
				tt = (byte) (i[x] >> 7);
				i[x] = (byte) (((i[x] << 1) | t) & 0xFF);
				t = tt;
			}

			if (tt > 0)
				i[0] ^= 0x87;
		}
	}
}