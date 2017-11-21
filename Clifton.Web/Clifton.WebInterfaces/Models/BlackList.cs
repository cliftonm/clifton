using System;
using System.Data.Linq.Mapping;

using Clifton.Core.ModelTableManagement;

namespace Clifton.WebInterfaces
{
    [Table]
    public class BlackList : IEntity
    {
        [Column(IsPrimaryKey = true, AutoSync = AutoSync.OnInsert, IsDbGenerated = true)]
        public int? Id { get; set; }

        [Column(CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        public string IP { get; set; }

        [Column(CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        public DateTime LastHit { get; set; }

        [Column(CanBeNull = false, UpdateCheck = UpdateCheck.Never)]
        public int Hits { get; set; }
    }
}
