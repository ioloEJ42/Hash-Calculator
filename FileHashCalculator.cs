using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace HashCalculator
{
    public class FileHashCalculator
    {
        public async Task<string> CalculateHashAsync(string filePath, string algorithm)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var hashAlgorithm = CreateHashAlgorithm(algorithm))
                {
                    byte[] hashBytes = await Task.Run(() => hashAlgorithm.ComputeHash(fileStream));
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }

        private HashAlgorithm CreateHashAlgorithm(string algorithm)
        {
            return algorithm.ToUpper() switch
            {
                "MD5" => MD5.Create(),
                "SHA256" => SHA256.Create(),
                _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
            };
        }
    }
}