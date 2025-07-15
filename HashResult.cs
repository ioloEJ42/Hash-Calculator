using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashCalculator
{
    public class HashResult
    {
        public string FileName { get; set; } = "";
        public string Algorithm { get; set; } = "";
        public string HashValue { get; set; } = "";
        public string Status { get; set; } = "";
        public string ExpectedHash { get; set; } = "";
        public string VerificationStatus { get; set; } = "";
    }
}