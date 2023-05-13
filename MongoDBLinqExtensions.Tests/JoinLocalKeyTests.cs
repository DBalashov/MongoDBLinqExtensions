using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBLinqExtensions.Join;
using MongoDBLinqExtensions.Tests;
using NUnit.Framework;

namespace MongoDBLinqExtensions.LocalKey.Tests;

public class JoinLocalKeyTests : Base
{
    const int requestCount        = 10;
    const int requestDetailsCount = 10;
    const int usersCount          = 10;

    readonly DBUser[] users = Enumerable.Range(0, usersCount)
                                        .Select(p => new DBUser() {Id = ObjectId.GenerateNewId(), Login = "user-" + p})
                                        .ToArray();

    readonly DBRequestDetail[] requestDetails = Enumerable.Range(0, requestDetailsCount)
                                                          .Select(p => new DBRequestDetail()
                                                                       {
                                                                           Id   = ObjectId.GenerateNewId(),
                                                                           Name = "request-detail-" + p
                                                                       })
                                                          .ToArray();

    [SetUp]
    public void Setup()
    {
        Teardown();

        db.GetCollectionByType<DBUser>().SaveMany(users);
        db.GetCollectionByType<DBRequestDetail>().SaveMany(requestDetails);

        db.GetCollectionByType<DBRequest>()
          .SaveMany(Enumerable.Range(0, requestCount)
                              .Select(p => new DBRequest()
                                           {
                                               Id     = ObjectId.GenerateNewId(),
                                               UserId = users[Random.Shared.Next(users.Length)].Id,
                                               RequestDetailIds = Enumerable.Range(0, p)
                                                                            .Select(r => requestDetails[Random.Shared.Next(r)].Id)
                                                                            .Concat(p % 2 == 0
                                                                                        ? new[]
                                                                                          {
                                                                                              ObjectId.GenerateNewId(),
                                                                                              ObjectId.GenerateNewId()
                                                                                          }
                                                                                        : Array.Empty<ObjectId>())
                                                                            .Distinct()
                                                                            .ToArray()
                                           }));
    }

    [TearDown]
    public void Teardown()
    {
        db.DropCollection(typeof(DBRequest).GetCollectionName());
        db.DropCollection(typeof(DBUser).GetCollectionName());
        db.DropCollection(typeof(DBRequestDetail).GetCollectionName());
    }

    [Test]
    public void JoinLocalKey()
    {
        var requestIds = db.GetCollectionByType<DBRequest>()
                           .Find(p => true)
                           .Limit((int) (requestCount * 2 / 3.0))
                           .ToList()
                           .Select(p => p.Id)
                           .ToArray();
        var items = db.GetCollectionByType<DBRequest>()
                      .Aggregate()
                      .Match(p => requestIds.Contains(p.Id))
                      .Join<DBRequest, DBRequestJoined>(p => p.User, p => p.RequestDetails)
                      .As<DBRequestJoined>()
                      .ToList();

        Assert.IsFalse(requestIds.Except(items.Select(c => c.Id)).Any());

        foreach (var item in items)
        {
            Assert.IsNotNull(item.User);
            Assert.IsTrue(item.User.Id == item.UserId);

            Assert.IsNotNull(item.RequestDetails);
            Assert.IsTrue(item.RequestDetails.All(p => item.RequestDetailIds.Contains(p.Id)));
        }
    }
}