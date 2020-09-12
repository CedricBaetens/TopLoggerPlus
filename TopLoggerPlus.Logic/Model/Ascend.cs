using System;
using System.Collections.Generic;
using System.Text;

namespace TopLoggerPlus.Logic
{
    public class Ascend
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int climb_id { get; set; }
        public bool topped { get; set; }
        public DateTime date_logged { get; set; }
        public bool used { get; set; }
        public int checks { get; set; }
    }
}
