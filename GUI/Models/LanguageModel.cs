using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class LanguageModel
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public LanguageModel(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }
}
