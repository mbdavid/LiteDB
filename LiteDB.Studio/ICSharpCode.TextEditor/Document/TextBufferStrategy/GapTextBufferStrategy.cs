// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Text;

namespace ICSharpCode.TextEditor.Document
{
	public class GapTextBufferStrategy : ITextBufferStrategy
	{
		#if DEBUG
		int creatorThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
		
		void CheckThread()
		{
			if (System.Threading.Thread.CurrentThread.ManagedThreadId != creatorThread)
				throw new InvalidOperationException("GapTextBufferStategy is not thread-safe!");
		}
		#endif
		
		char[] buffer = new char[0];
		string cachedContent;
		
		int gapBeginOffset = 0;
		int gapEndOffset = 0;
		int gapLength = 0; // gapLength == gapEndOffset - gapBeginOffset
		
		const int minGapLength = 128;
		const int maxGapLength = 2048;
		
		public int Length {
			get {
				return buffer.Length - gapLength;
			}
		}
		
		public void SetContent(string text)
		{
			if (text == null) {
				text = String.Empty;
			}
			cachedContent = text;
			buffer = text.ToCharArray();
			gapBeginOffset = gapEndOffset = gapLength = 0;
		}
		
		public char GetCharAt(int offset)
		{
			#if DEBUG
			CheckThread();
			#endif
			
			if (offset < 0 || offset >= Length) {
				throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset < " + Length.ToString());
			}
			
			return offset < gapBeginOffset ? buffer[offset] : buffer[offset + gapLength];
		}
		
		public string GetText(int offset, int length)
		{
			#if DEBUG
			CheckThread();
			#endif
			
			if (offset < 0 || offset > Length) {
				throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + Length.ToString());
			}
			if (length < 0 || offset + length > Length) {
				throw new ArgumentOutOfRangeException("length", length, "0 <= length, offset(" + offset + ")+length <= " + Length.ToString());
			}
			if (offset == 0 && length == Length) {
				if (cachedContent != null)
					return cachedContent;
				else
					return cachedContent = GetTextInternal(offset, length);
			} else {
				return GetTextInternal(offset, length);
			}
		}
		
		string GetTextInternal(int offset, int length)
		{
			int end = offset + length;
			
			if (end < gapBeginOffset) {
				return new string(buffer, offset, length);
			}
			
			if (offset > gapBeginOffset) {
				return new string(buffer, offset + gapLength, length);
			}
			
			int block1Size = gapBeginOffset - offset;
			int block2Size = end - gapBeginOffset;
			
			StringBuilder buf = new StringBuilder(block1Size + block2Size);
			buf.Append(buffer, offset,       block1Size);
			buf.Append(buffer, gapEndOffset, block2Size);
			return buf.ToString();
		}
		
		public void Insert(int offset, string text)
		{
			Replace(offset, 0, text);
		}
		
		public void Remove(int offset, int length)
		{
			Replace(offset, length, String.Empty);
		}
		
		public void Replace(int offset, int length, string text)
		{
			if (text == null) {
				text = String.Empty;
			}
			
			#if DEBUG
			CheckThread();
			#endif
			
			if (offset < 0 || offset > Length) {
				throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + Length.ToString());
			}
			if (length < 0 || offset + length > Length) {
				throw new ArgumentOutOfRangeException("length", length, "0 <= length, offset+length <= " + Length.ToString());
			}
			
			cachedContent = null;
			
			// Math.Max is used so that if we need to resize the array
			// the new array has enough space for all old chars
			PlaceGap(offset, text.Length - length);
			gapEndOffset += length; // delete removed text
			text.CopyTo(0, buffer, gapBeginOffset, text.Length);
			gapBeginOffset += text.Length;
			gapLength = gapEndOffset - gapBeginOffset;
			if (gapLength > maxGapLength) {
				MakeNewBuffer(gapBeginOffset, minGapLength);
			}
		}
		
		void PlaceGap(int newGapOffset, int minRequiredGapLength)
		{
			if (gapLength < minRequiredGapLength) {
				// enlarge gap
				MakeNewBuffer(newGapOffset, minRequiredGapLength);
			} else {
				while (newGapOffset < gapBeginOffset) {
					buffer[--gapEndOffset] = buffer[--gapBeginOffset];
				}
				while (newGapOffset > gapBeginOffset) {
					buffer[gapBeginOffset++] = buffer[gapEndOffset++];
				}
			}
		}
		
		void MakeNewBuffer(int newGapOffset, int newGapLength)
		{
			if (newGapLength < minGapLength) newGapLength = minGapLength;
			
			char[] newBuffer = new char[Length + newGapLength];
			if (newGapOffset < gapBeginOffset) {
				// gap is moving backwards
				
				// first part:
				Array.Copy(buffer, 0, newBuffer, 0, newGapOffset);
				// moving middle part:
				Array.Copy(buffer, newGapOffset, newBuffer, newGapOffset + newGapLength, gapBeginOffset - newGapOffset);
				// last part:
				Array.Copy(buffer, gapEndOffset, newBuffer, newBuffer.Length - (buffer.Length - gapEndOffset), buffer.Length - gapEndOffset);
			} else {
				// gap is moving forwards
				// first part:
				Array.Copy(buffer, 0, newBuffer, 0, gapBeginOffset);
				// moving middle part:
				Array.Copy(buffer, gapEndOffset, newBuffer, gapBeginOffset, newGapOffset - gapBeginOffset);
				// last part:
				int lastPartLength = newBuffer.Length - (newGapOffset + newGapLength);
				Array.Copy(buffer, buffer.Length - lastPartLength, newBuffer, newGapOffset + newGapLength, lastPartLength);
			}
			
			gapBeginOffset = newGapOffset;
			gapEndOffset = newGapOffset + newGapLength;
			gapLength = newGapLength;
			buffer = newBuffer;
		}
	}
}
