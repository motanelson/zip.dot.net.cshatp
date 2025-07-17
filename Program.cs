using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.Yellow;
        Console.Write("Ficheiros para zip (separados por espaço): ");
        string[] files = Console.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string zipName = "output.zip";

        using (FileStream zip = new FileStream(zipName, FileMode.Create, FileAccess.Write))
        {
            List<(string name, uint crc, uint size, uint offset)> centralDir = new();
            foreach (string file in files)
            {
                byte[] data = File.ReadAllBytes(file);
                uint crc = CRC32(data);
                uint offset = (uint)zip.Position;

                WriteLocalHeader(zip, file, crc, (uint)data.Length);
                zip.Write(data, 0, data.Length);

                centralDir.Add((file, crc, (uint)data.Length, offset));
            }

            uint centralDirStart = (uint)zip.Position;
            foreach (var entry in centralDir)
                WriteCentralDirectory(zip, entry.name, entry.crc, entry.size, entry.offset);
            uint centralDirEnd = (uint)zip.Position;

            WriteEndOfCentralDirectory(zip, (ushort)centralDir.Count, centralDirEnd - centralDirStart, centralDirStart);
        }

        Console.WriteLine("ZIP criado com sucesso: " + zipName);
    }

    static void WriteLocalHeader(FileStream zip, string name, uint crc, uint size)
    {
        ushort dosTime = DosTime(), dosDate = DosDate();

        zip.Write(Encoding.ASCII.GetBytes("PK\x03\x04"));
        WriteU16(zip, 20); // version needed
        WriteU16(zip, 0);  // flags
        WriteU16(zip, 0);  // no compression
        WriteU16(zip, dosTime);
        WriteU16(zip, dosDate);
        WriteU32(zip, crc);
        WriteU32(zip, size);
        WriteU32(zip, size);
        WriteU16(zip, (ushort)name.Length);
        WriteU16(zip, 0);
        zip.Write(Encoding.UTF8.GetBytes(name));
    }

    static void WriteCentralDirectory(FileStream zip, string name, uint crc, uint size, uint offset)
    {
        ushort dosTime = DosTime(), dosDate = DosDate();

        zip.Write(Encoding.ASCII.GetBytes("PK\x01\x02"));
        WriteU16(zip, 0x0314); // version made by (DOS)
        WriteU16(zip, 20);     // version needed
        WriteU16(zip, 0);
        WriteU16(zip, 0);
        WriteU16(zip, dosTime);
        WriteU16(zip, dosDate);
        WriteU32(zip, crc);
        WriteU32(zip, size);
        WriteU32(zip, size);
        WriteU16(zip, (ushort)name.Length);
        WriteU16(zip, 0);  // extra
        WriteU16(zip, 0);  // comment
        WriteU16(zip, 0);  // disk start
        WriteU16(zip, 0);  // int attrs
        WriteU32(zip, 0);  // ext attrs
        WriteU32(zip, offset);
        zip.Write(Encoding.UTF8.GetBytes(name));
    }

    static void WriteEndOfCentralDirectory(FileStream zip, ushort count, uint size, uint offset)
    {
        zip.Write(Encoding.ASCII.GetBytes("PK\x05\x06"));
        WriteU16(zip, 0); // disk
        WriteU16(zip, 0); // disk with CD
        WriteU16(zip, count);
        WriteU16(zip, count);
        WriteU32(zip, size);
        WriteU32(zip, offset);
        WriteU16(zip, 0); // comment
    }

    static void WriteU16(FileStream f, ushort v)
    {
        f.WriteByte((byte)(v & 0xFF));
        f.WriteByte((byte)(v >> 8));
    }

    static void WriteU32(FileStream f, uint v)
    {
        f.WriteByte((byte)(v & 0xFF));
        f.WriteByte((byte)((v >> 8) & 0xFF));
        f.WriteByte((byte)((v >> 16) & 0xFF));
        f.WriteByte((byte)((v >> 24) & 0xFF));
    }

    static ushort DosTime()
    {
        var t = DateTime.Now;
        return (ushort)((t.Hour << 11) | (t.Minute << 5) | (t.Second / 2));
    }

    static ushort DosDate()
    {
        var t = DateTime.Now;
        return (ushort)(((t.Year - 1980) << 9) | (t.Month << 5) | t.Day);
    }

    static uint[] crcTable = null;
    static uint CRC32(byte[] data)
    {
        if (crcTable == null)
        {
            crcTable = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int j = 0; j < 8; j++)
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                crcTable[i] = c;
            }
        }

        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
            crc = crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return ~crc;
    }
}
