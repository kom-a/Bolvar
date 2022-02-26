using System;
using System.Windows.Documents;

namespace Bolvar.Utils
{
    public class PreviewData : ICloneable
    {
        public FlowDocument[] Documents;
        public int Index;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
