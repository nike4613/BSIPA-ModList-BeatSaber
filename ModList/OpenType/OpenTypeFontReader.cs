using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.ModList.BeatSaber.OpenType
{
    public class OpenTypeFontReader : OpenTypeReader
    {
        public OpenTypeFontReader(Stream input) : base(input)
        {
        }

        public OpenTypeFontReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public OpenTypeFontReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public OffsetTable ReadOffsetTable()
            => new OffsetTable()
            {
                SFNTVersion = ReadUInt32(),
                NumTables = ReadUInt16(),
                SearchRange = ReadUInt16(),
                EntrySelector = ReadUInt16(),
                RangeShift = ReadUInt16(),
            };

        public TableRecord ReadTableRecord()
            => new TableRecord()
            {
                TableTag = ReadTag(),
                Checksum = ReadUInt32(),
                Offset = ReadOffset32(),
                Length = ReadUInt32()
            };

        public TableRecord[] ReadTableRecords(OffsetTable offsets)
            => Enumerable.Range(0, offsets.NumTables)
                .Select(_ => ReadTableRecord()).ToArray();

        public TableRecord[] ReadAllTables() => ReadTableRecords(ReadOffsetTable());

        public OpenTypeTable TryReadTable(TableRecord table)
        {
            BaseStream.Position = table.Offset;

            OpenTypeTable result = null;
            if (table.TableTag == OpenTypeTag.NAME)
                result = new OpenTypeNameTable();

            result?.ReadFrom(this, table.Length);

            return result;
        }
    }
}
