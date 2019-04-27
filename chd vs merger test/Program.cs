using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace chd_vs_merger_test
{
    class Program
    {
        struct ds_gs
        {
            public int offset;
            public int size;
        }

        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                string test_file = arg;

                string type = GetType(test_file);

                Dictionary<string, ds_gs> dupes = new Dictionary<string, ds_gs>();

                FileStream output = new FileStream(test_file + ".mrg", FileMode.Create);

                BinaryWriter bw = new BinaryWriter(output);

                MemoryStream map = new MemoryStream();
                BinaryWriter map_bw = new BinaryWriter(map);

                long size = 0;

                long i2448_1 = 0;
                long i2448_2 = 0;

                using (BinaryReader br = new BinaryReader(new FileStream(test_file, FileMode.Open)))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        byte[] temp_block = new byte[2048];
                        string temp_block_md5 = string.Empty;

                        switch (type)
                        {
                            case "2448-1":
                                br.BaseStream.Seek(i2448_1++ * 2448 + 16, SeekOrigin.Begin);
                                temp_block = br.ReadBytes(2048);
                                temp_block_md5 = CalcMD5(temp_block);
                                br.BaseStream.Seek(384, SeekOrigin.Current);
                                break;
                            case "2448-2":
                                br.BaseStream.Seek(i2448_2++ * 2448 + 16 + 8, SeekOrigin.Begin);
                                temp_block = br.ReadBytes(2048);
                                temp_block_md5 = CalcMD5(temp_block);
                                br.BaseStream.Seek(376, SeekOrigin.Current);
                                break;
                        }

                        if (dupes.ContainsKey(temp_block_md5))
                        {
                            map_bw.Write(dupes[temp_block_md5].offset);
                            map_bw.Write(dupes[temp_block_md5].size);
                        }
                        else
                        {
                            ds_gs offsets = new ds_gs();
                            offsets.offset = (int)output.Position;

                            MemoryStream block = new MemoryStream(temp_block);

                            MemoryStream block_compressed = new MemoryStream();
                            DeflateStream zip = new DeflateStream(block_compressed, CompressionLevel.Optimal);
                            block.CopyTo(zip);
                            zip.Close();

                            bw.Write(block_compressed.ToArray());

                            offsets.size = (int)(output.Position - size);
                            dupes.Add(temp_block_md5, offsets);

                            map_bw.Write(offsets.offset);
                            map_bw.Write(offsets.size);

                            size = output.Position;
                        }
                    }
                }

                File.WriteAllBytes(test_file + ".map", map.ToArray());

                map_bw.Dispose();
                output.Dispose();
            }
        }

        private static string GetType(string test_file)
        {
            string type = string.Empty;

            byte[] sync = { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00 };

            using (BinaryReader br = new BinaryReader(new FileStream(test_file, FileMode.Open)))
            {
                bool raw = br.ReadBytes(12).SequenceEqual(sync);

                if (raw)
                {
                    br.BaseStream.Position = 2352;
                    if (br.ReadBytes(12).SequenceEqual(sync))
                    {
                        br.BaseStream.Position += 3;
                        byte mode = br.ReadByte();
                        switch (mode)
                        {
                            case 1:
                                return "2352-1";
                            case 2:
                                return "2352-2";
                        }
                    }

                    br.BaseStream.Position = 2448;
                    if (br.ReadBytes(12).SequenceEqual(sync))
                    {
                        br.BaseStream.Position += 3;
                        byte mode = br.ReadByte();
                        switch (mode)
                        {
                            case 1:
                                return "2448-1";
                            case 2:
                                return "2448-2";
                        }
                    }
                }
            }

            return "512";
        }

        private static string CalcMD5(byte[] temp_block)
        {
            string md5 = string.Empty;
            using (MD5 hash = MD5.Create())
            {
                md5 = BitConverter.ToString(hash.ComputeHash(temp_block)).Replace("-", "").ToLower();
            }
            return md5;
        }
    }
}