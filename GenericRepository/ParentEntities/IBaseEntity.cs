using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepositories.ParentEntities
{
    public interface IBaseEntity
    {

    }
    public interface IBaseEntity<TKey, TDateProperty> : IBaseEntity
    {
        public TKey Id { get; set; }
        public TDateProperty? CreateDate { get; set; }
        public TKey? CreateUserId { get; set; }
        public TDateProperty? ModifyDate { get; set; }
        public TKey? ModifyUserId { get; set; }
        public byte[] RowVersion { get; set; }
        public TDateProperty DeletedDate { get; set; }
        public TKey? DeletedUserId { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
