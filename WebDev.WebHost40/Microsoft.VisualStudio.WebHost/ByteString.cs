using System;
using System.Collections;
using System.Text;
namespace Microsoft.VisualStudio.WebHost
{
	internal sealed class ByteString
	{
		private byte[] _bytes;
		private int _offset;
		private int _length;
		public byte[] Bytes
		{
			get
			{
				return this._bytes;
			}
		}
		public bool IsEmpty
		{
			get
			{
				return this._bytes == null || this._length == 0;
			}
		}
		public int Length
		{
			get
			{
				return this._length;
			}
		}
		public int Offset
		{
			get
			{
				return this._offset;
			}
		}
		public byte this[int index]
		{
			get
			{
				return this._bytes[this._offset + index];
			}
		}
		public ByteString(byte[] bytes, int offset, int length)
		{
			this._bytes = bytes;
			if (this._bytes != null && offset >= 0 && length >= 0 && offset + length <= this._bytes.Length)
			{
				this._offset = offset;
				this._length = length;
			}
		}
		public string GetString()
		{
			return this.GetString(Encoding.UTF8);
		}
		public string GetString(Encoding enc)
		{
			if (this.IsEmpty)
			{
				return string.Empty;
			}
			return enc.GetString(this._bytes, this._offset, this._length);
		}
		public byte[] GetBytes()
		{
			byte[] array = new byte[this._length];
			if (this._length > 0)
			{
				Buffer.BlockCopy(this._bytes, this._offset, array, 0, this._length);
			}
			return array;
		}
		public int IndexOf(char ch)
		{
			return this.IndexOf(ch, 0);
		}
		public int IndexOf(char ch, int offset)
		{
			for (int i = offset; i < this._length; i++)
			{
				if (this[i] == (byte)ch)
				{
					return i;
				}
			}
			return -1;
		}
		public ByteString Substring(int offset)
		{
			return this.Substring(offset, this._length - offset);
		}
		public ByteString Substring(int offset, int len)
		{
			return new ByteString(this._bytes, this._offset + offset, len);
		}
		public ByteString[] Split(char sep)
		{
			ArrayList arrayList = new ArrayList();
			int i = 0;
			while (i < this._length)
			{
				int num = this.IndexOf(sep, i);
				if (num < 0)
				{
					arrayList.Add(this.Substring(i));
					break;
				}
				arrayList.Add(this.Substring(i, num - i));
				i = num + 1;
				while (i < this._length && this[i] == (byte)sep)
				{
					i++;
				}
			}
			int count = arrayList.Count;
			ByteString[] array = new ByteString[count];
			for (int j = 0; j < count; j++)
			{
				array[j] = (ByteString)arrayList[j];
			}
			return array;
		}
	}
}
