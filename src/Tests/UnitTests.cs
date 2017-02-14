using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DocumentDb.Fluent.Tests
{
    public class TestsFixture : IDisposable
    {
        public TestsFixture()
        {
        }

        public void Dispose()
        {
            Assert.Equal(0, Helpers.GetInstance().Clear().Query.AsEnumerable().Count());
        }
    }

    public class UnitTests : IClassFixture<TestsFixture>
    {
        [Fact]
        public void HasDatabase()
        {
            var db = GetDatabase().Read();

            Assert.NotNull(db);
            Assert.NotNull(db.Id);
        }

        [Fact]
        public void HasCollection()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);

            Assert.NotNull(collection);
            Assert.NotNull(collection.Id);
        }

        [Fact]
        public void CanInferCollectionName()
        {
            var collection = GetDatabase()
                .Collection<TestObject>();

            Assert.NotNull(collection);
            Assert.Equal("TestObjects", collection.Id);
        }

        [Fact]
        public async void CanGetDocumentById()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);
            var obj = GetObject();

            var docId = collection.Clear().Document().Create(obj).Id;

            Assert.Equal(obj.Text, collection.Document(docId).Read().Text);
            Assert.Equal(obj.Text, (await collection.Document(docId).ReadAsync()).Text);
            Assert.Equal(0, collection.Clear().Query.AsEnumerable().Count());
        }

        [Fact]
        public async void CanEditDocumentById()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);
            var obj = GetObject();

            var docId = collection.Document().Create(obj).Id;

            var text = "Awesome";
            collection.Document(docId).Edit(o => o.Text = text);
            Assert.Equal(text, collection.Document(docId).Read().Text);

            text = "Cool";
            collection.Document(docId).Edit(o => o.Text = text);
            Assert.Equal(text, collection.Document(docId).Read().Text);

            Assert.Equal(obj.Int, (await collection.Document(docId).ReadAsync()).Int);

            Assert.Equal(0, collection.Clear().Query.AsEnumerable().Count());
        }

        [Fact]
        public void CanDeleteDocumentById()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);
            var obj = GetObject();

            var docId = collection.Document().Create(obj).Id;
            Assert.NotNull(collection.Document(docId).Read());

            collection.Document(docId).Delete();

            Assert.ThrowsAny<Exception>(() => collection.Document(docId).Read());
        }

        [Fact]
        public async void CanDeleteAsyncDocumentById()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);
            var obj = GetObject();

            var docId = (await collection.Document().CreateAsync(obj)).Id;
            Assert.NotNull(await collection.Document(docId).ReadAsync());

            await collection.Document(docId).DeleteAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => collection.Document(docId).ReadAsync());
        }

        [Fact]
        public async void CanAddDocuments()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);

            var doc = GetObject();
            var docs = GetObjectList();

            collection.Add(doc);
            await collection.AddAsync(doc);
            collection.Document().Create(doc);
            await collection.Document().CreateAsync(doc);

            collection.Add(docs);
            await collection.AddAsync(docs);
            collection.Document().Create(docs);
            await collection.Document().CreateAsync(docs);

            Assert.Equal(12, collection.Query.AsEnumerable().Count());
            Assert.Equal(0, collection.Clear().Query.AsEnumerable().Count());
        }

        [Fact]
        public async void CanGetChanges()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);
            var doc = GetObject();

            collection.Clear();

            collection.Add(doc);
            Assert.Equal(1, (await collection.GetChangesAsync()).Count());

            collection.Add(doc);
            collection.Add(doc);
            Assert.Equal(2, (await collection.GetChangesAsync()).Count());

            Assert.Equal(3, collection.Query.AsEnumerable().Count());
            Assert.Equal(0, collection.Clear().Query.AsEnumerable().Count());
        }

        [Fact]
        public void CanQueryCollection()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);

            collection.Add(new TestObject { Text = "1", Int = 1, Double = 1 });
            collection.Add(new TestObject { Text = "1", Int = 1, Double = 1 });
            collection.Add(new TestObject { Text = "2", Int = 2, Double = 2 });

            Assert.Equal(2, collection.Query.Where(o => o.Text == "1").AsEnumerable().Count());
            Assert.Equal(1, collection.Query.Where(o => o.Text == "2").AsEnumerable().Count());
        }

        [Fact]
        public void CanCastDocumentCollection()
        {
            var db = GetDatabase();
            var collection = db.Collection<TestObject>(CollectionId);

            collection.Add(new TestObject { Text = "1", Int = 1, Double = 1 });
            collection.Add(new TestObject { Text = "1", Int = 1, Double = 1 });
            collection.Add(new TestObject { Text = "2", Int = 2, Double = 2 });

            Assert.Equal(1, db.WrappedQuery.Count());
            Assert.Equal(2, db.WrappedQuery.First().Cast<TestObject>().Query.Where(o => o.Text == "1").AsEnumerable().Count());
            Assert.Equal(null, db.WrappedQuery.First().Document().Create(GetObject()).Cast<TestObject2>().Read().Text2);
        }

        [Fact]
        public void CanDeleteCollection()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);

            Assert.NotNull(collection);
            Assert.NotNull(collection.Id);

            collection.Delete();

            Assert.ThrowsAny<Exception>(() => collection.Read());
        }

        [Fact]
        public void CanDeleteDatabase()
        {
            var db = GetDatabase();

            Assert.NotNull(db);
            Assert.NotNull(db.Id);

            db.Delete();

            Assert.ThrowsAny<Exception>(() => db.Read());
        }

        [Fact]
        public void CanClearDatabase()
        {
            var db = GetDatabase();

            Assert.NotNull(db);
            Assert.NotNull(db.Id);

            db.Collection<TestObject>("Collection1");
            db.Collection<TestObject>("Collection2");

            Assert.Equal(2, db.Query.AsEnumerable().Count());
            Assert.Equal(0, db.Clear().Query.AsEnumerable().Count());
        }

        #region Helpers
        
        private const string CollectionId = "MyTestCollection";

        private IDatabase GetDatabase()
        {
            return Helpers.GetInstance()
                .Database(Guid.NewGuid().ToString());
        }

        private TestObject GetObject()
        {
            return new TestObject
            {
                Text = "Hello",
                Int = 1,
                Double = 2.71
            };
        }

        private List<TestObject> GetObjectList()
        {
            return new List<TestObject>{
                new TestObject
                {
                    Text = "Hello",
                    Int = 1,
                    Double = 2.71
                },
                new TestObject
                {
                    Text = "World",
                    Int = 2,
                    Double = 3.14
                },
            };
        }

        #endregion
    }
}
