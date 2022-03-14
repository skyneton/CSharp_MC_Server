﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NiaBukkit.API.Util;

namespace NiaBukkit.Network
{
    public class ByteBuf
    {
        private byte[] _readBuf;
        private readonly List<byte> _buf = new ();

        private int _pos = 0;

        public bool Available => _buf.Count > 0;
        public int Length => _readBuf.Length - _pos;
        public int WriteLength => _buf.Count;
        public int Position
        {
            get => _pos;
            set => _pos = value;
        }

        public ByteBuf(byte[] data)
        {
            _readBuf = data;
        }
        public ByteBuf() {}

        public static byte[] GetVarInt(int integer)
        {
            var buf = new List<byte>();
            
            while ((integer & -128) != 0)
            {
                buf.Add((byte) (integer & 127 | 128));
                integer = (int) ((uint) integer >> 7);
            }
            
            buf.Add((byte) integer);
            return buf.ToArray();
        }
        
        public static int ReadVarInt(NetworkStream stream)
        {
            var value = 0;
            var size = 0;
            int b;

            while (((b = stream.ReadByte()) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 5)
                {
                    throw new IOException("VarInt too long.");
                }
            }

            return value | ((b & 0x7F) << (size * 7));
        }
        
        public static int ReadVarInt(byte[] data)
        {
            var value = 0;
            var size = 0;
            int b;

            while (((b = data[size]) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 5)
                {
                    throw new IOException("VarInt too long.");
                }
            }

            return value | ((b & 0x7F) << (size * 7));
        }

        public int ReadByte()
        {
            return _readBuf[_pos++];
        }

        public byte[] Read(int length)
        {
            var buffer = new byte[length];
            Buffer.BlockCopy(_readBuf, _pos, buffer, 0, length);
            _pos += length;
            
            return buffer;
        }

        public byte[] Peek(int length)
        {
            var buffer = new byte[length];
            Buffer.BlockCopy(_readBuf, _pos, buffer, 0, length);
            
            return buffer;
        }

        public string ReadUtf()
        {
            //https://stackoverflow.com/questions/26416779/c-sharp-binaryreader-readutf-from-javas-dataoutputstream
            var length = (ReadByte() << 8) + ReadByte();

            var bytes = Peek(length);
            var result = new char[length];

            var count = 0;
            var index = 0;
            
            while(count < length)
            {
                var b = bytes[count] & 0xff;
                if (b > 127) break;
                count++;
                result[index++] = (char) b;
            }

            while (count < length)
            {
                var b = bytes[count] & 0xff;
                switch (b >> 4)
                {
                    case >= 0 and <= 7:
                        count++;
                        result[index++] = (char) b;
                        break;
                    case 12: case 13:
                        count += 2;
                        if (count > length)
                        {
                            _pos += count;
                            throw new IOException("Count too long");
                        }

                        int b2 = bytes[count - 1];
                        if ((b2 & 0xC0) != 0x80)
                        {
                            _pos += count;
                            throw new IOException("Malformed Input");
                        }

                        result[index++] = (char) (((b & 0x1F) << 6) | (b2 & 0x3F));
                        break;
                    case 14:
                        count += 3;
                        if (count > length)
                        {
                            _pos += count;
                            throw new IOException("Count too long");
                        }

                        b2 = bytes[count - 2];
                        int b3 = bytes[count - 1];
                        if (((b2 & 0xC0) != 0x80) || ((b3 & 0xC0) != 0x80))
                        {
                            _pos += count;
                            throw new IOException("Malformed Input");
                        }

                        result[index++] = (char) (((b & 0x0F) << 12) | ((b2 & 0x3F) << 6) | ((b3 & 0x3F) << 0));
                        break;
                    default:
                    {
                        _pos += count;
                        throw new IOException("Malformed Input");
                    }
                }
            }

            _pos += count;
            return new string(result);
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }
        
        public int ReadVarInt()
        {
            var value = 0;
            var size = 0;
            int b;

            while (((b = ReadByte()) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 5)
                {
                    throw new IOException("VarInt too long.");
                }
            }

            return value | ((b & 0x7F) << (size * 7));
        }

        public int ReadInt()
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(Read(4)));
        }

        public string ReadString()
        {
            return Encoding.UTF8.GetString(Read(ReadVarInt()));
        }

        public long ReadLong()
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(Read(8)));
        }

        public short ReadShort()
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(Read(2)));
        }

        public float ReadFloat()
        {
            return NetworkToHostOrder(BitConverter.ToSingle(Read(4)));
        }

        public double ReadDouble()
        {
            return NetworkToHostOrder(Read(8));
        }

        public Uuid ReadUuid()
        {
            return new Uuid(ReadLong(), ReadLong());
        }

        public void Write(IEnumerable<byte> data)
        {
            _buf.AddRange(data);
        }

        public void Write(byte[] data, int length)
        {
            var arr = new byte[length];
            Buffer.BlockCopy(data, 0, arr, 0, length);
            _buf.AddRange(arr);
        }

        public void WriteUtf(string str)
        {
            var stringLength = str.Length;
            var length = 0;
            var count = 0;
            int b;

            for (int i = 0; i < stringLength; i++)
            {
                b = str[i];
                switch (b)
                {
                    case >= 0x0001 and <= 0x007F:
                        length++;
                        break;
                    case > 0x07FF:
                        length += 3;
                        break;
                    default:
                        length += 2;
                        break;
                }
            }

            if (length > 65535)
                throw new IOException($"String Too long :{length}");

            var arr = new byte[length * 2 + 2];
            // var arr = new byte[length];
            arr[count++] = (byte) (((uint) length >> 8) & 0xFF);
            arr[count++] = (byte) ((uint) length & 0xFF);

            int x = 0;
            while (x < stringLength)
            {
                b = str[x];
                if (b is not (>= 0x0001 and <= 0x007F))
                    break;
                arr[count++] = (byte) b;
                x++;
            }

            while (x < stringLength)
            {
                b = str[x];
                switch (b)
                {
                    case >= 0x0001 and <= 0x007F:
                        arr[count++] = (byte) b;
                        break;
                    case > 0x07FF:
                        arr[count++] = (byte) (0xE0 | ((b >> 12) & 0x0F));
                        arr[count++] = (byte) (0x80 | ((b >> 6) & 0x3F));
                        arr[count++] = (byte) (0x80 | ((b >> 0) & 0x3F));
                        break;
                    default:
                        arr[count++] = (byte) (0xC0 | ((b >>  6) & 0x1F));
                        arr[count++] = (byte) (0x80 | ((b >>  0) & 0x3F));
                        break;
                }
                x++;
            }
            
            Write(arr, count);
        }

        public void WriteByte(byte b)
        {
            _buf.Add(b);
        }

        public void WriteVarInt(int integer)
        {
            _buf.AddRange(GetVarInt(integer));
        }

        public void WriteInt(int data)
        {
            _buf.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data)));
        }

        public void WriteString(string data)
        {
            byte[] stringData = Encoding.UTF8.GetBytes(data);
            WriteVarInt(stringData.Length);
            _buf.AddRange(stringData);
        }

        public void WriteShort(short data)
        {
            _buf.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data)));
        }

        public void WriteUShort(ushort data)
        {
            _buf.AddRange(BitConverter.GetBytes(data));
            // _buf.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data)));
        }

        public void WriteBool(bool data)
        {
            _buf.Add((byte) (data ? 1 : 0));
        }

        public void WriteDouble(double data)
        {
            _buf.AddRange(HostToNetworkOrder(data));
        }

        public void WriteFloat(float data)
        {
            _buf.AddRange(BitConverter.GetBytes(data));
            // buf.AddRange(HostToNetworkOrder(data));
        }

        public void WriteLong(long data)
        {
            _buf.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data)));
        }

        public void WriteULong(ulong data)
        {
            _buf.AddRange(BitConverter.GetBytes((ulong) IPAddress.HostToNetworkOrder((long) data)));
        }

        public void WriteLongArray(long[] array)
        {
            var size = array.Length;
            WriteVarInt(size);
            for(var i = 0; i < size; i++)
                WriteLong(array[i]);
        }

        public void WriteUuid(Uuid uuid)
        {
            WriteLong(uuid.GetMostSignificantBits());
            WriteLong(uuid.GetLeastSignificantBits());
        }

        private byte[] HostToNetworkOrder(double d)
        {
            byte[] data = BitConverter.GetBytes(d);
            if(BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return data;
        }

        private float NetworkToHostOrder(float host)
        {
            var data = BitConverter.GetBytes(host);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToSingle(data, 0);
        }

        private double NetworkToHostOrder(byte[] data)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return BitConverter.ToDouble(data, 0);
        }

        public byte[] Flush()
        {
            _buf.InsertRange(0, GetVarInt(_buf.Count));
            var data = _buf.ToArray();

            _readBuf = null;
            _buf.Clear();

            return data;
        }

        public byte[] GetBytes()
        {
            return _buf.ToArray();
        }

        public byte[] GetReadBytes()
        {
            return _readBuf;
        }
    }
}