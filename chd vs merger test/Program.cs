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
            public int ds_position;
            public int gs_position;
        }

        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                string test_file = arg;

                Dictionary<string, ds_gs> dupes = new Dictionary<string, ds_gs>();

                FileStream fs_for_ds = new FileStream(test_file + ".ds.mrg", FileMode.Create);
                FileStream gs_for_gs = new FileStream(test_file + ".gs.mrg", FileMode.Create);

                DeflateStream ds = new DeflateStream(fs_for_ds, CompressionLevel.Optimal);
                GZipStream gs = new GZipStream(gs_for_gs, CompressionLevel.Optimal);

                MemoryStream map_ds = new MemoryStream();
                MemoryStream map_gs = new MemoryStream();

                BinaryWriter writer_map_ds = new BinaryWriter(map_ds);
                BinaryWriter writer_map_gs = new BinaryWriter(map_gs);

                using (BinaryReader br = new BinaryReader(new FileStream(test_file, FileMode.Open)))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        byte[] temp_block = br.ReadBytes(512);

                        string temp_block_md5 = CalcMD5(temp_block);

                        if (dupes.ContainsKey(temp_block_md5))
                        {
                            writer_map_ds.Write(dupes[temp_block_md5].ds_position);
                            writer_map_gs.Write(dupes[temp_block_md5].gs_position);
                        }
                        else
                        {
                            ds_gs offsets = new ds_gs();
                            int ds_position = (int)ds.BaseStream.Position;
                            int gs_position = (int)gs.BaseStream.Position;
                            dupes.Add(temp_block_md5, offsets);

                            writer_map_ds.Write(ds_position);
                            writer_map_gs.Write(gs_position);

                            MemoryStream block = new MemoryStream(temp_block);
                            block.CopyTo(ds);
                            block.Position = 0;
                            block.CopyTo(gs);
                        }
                    }
                }

                File.WriteAllBytes(test_file + ".ds.map", map_ds.ToArray());
                File.WriteAllBytes(test_file + ".gs.map", map_gs.ToArray());

                writer_map_ds.Dispose();
                writer_map_gs.Dispose();

                ds.Dispose();
                gs.Dispose();

                fs_for_ds.Dispose();
                gs_for_gs.Dispose();
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