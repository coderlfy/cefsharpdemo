using System;
namespace Microsoft.VisualStudio.WebHost
{
	internal sealed class ByteParser
	{
		private byte[] _bytes;
		private int _pos;
		internal int CurrentOffset
		{
			get
			{
				return this._pos;
			}
		}
		internal ByteParser(byte[] bytes)
		{
			this._bytes = bytes;
			this._pos = 0;
		}
		internal ByteString ReadLine()
		{
			ByteString result = null;
			for (int i = this._pos; i < this._bytes.Length; i++)
			{
				if (this._bytes[i] == 10)
				{
					int num = i - this._pos;
					if (num > 0 && this._bytes[i - 1] == 13)
					{
						num--;
					}
					result = new ByteString(this._bytes, this._pos, num);
					this._pos = i + 1;
					return result;
				}
			}
			if (this._pos < this._bytes.Length)
			{
				result = new ByteString(this._bytes, this._pos, this._bytes.Length - this._pos);
			}
			this._pos = this._bytes.Length;
			return result;
		}
	}
}
