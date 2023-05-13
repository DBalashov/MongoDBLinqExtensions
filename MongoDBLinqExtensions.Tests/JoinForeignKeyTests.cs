using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBLinqExtensions.Join;
using MongoDBLinqExtensions.Tests;
using NUnit.Framework;

namespace MongoDBLinqExtensions.ForeignKey.Tests;

public class JoinForeignKeyTests : Base
{
    const int requestCount        = 10;
    const int requestDetailsCount = 30;
    const int usersCount          = 30;

    readonly DBRequest[] requests = Enumerable.Range(0, requestCount)
                                              .Select(p => new DBRequest() {Name = "request-" + p})
                                              .ToArray();

    [SetUp]
    public async Task Setup()
    {
        Teardown();

        foreach (var item in requests)
            await db.GetCollectionByType<DBRequest>().SaveOneAsync(item);

        foreach (var item in Enumerable.Range(0, requestDetailsCount)
                                       .Select(p => new DBRequestDetail()
                                                    {
                                                        Name      = "request-detail-" + p,
                                                        RequestId = requests[Random.Shared.Next(requests.Length)].Id
                                                    })
                                       .Concat(Enumerable.Range(0, requestDetailsCount)
                                                         .Select(p => new DBRequestDetail()
                                                                      {
                                                                          Name      = "request-detail-" + p,
                                                                          RequestId = ObjectId.GenerateNewId() // invalid (fake) request id
                                                                      })))
            db.GetCollectionByType<DBRequestDetail>().SaveOne(item);

        await db.GetCollectionByType<DBUser>().SaveManyAsync(Enumerable.Range(0, usersCount)
                                                                       .Select(p => new DBUser()
                                                                                    {
                                                                                        Login     = "user-" + p,
                                                                                        RequestId = requests[Random.Shared.Next(requests.Length)].Id
                                                                                    })
                                                                       .Concat(Enumerable.Range(0, usersCount)
                                                                                         .Select(p => new DBUser()
                                                                                                      {
                                                                                                          Login     = "user-fake-" + p,
                                                                                                          RequestId = ObjectId.GenerateNewId() // invalid (fake) user id
                                                                                                      }))
                                                                       .ToArray());
    }

    [TearDown]
    public void Teardown()
    {
        db.DropCollection(typeof(DBRequest).GetCollectionName());
        db.DropCollection(typeof(DBUser).GetCollectionName());
        db.DropCollection(typeof(DBRequestDetail).GetCollectionName());
    }

    [Test]
    public void JoinForeignKey()
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
            Assert.IsTrue(item.User == null || item.User.RequestId == item.Id);
            Assert.IsNotNull(item.RequestDetails);
            Assert.IsTrue(item.RequestDetails.All(p => p.RequestId == item.Id));
        }
    }

    [Test]
    public void JoinInvalid()
    {
        var requestIds = db.GetCollectionByType<DBRequest>()
                           .Find(p => true)
                           .Limit((int) (requestCount * 2 / 3.0))
                           .ToList()
                           .Select(p => p.Id)
                           .ToArray();

        Assert.Throws<NotSupportedException>(() =>
                                             {
                                                 var items = db.GetCollectionByType<DBRequest>()
                                                               .Aggregate()
                                                               .Match(p => requestIds.Contains(p.Id))
                                                               .Join<DBRequest, DBRequestJoined>(p => p.User.Login.ToLower())
                                                               .As<DBRequestJoined>()
                                                               .ToList();
                                             });
    }
}