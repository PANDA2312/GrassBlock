namespace GrassBlock
{
    namespace Type
    {
        public static class VarNum
        {
            private const long SEGMENT_BITS = 0b_01111111;
            private const long CONTINUE_BIT = 0b_10000000;
            public static int ReadVarInt(byte[] buffer, ref int index, out int len)
            {
				int startIndex = index;
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                byte cur;
                int res = 0;
                int position = 0;
                while (true)
                {
                    cur = buffer[index];
                    res |= (cur & (int)SEGMENT_BITS) << position;
                    index++;
                    if ((cur & CONTINUE_BIT) == 0) break;
                    position += 7;
                    if (position >= 32) throw new InvalidDataException("VarInt is too big!");
					
                }
				len = index - startIndex;
                return res;
            }
            public static long ReadVarLong(byte[] buffer, ref int index)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                byte cur;
                long res = 0;
                int position = 0;
                while (true)
                {
                    cur = buffer[index];
                    res |= (cur & SEGMENT_BITS) << position;
                    if ((cur & CONTINUE_BIT) == 0) break;
                    position += 7;
                    index++;
                    if (position >= 64) throw new InvalidDataException("VarInt is too big!");
                }
                index++;
                return res;
            }
            public static byte[] GetVarNum(long val)
            {
				if(val == 0) return new byte[1];
                long tmp = val;
                int len = 0;
                while (tmp > 0)
                {
                    len++;
                    tmp >>= 7;
                }
                byte[] res = new byte[len];
                int cur = 0;
                while (val > 0)
                {
                    if (cur == len - 1) res[cur] |= (byte)(val & SEGMENT_BITS);
                    else res[cur] |= (byte)(val & SEGMENT_BITS | CONTINUE_BIT);
                    val >>= 7;
                    cur++;
                }
                return res;
            }
        }
    }
}