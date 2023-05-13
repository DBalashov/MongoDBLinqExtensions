using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBLinqExtensions.Tests;
using NUnit.Framework;

namespace MongoDBLinqExtensions.Hierarchy.Tests;

public class HierarchyTests : Base
{
    public const int maxGroups = 10;

    [SetUp]
    public async Task Setup()
    {
        Teardown();
        var groups = new List<DBGroup>();
        foreach (var l1 in Enumerable.Range(0, maxGroups))
        {
            var g1 = new DBGroup() {Id = ObjectId.GenerateNewId(), Name = "root-" + l1, Level = 0};
            groups.Add(g1);

            foreach (var l2 in Enumerable.Range(0, maxGroups))
            {
                var g2 = new DBGroup() {Id = ObjectId.GenerateNewId(), ParentId = g1.Id, Name = "subgroup-" + l2, Level = 1};
                groups.Add(g2);

                foreach (var l3 in Enumerable.Range(0, maxGroups))
                {
                    var g3 = new DBGroup() {Id = ObjectId.GenerateNewId(), ParentId = g2.Id, Name = "subsubgroup-" + l3, Level = 2};
                    groups.Add(g3);

                    foreach (var l4 in Enumerable.Range(0, maxGroups))
                    {
                        var g4 = new DBGroup() {Id = ObjectId.GenerateNewId(), ParentId = g3.Id, Name = "subsubsubgroup-" + l4, Level = 3};
                        groups.Add(g4);
                    }
                }
            }
        }

        await db.GetCollectionByType<DBGroup>().InsertManyAsync(groups);
    }

    [TearDown]
    public void Teardown()
    {
        db.DropCollection(typeof(DBGroup).GetCollectionName());
    }

    #region GetHierarchy

    [Test]
    public void GetHierarchy()
    {
        var c = db.GetCollectionByType<DBGroup>();
        var rootIds = c.Find(p => p.ParentId == ObjectId.Empty)
                       .Limit(Random.Shared.Next(1, maxGroups / 2))
                       .ToList()
                       .Select(p => p.Id)
                       .ToArray();

        checkHierarcy(rootIds,
                      c.Aggregate()
                       .Match(p => rootIds.Contains(p.Id))
                       .GetHierarchy());
    }

    [Test]
    public async Task GetHierarchyAsync()
    {
        var c = db.GetCollectionByType<DBGroup>();
        var rootIds = c.Find(p => p.ParentId == ObjectId.Empty)
                       .Limit(Random.Shared.Next(1, maxGroups / 2))
                       .ToList()
                       .Select(p => p.Id)
                       .ToArray();

        checkHierarcy(rootIds,
                      await c.Aggregate()
                             .Match(p => rootIds.Contains(p.Id))
                             .GetHierarchyAsync());
    }

    void checkHierarcy(ObjectId[] rootIds, Dictionary<ObjectId, DBGroup[]> r)
    {
        Assert.IsNotNull(r);
        Assert.IsFalse(r.Keys.Except(rootIds).Any());

        foreach (var (key, value) in r)
        {
            var groups = value;
            Assert.IsNotNull(value);
            Assert.IsTrue(value.Length > 0);

            var l2groups = groups.Where(p => p.ParentId == key).ToArray();
            groups = groups.Where(p => p.ParentId != key).ToArray();
            Assert.IsTrue(l2groups.Length == maxGroups);
            Assert.IsTrue(l2groups.All(g => g.Level == 1));

            var l2groupIds = l2groups.Select(p => p.Id).ToArray();
            var l3groups   = groups.Where(p => l2groupIds.Contains(p.ParentId)).ToArray();
            groups = groups.Where(p => !l2groupIds.Contains(p.ParentId)).ToArray();
            Assert.IsTrue(l3groups.Length == maxGroups * maxGroups);
            Assert.IsTrue(l3groups.All(g => g.Level == 2));

            var l3groupIds = l3groups.Select(p => p.Id).ToArray();
            var l4groups   = groups.Where(p => l3groupIds.Contains(p.ParentId)).ToArray();
            groups = groups.Where(p => !l3groupIds.Contains(p.ParentId)).ToArray();
            Assert.IsTrue(l4groups.Length == maxGroups * maxGroups * maxGroups);
            Assert.IsTrue(l4groups.All(g => g.Level == 3));

            Assert.IsFalse(groups.Any());
        }
    }

    #endregion

    #region GetPath

    [Test]
    public void GetPath()
    {
        var c = db.GetCollectionByType<DBGroup>();
        var leafNodeIds = c.Find(p => p.Level == 3)
                           .Limit(maxGroups * maxGroups)
                           .ToList()
                           .Select(p => p.Id)
                           .ToArray();

        checkPath(leafNodeIds,
                  c.Aggregate()
                   .Match(p => leafNodeIds.Contains(p.Id))
                   .GetPath());
    }

    [Test]
    public async Task GetPathAsync()
    {
        var c = db.GetCollectionByType<DBGroup>();
        var leafNodeIds = c.Find(p => p.Level == 3)
                           .Limit(maxGroups * maxGroups)
                           .ToList()
                           .Select(p => p.Id)
                           .ToArray();

        checkPath(leafNodeIds,
                  await c.Aggregate()
                         .Match(p => leafNodeIds.Contains(p.Id))
                         .GetPathAsync());
    }

    void checkPath(ObjectId[] leafNodeIds, Dictionary<ObjectId, DBGroup[]> r)
    {
        Assert.IsNotNull(r);
        Assert.IsFalse(r.Keys.Except(leafNodeIds).Any());

        foreach (var (key, value) in r)
        {
            Assert.IsNotNull(value);

            var groupId = ObjectId.Empty;
            var index   = 0;
            foreach (var g in value)
            {
                Assert.IsNotNull(g);
                Assert.IsTrue(g.ParentId == groupId);
                Assert.IsTrue(g.Level    == index);

                groupId = g.Id;
                index++;
            }
        }
    }

    #endregion
}