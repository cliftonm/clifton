using System;
using System.Data.Linq.Mapping;

using Clifton.Core.ModelTableManagement;

namespace Clifton.WebInterfaces
{
    [Table]
    public class WhiteList : IEntity
    {
        [Column(IsPrimaryKey = true, AutoSync = AutoSync.OnInsert, IsDbGenerated = true)]
        public int? Id { get; set; }

        [Column(CanBeNull = false)]
        public string IP { get; set; }

        [Column(CanBeNull = false)]
        public DateTime LastHit { get; set; }

        [Column(CanBeNull = false)]
        public int Hits { get; set; }
    }
}
