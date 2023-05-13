using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;
using MongoDBLinqExtensions.Hierarchy;
using MongoDBLinqExtensions.Join;

namespace MongoDBLinqExtensions;

static class TypesCache
{
    static readonly Dictionary<Type, MongoTypeInfo> types  = new();
    static readonly ReaderWriterLockSlim            rwLock = new();

    internal static MongoTypeInfo FromCache(this Type type)
    {
        rwLock.EnterUpgradeableReadLock();
        try
        {
            if (types.TryGetValue(type, out var typeInfo)) return typeInfo;
            rwLock.EnterWriteLock();

            if (types.TryGetValue(type, out typeInfo)) return typeInfo;
            try
            {
                var collectionName = type.GetCustomAttribute<TableAttribute>();
                if (collectionName == null)
                    throw new NotSupportedException(string.Format(Consts.CANT_FIND_ATTR_TYPE, type));

                var props  = type.GetProperties();
                var propPK = props.FirstOrDefault(p => p.GetCustomAttribute<BsonIdAttribute>() != null) ?? throw new NotSupportedException(string.Format(Consts.CANT_FIND_ATTR, "BsonId"));

                var propParentId = props.FirstOrDefault(p => p.GetCustomAttribute<ParentIdAttribute>() != null);

                var localKeys   = new Dictionary<string, string>(StringComparer.Ordinal);
                var foreignKeys = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var prop in props)
                {
                    var attrLK = prop.GetCustomAttribute<LocalKeyAttribute>();
                    if (attrLK != null)
                    {
                        localKeys.Add(prop.Name, attrLK.Name);
                        continue;
                    }

                    var attrFK = prop.GetCustomAttribute<ForeignKeyAttribute>();
                    if (attrFK != null)
                        foreignKeys.Add(prop.Name, attrFK.Name);
                }

                typeInfo = new MongoTypeInfo(collectionName.Name, propPK, propParentId, localKeys, foreignKeys);
                types.Add(type, typeInfo);
                return typeInfo;
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
        finally
        {
            rwLock.ExitUpgradeableReadLock();
        }
    }

    internal sealed record MongoTypeInfo(string                     CollectionName,
                                         PropertyInfo               PrimaryKey,
                                         PropertyInfo?              ParentId,
                                         Dictionary<string, string> LocalKeys,
                                         Dictionary<string, string> ForeignKeys);
}