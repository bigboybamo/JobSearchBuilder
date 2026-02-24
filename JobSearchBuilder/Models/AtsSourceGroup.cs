using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSearchBuilder.Models
{
    public class AtsSourceGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Domains { get; set; }

        public AtsSourceGroup()
        {
            Name = string.Empty;
            Domains = new List<string>();
        }

        public override string ToString() { return Name; }
    }
}
