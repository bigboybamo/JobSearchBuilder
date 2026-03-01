using JobSearchBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSearchBuilder
{
    public interface IProfileStore
    {
        IReadOnlyList<SearchProfile> GetAll();
        SearchProfile GetById(int id);
        SearchProfile Save(SearchProfile profile);
        void Delete(int id);
    }
}
