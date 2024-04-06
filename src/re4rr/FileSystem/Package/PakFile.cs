﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using REE;

namespace IntelOrca.Biohazard.BioRand.RE4R.FileSystem.Package
{
    internal class PakFile : IDisposable
    {
        private readonly FileStream _fileStream;
        private readonly List<PakEntry> _entries = new List<PakEntry>();

        public string Path { get; }
        public int NumEntries => _entries.Count;

        public PakFile(string path)
        {
            Path = path;
            var stream = File.OpenRead(path);
            try
            {
                if (stream.Length <= 16)
                    throw new InvalidDataException("Empty PAK archive file");

                var m_Header = new PakHeader();
                m_Header.dwMagic = stream.ReadUInt32();
                m_Header.bMajorVersion = (byte)stream.ReadByte();
                m_Header.bMinorVersion = (byte)stream.ReadByte();
                m_Header.wFeature = stream.ReadInt16();
                m_Header.dwTotalFiles = stream.ReadInt32();
                m_Header.dwHash = stream.ReadUInt32();

                if (m_Header.dwMagic != 0x414B504B)
                    throw new InvalidDataException("Invalid magic of PAK archive file");

                if (m_Header.bMajorVersion != 2 && m_Header.bMajorVersion != 4 || m_Header.bMinorVersion != 0 && m_Header.bMinorVersion != 1)
                    throw new InvalidDataException("Invalid version of PAK archive file -> " + m_Header.bMajorVersion.ToString() + "." + m_Header.bMinorVersion.ToString() + ", expected 2.0, 4.0 & 4.1");

                if (m_Header.wFeature != 0 && m_Header.wFeature != 8)
                    throw new InvalidDataException("Archive is encrypted (obfuscated) with an unsupported algorithm");

                int dwEntrySize = 0;
                switch (m_Header.bMajorVersion)
                {
                    case 2: dwEntrySize = 24; break;
                    case 4: dwEntrySize = 48; break;
                    default: break;
                }

                var lpTable = stream.ReadBytes(m_Header.dwTotalFiles * dwEntrySize);

                if (m_Header.wFeature == 8)
                {
                    var lpEncryptedKey = stream.ReadBytes(128);

                    lpTable = PakCipher.iDecryptData(lpTable, lpEncryptedKey);
                }

                _entries.Clear();
                using (var TEntryReader = new MemoryStream(lpTable))
                {
                    for (var i = 0; i < m_Header.dwTotalFiles; i++)
                    {
                        var entry = new PakEntry();
                        if (m_Header.bMajorVersion == 2 && m_Header.bMinorVersion == 0)
                        {
                            entry.dwOffset = TEntryReader.ReadInt64();
                            entry.dwDecompressedSize = TEntryReader.ReadInt64();
                            entry.dwHashNameLower = TEntryReader.ReadUInt32();
                            entry.dwHashNameUpper = TEntryReader.ReadUInt32();
                            entry.dwCompressedSize = 0;
                            entry.wCompressionType = 0;
                            entry.dwChecksum = 0;
                        }
                        else if (m_Header.bMajorVersion == 2 && m_Header.bMajorVersion == 4 || m_Header.bMinorVersion == 0 || m_Header.bMinorVersion == 1)
                        {
                            entry.dwHashNameLower = TEntryReader.ReadUInt32();
                            entry.dwHashNameUpper = TEntryReader.ReadUInt32();
                            entry.dwOffset = TEntryReader.ReadInt64();
                            entry.dwCompressedSize = TEntryReader.ReadInt64();
                            entry.dwDecompressedSize = TEntryReader.ReadInt64();
                            entry.wCompressionType = PakUtils.iGetCompressionType(TEntryReader.ReadInt64());
                            entry.dwChecksum = TEntryReader.ReadUInt64();
                        }
                        else
                        {
                            throw new InvalidDataException("Something is wrong when reading the entry table");
                        }
                        _entries.Add(entry);
                    }
                }
                _fileStream = stream;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
        }

        public byte[]? GetFileData(string path)
        {
            var entryIndex = FindEntry(path);
            if (entryIndex != -1)
            {
                return GetEntryData(_entries[entryIndex]);
            }
            return null;
        }

        private int FindEntry(string path)
        {
            ulong hash;
            uint lower;
            uint upper;
            path = path.Replace("\\", "/");
            if (path.Contains("__Unknown"))
            {
                var pathWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(path);
                hash = Convert.ToUInt64(pathWithoutExtension, 16);
            }
            else
            {
                lower = PakHash.iGetHash(path.ToLowerInvariant());
                upper = PakHash.iGetHash(path.ToUpperInvariant());
                hash = ((ulong)upper << 32) | lower;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.dwHashName == hash)
                {
                    return i;
                }
            }

            return -1;
        }

        public async Task ExtractAllAsync(string destinationPath, CancellationToken ct)
        {
            foreach (var entry in _entries)
            {
                ct.ThrowIfCancellationRequested();

                var fileName = PakList.iGetNameFromHashList((ulong)entry.dwHashNameUpper << 32 | entry.dwHashNameLower);
                var fullPath = System.IO.Path.Combine(destinationPath, fileName!);

                Utils.iCreateDirectory(fullPath);

                var entryData = GetEntryData(in entry);
                fullPath = PakUtils.iDetectFileType(fullPath, entryData);
                await File.WriteAllBytesAsync(fullPath, entryData, ct);
            }
        }

        private byte[] GetEntryData(in PakEntry entry)
        {
            _fileStream.Seek(entry.dwOffset, SeekOrigin.Begin);
            if (entry.wCompressionType == PakFlags.NONE)
            {
                return _fileStream.ReadBytes(PakUtils.iGetSize(entry.dwCompressedSize, entry.dwDecompressedSize));
            }
            else if (entry.wCompressionType == PakFlags.DEFLATE || entry.wCompressionType == PakFlags.ZSTD)
            {
                var lpSrcBuffer = _fileStream.ReadBytes((int)entry.dwCompressedSize);
                var lpDstBuffer = Array.Empty<byte>();
                switch (entry.wCompressionType)
                {
                    case PakFlags.DEFLATE: lpDstBuffer = DEFLATE.iDecompress(lpSrcBuffer); break;
                    case PakFlags.ZSTD: lpDstBuffer = ZSTD.iDecompress(lpSrcBuffer); break;
                }
                return lpDstBuffer;
            }
            else
            {
                throw new InvalidDataException("Unknown compression id detected -> " + entry.wCompressionType.ToString());
            }
        }
    }
}
