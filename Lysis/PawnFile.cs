using System;
using System.Collections.Generic;
using System.IO;

namespace Lysis
{
    public class Public
    {
        private readonly uint address_;
        private readonly string name_;

        public Public(string name, uint address)
        {
            name_ = name;
            address_ = address;
        }

        public string name => name_;
        public uint address => address_;
    }

    public abstract class PawnFile
    {
        protected Function[] functions_;
        protected Public[] publics_;
        protected Variable[] globals_;

        public static PawnFile FromFile(string path)
        {
            var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var bytes = new List<byte>();
            int b;
            while ((b = fs.ReadByte()) >= 0)
            {
                bytes.Add((byte)b);
            }

            var vec = bytes.ToArray();
            var magic = BitConverter.ToUInt32(vec, 0);
            if (magic == SourcePawn.SourcePawnFile.MAGIC)
            {
                return new SourcePawn.SourcePawnFile(vec);
            }

            throw new Exception("not a .smx file!");
        }

        public abstract string stringFromData(int address);
        public abstract float floatFromData(int address);
        public abstract int int32FromData(int address);

        public Function lookupFunction(uint pc)
        {
            for (var i = 0; i < functions_.Length; i++)
            {
                var f = functions_[i];
                if (pc >= f.codeStart && pc < f.codeEnd)
                {
                    return f;
                }
            }
            return null;
        }
        public Public lookupPublic(string name)
        {
            for (var i = 0; i < publics_.Length; i++)
            {
                if (publics_[i].name == name)
                {
                    return publics_[i];
                }
            }
            return null;
        }

        public Public lookupPublic(uint addr)
        {
            for (var i = 0; i < publics_.Length; i++)
            {
                if (publics_[i].address == addr)
                {
                    return publics_[i];
                }
            }
            return null;
        }

        public Function[] functions => functions_;
        public Public[] publics => publics_;
        public Variable[] globals => globals_;
        public abstract byte[] DAT
        {
            get;
        }
    }
}

