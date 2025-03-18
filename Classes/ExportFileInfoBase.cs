using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipsOrganizer.Model{
    public class ExportFileInfoBase : Item {
        public string OutputPath { get; set; }
        public string OutputFormat { get; set; }
        public int Quality { get; set; } = 5;
        public ExportFileInfoBase() { }
        public ExportFileInfoBase(Item item) {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            this.Name = item.Name;
            this.Path = item.Path;
            this.Date = item.Date;
            this.Color = item.Color;
        }
    }
}
