﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Symbols
{
    /// <summary>
    /// Parser for .r2rmap files generated by the .NET crossgen2 compiler.
    /// Format spec: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/r2r-perfmap-format.md
    /// </summary>
    internal class R2RPerfMapSymbolModule : ISymbolLookup
    {
        private readonly uint _loadedLayoutTextOffset;
        private readonly List<R2RPerfMapSymbol> _symbols = new List<R2RPerfMapSymbol>();

        public R2RPerfMapSymbolModule(string filePath, uint loadedLayoutTextOffset)
        {
            _loadedLayoutTextOffset = loadedLayoutTextOffset;
            ReadAndParse(new FileStream(filePath, FileMode.Open, FileAccess.Read));
        }

        public R2RPerfMapSymbolModule(Stream stream, uint loadedLayoutTextOffset)
        {
            _loadedLayoutTextOffset = loadedLayoutTextOffset;
            ReadAndParse(stream);
        }

        public string FindNameForRva(uint rva, ref uint symbolStart)
        {
            // Shift the RVA by the difference between the expected loaded layout and file offset.
            // This is required because of how CoreCLR loads PE images on Linux.  Instead of reserving
            // and loading the whole image, it loads the image in chunks using multiple mappings.
            // RVAs are expected to be relative from the image base, but we don't actually have the virtual address
            // of the image base, instead we have the virtual address of the start of the text section.
            // Adding the offset here ensures that the math conforms to the expectation that IP - ImageBase = RVA.
            rva = rva + _loadedLayoutTextOffset;
            int index = BinarySearch(rva);
            if (index >= 0 && index < _symbols.Count)
            {
                var symbol = _symbols[index];
                symbolStart = symbol.StartAddress;
                return symbol.Name;
            }

            return string.Empty;
        }

        public Guid Signature { get; private set; }
        public uint Version { get; private set; }
        public R2RPerfMapOS TargetOS { get; private set; }
        public R2RPerfMapArchitecture TargetArchitecture { get; private set; }
        public R2RPerfMapABI TargetABI { get; private set; }

        private void ReadAndParse(Stream input)
        {
            using (StreamReader reader = new StreamReader(input))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    R2RPerfMapSymbol symbol = ParseLine(line);
                    OnParsedSymbol(symbol);
                }
            }
            FinalizeSymbols();
        }

        private R2RPerfMapSymbol ParseLine(string line)
        {
            // Find the first space
            int firstSpaceIndex = line.IndexOf(' ');
            if (firstSpaceIndex == -1)
            {
                throw new FormatException($"Invalid line format - missing first space");
            }

            // Parse the address
            string addressStr = line.Substring(0, firstSpaceIndex);
            if (!uint.TryParse(addressStr, System.Globalization.NumberStyles.HexNumber, null, out uint address))
            {
                throw new FormatException($"Could not parse address from '{addressStr}'");
            }

            // Find second space and parse remainder
            string remainder = line.Substring(firstSpaceIndex + 1);
            int secondSpaceIndex = remainder.IndexOf(' ');
            if (secondSpaceIndex == -1)
            {
                throw new FormatException($"Invalid line format - missing second space");
            }

            // Parse the size
            string sizeStr = remainder.Substring(0, secondSpaceIndex);
            if (!uint.TryParse(sizeStr, System.Globalization.NumberStyles.HexNumber, null, out uint size))
            {
                throw new FormatException($"Could not parse size from '{sizeStr}'");
            }

            // Get the name (everything after the second space)
            string name = remainder.Substring(secondSpaceIndex + 1);

            return new R2RPerfMapSymbol(address, size, name);
        }

        private void OnParsedSymbol(R2RPerfMapSymbol symbol)
        {
            switch (symbol.StartAddress)
            {
                case 0xFFFFFFFF:
                    if (Guid.TryParse(symbol.Name, out Guid signature))
                    {
                        Signature = signature;
                    }
                    else
                    {
                        throw new FormatException($"Could not parse signature from '{symbol.Name}'");
                    }
                    break;
                case 0xFFFFFFFE:
                    uint parsedVersion;
                    if (uint.TryParse(symbol.Name, out parsedVersion))
                    {
                        Version = parsedVersion;
                    }
                    else
                    {
                        throw new FormatException($"Could not parse version from '{symbol.Name}'");
                    }
                    break;
                case 0xFFFFFFFD:
                    if (Enum.TryParse(symbol.Name, out R2RPerfMapOS os) && os < R2RPerfMapOS.Invalid)
                    {
                        TargetOS = os;
                    }
                    else
                    {
                        throw new FormatException($"Could not parse OS from '{symbol.Name}'");
                    }
                    break;
                case 0xFFFFFFFC:
                    if (Enum.TryParse(symbol.Name, out R2RPerfMapArchitecture arch) && arch < R2RPerfMapArchitecture.Invalid)
                    {
                        TargetArchitecture = arch;
                    }
                    else
                    {
                        throw new FormatException($"Could not parse architecture from '{symbol.Name}'");
                    }
                    break;
                case 0xFFFFFFFB:
                    if (Enum.TryParse(symbol.Name, out R2RPerfMapABI abi) && abi < R2RPerfMapABI.Invalid)
                    {
                        TargetABI = abi;
                    }
                    else
                    {
                        throw new FormatException($"Could not parse ABI from '{symbol.Name}'");
                    }
                    break;
                default:
                    _symbols.Add(symbol);
                    break;
            }
        }

        private void FinalizeSymbols()
        {
            _symbols.Sort((a, b) => a.StartAddress.CompareTo(b.StartAddress));
        }

        private int BinarySearch(uint rva)
        {
            int left = 0;
            int right = _symbols.Count - 1;

            while (left <= right)
            {
                int mid = left + ((right - left) >> 1);
                var symbol = _symbols[mid];

                // Check if address falls within this symbol's range
                if (rva >= symbol.StartAddress && rva < symbol.StartAddress + symbol.Size)
                {
                    return mid;
                }

                // If address is less than symbol start, search left half
                if (rva < symbol.StartAddress)
                {
                    right = mid - 1;
                }
                // If address is beyond symbol range, search right half
                else
                {
                    left = mid + 1;
                }
            }

            return -1;
        }
    }

    internal class R2RPerfMapSymbol
    {
        public R2RPerfMapSymbol(uint startAddress, uint size, string name)
        {
            StartAddress = startAddress;
            Size = size;
            Name = name;
        }

        public uint StartAddress { get; private set; }
        public uint Size { get; private set; }
        public string Name { get; private set; }
    }

    internal enum R2RPerfMapArchitecture
    {
        Unknown = 0,
        ARM = 1,
        ARM64 = 2,
        X64 = 3,
        X86 = 4,
        Invalid = 5,
    }

    internal enum R2RPerfMapOS
    {
        Unknown = 0,
        Windows = 1,
        Linux = 2,
        OSX = 3,
        FreeBSD = 4,
        NetBSD = 5,
        SunOS = 6,
        Invalid = 7,
    }

    internal enum R2RPerfMapABI
    {
        Unknown = 0,
        Default = 1,
        Armel = 2,
        Invalid = 3,
    }
}
