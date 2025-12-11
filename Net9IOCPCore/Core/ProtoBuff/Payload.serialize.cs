using System;
using System.Text;

namespace Net9IOCPCore.Core.ProtoBuff;

public partial class Payload
{
    public void AddByte(byte value)
    {
        lock (_lock)
        {
            int required = 1 + 1;
            if (IsBufferFull(required))
                throw new BufferFullException("Buffer is full, cannot add more byte.");
            _buffer.Add((byte)PayloadType.Byte);
            _buffer.Add(value);
        }
    }

    public void AddBool(bool value)
    {
        lock (_lock)
        {
            int required = 1 + 1;
            if (IsBufferFull(required))
                throw new BufferFullException("Buffer is full, cannot add more bool.");
            _buffer.Add((byte)PayloadType.Bool);
            _buffer.Add((byte)(value ? 1 : 0));
        }
    }

    public void AddChar(char value)
    {
        lock (_lock)
        {
            int required = 1 + 2;
            if (IsBufferFull(required))
                throw new BufferFullException("Buffer is full, cannot add more char.");
            _buffer.Add((byte)PayloadType.Char);
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
    }

    public void AddShort(short value)
    {
        lock (_lock)
        {
            int required = 1 + 2;
            if (IsBufferFull(required))
                throw new BufferFullException("Buffer is full, cannot add more short.");
            _buffer.Add((byte)PayloadType.Short);
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
    }

    public void AddInt(int value)
    {
        lock (_lock)
        {
            int required = 1 + 4;
            if (IsBufferFull(required))
                throw new BufferFullException("Buffer is full, cannot add more int.");
            _buffer.Add((byte)PayloadType.Int);
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
    }

    public void AddLong(long value)
    {
        lock (_lock)
        {
            int required = 1 + 8;
            if (IsBufferFull(required))
                throw new BufferFullException("Buffer is full, cannot add more long.");
            _buffer.Add((byte)PayloadType.Long);
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
    }

    public void AddFloat(float value)
    {
        lock (_lock)
        {
            int required = 1 + 4;
            if (IsBufferFull(required))
                throw new BufferFullException("Buffer is full, cannot add more float.");
            _buffer.Add((byte)PayloadType.Float);
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
    }

    public void AddDouble(double value)
    {
        lock (_lock)
        {
            int required = 1 + 8;
            if (IsBufferFull(required))
                throw new BufferFullException("Buffer is full, cannot add more double.");
            _buffer.Add((byte)PayloadType.Double);
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
    }

    public void AddString(string value)
    {
        lock (_lock)
        {
            byte[] strBytes = Encoding.UTF8.GetBytes(value);
            int required = 1 + 2 + strBytes.Length; // type + length(short) + data
            if (IsBufferFull(required))
                throw new BufferFullException("Buffer is full, cannot add more string.");
            _buffer.Add((byte)PayloadType.String);
            _buffer.AddRange(BitConverter.GetBytes((short)strBytes.Length));
            _buffer.AddRange(strBytes);
        }
    }
}
