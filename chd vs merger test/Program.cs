using System;
using System.Collections.Generic;
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

                Dictionary<string, ds_gs> dupes = new Dictionary<string, ds_gs>();

                FileStream output = new FileStream(test_file + ".mrg", FileMode.Create);
                GZipStream gz = new GZipStream(output, CompressionLevel.Optimal);

                MemoryStream map = new MemoryStream();
                BinaryWriter map_bw = new BinaryWriter(map);

                long size = 0;

                using (BinaryReader br = new BinaryReader(new FileStream(test_file, FileMode.Open)))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        byte[] temp_block = br.ReadBytes(512);

                        string temp_block_md5 = CalcMD5(temp_block);

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
                            block.CopyTo(gz);

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
                gz.Dispose();
                output.Dispose();
            }
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