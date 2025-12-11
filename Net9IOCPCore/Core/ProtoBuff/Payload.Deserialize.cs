using System;
using System.Collections.Generic;
using System.Text;

namespace Net9IOCPCore.Core.ProtoBuff;

public partial class Payload
{
    public static List<object> Deserialize(byte[] data)
    {
        var result = new List<object>();
        int pos = 0;
        while (pos < data.Length)
        {
            if (pos >= data.Length)
                throw new InvalidOperationException("Unexpected end of buffer (type).");

            PayloadType type = (PayloadType)data[pos++];

            switch (type)
            {
                case PayloadType.Byte:
                    if (pos + 1 > data.Length)
                        throw new InvalidOperationException("Buffer too short for Byte.");
                    result.Add(data[pos++]);
                    break;
                case PayloadType.Bool:
                    if (pos + 1 > data.Length)
                        throw new InvalidOperationException("Buffer too short for Bool.");
                    result.Add(data[pos++] != 0);
                    break;
                case PayloadType.Char:
                    if (pos + 2 > data.Length)
                        throw new InvalidOperationException("Buffer too short for Char.");
                    result.Add(BitConverter.ToChar(data, pos));
                    pos += 2;
                    break;
                case PayloadType.Short:
                    if (pos + 2 > data.Length)
                        throw new InvalidOperationException("Buffer too short for Short.");
                    result.Add(BitConverter.ToInt16(data, pos));
                    pos += 2;
                    break;
                case PayloadType.Int:
                    if (pos + 4 > data.Length)
                        throw new InvalidOperationException("Buffer too short for Int.");
                    result.Add(BitConverter.ToInt32(data, pos));
                    pos += 4;
                    break;
                case PayloadType.Long:
                    if (pos + 8 > data.Length)
                        throw new InvalidOperationException("Buffer too short for Long.");
                    result.Add(BitConverter.ToInt64(data, pos));
                    pos += 8;
                    break;
                case PayloadType.Float:
                    if (pos + 4 > data.Length)
                        throw new InvalidOperationException("Buffer too short for Float.");
                    result.Add(BitConverter.ToSingle(data, pos));
                    pos += 4;
                    break;
                case PayloadType.Double:
                    if (pos + 8 > data.Length)
                        throw new InvalidOperationException("Buffer too short for Double.");
                    result.Add(BitConverter.ToDouble(data, pos));
                    pos += 8;
                    break;
                case PayloadType.String:
                    if (pos + 2 > data.Length)
                        throw new InvalidOperationException("Buffer too short for String length.");
                    short len = BitConverter.ToInt16(data, pos);
                    pos += 2;
                    if (pos + len > data.Length)
                        throw new InvalidOperationException("Buffer too short for String data.");
                    string str = Encoding.UTF8.GetString(data, pos, len);
                    pos += len;
                    result.Add(str);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown payload type: {type}");
            }
        }
        return result;
    }
}