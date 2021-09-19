using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace H2CodezPatcher
{
    class Installer
    {
        private readonly string BasePath;
        internal Installer(string basePath)
        {
            BasePath = basePath;
        }
        private const string FETCH_URL = @"https://github.com/Project-Cartographer/H2Codez/raw/H2OS_EK/Patches/";
        static string CalculateMD5(string filename)
        {
            if (!File.Exists(filename))
                return "";
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private void ForceMove(string sourceFilename, string destinationFilename)
        {
            if (!File.Exists(sourceFilename))
                return;
            if (File.Exists(destinationFilename))
            {
                System.IO.File.Delete(destinationFilename);
            }

            System.IO.File.Move(sourceFilename, destinationFilename);
        }

        [Flags]
        internal enum file_list : Byte
        {
            none = 0,
            tool = 2,
            sapien = 4,
            guerilla = 8
        }

        internal bool check_files( ref file_list files_to_patch)
        {
            string h2tool = CalculateMD5(BasePath + "h2tool.exe");
            if (h2tool == "dc221ca8c917a1975d6b3dd035d2f862")
                files_to_patch |= file_list.tool;
            else if (h2tool != "f81c24da93ce8d114caa8ba0a21c7a63")
                return false;

            string h2sapien = CalculateMD5(BasePath + "h2sapien.exe");
            if (h2sapien == "d86c488b7c8f64b86f90c732af01bf50")
                files_to_patch |= file_list.sapien;
            else if (h2sapien != "975c0d0ad45c1687d11d7d3fdfb778b8")
                return false;

            string h2guerilla = CalculateMD5(BasePath + "h2guerilla.exe");
            if (h2guerilla == "ce3803cc90e260b3dc59854d89b3ea88")
                files_to_patch |= file_list.guerilla;
            else if (h2guerilla != "55b09d5a6c8ecd86988a5c0f4d59d7ea")
                return false;

            return true;
        }

        internal void patch_file(string name, WebClient wc)
        {
            byte[] patch_data = wc.DownloadData(FETCH_URL + name + ".patch");
            using (FileStream unpatched_file = new FileStream(BasePath + name, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream patched_file = new FileStream(BasePath + name + ".patched", FileMode.Create))
                BsDiff.BinaryPatchUtility.Apply(unpatched_file, () => new MemoryStream(patch_data), patched_file);
            ForceMove(BasePath + name, BasePath + "backup\\" + name);
            ForceMove(BasePath + name + ".patched", BasePath + name);
        }

        internal void ApplyPatches(file_list files_to_patch, WebClient wc)
        {
            Directory.CreateDirectory(BasePath + "backup");
            ForceMove(BasePath + "Halo_2_Map_Editor_Launcher.exe", BasePath + "backup\\Halo_2_Map_Editor_Launcher.exe");
            if (files_to_patch.HasFlag(file_list.tool))
                patch_file("h2tool.exe", wc);
            if (files_to_patch.HasFlag(file_list.guerilla))
                patch_file("h2guerilla.exe", wc);
            if (files_to_patch.HasFlag(file_list.sapien))
                patch_file("h2sapien.exe", wc);
        }
    }
    class Program
    {
        static string GetInstallPath()
        {
            string? installPath = null;
            while (!Directory.Exists(installPath))
            {
                Console.WriteLine("Enter install path:");
                installPath = Console.ReadLine();
            }
            return installPath;
        }
        static void Main(string[] args)
        {
            string installPath = GetInstallPath();
            Console.WriteLine($"Installing h2codez to {installPath}");
            Installer installer = new(installPath);
            Installer.file_list files_to_patch = Installer.file_list.none;
            if (!installer.check_files(ref files_to_patch))
            {
                Console.WriteLine("Error installing unsupported version!");
                return;
            }
            if (files_to_patch == Installer.file_list.none)
            {
                Console.WriteLine("No files to patch, h2codez installed already!");
                return;
            }
            var wc = new WebClient();
            installer.ApplyPatches(files_to_patch, wc);
        }
    }
}
