using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DocumentDb.Fluent.Tests
{
    public class UnitTests
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

            var docId = collection.Clear().Document().Create(obj).Id;

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

            var docId = collection.Clear().Document().Create(obj).Id;
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

            var docId = (await collection.Clear().Document().CreateAsync(obj)).Id;
            Assert.NotNull(await collection.Document(docId).ReadAsync());

            await collection.Document(docId).DeleteAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => collection.Document(docId).ReadAsync());
        }

        [Fact]
        public async void CanAddDocuments()
        {
            var collection = GetDatabase()
                .Collection<TestObject>(CollectionId);

            collection.Clear();

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

        #region Helpers

        private const string DatabaseId = "MyDatabase";
        private const string CollectionId = "MyCollection";

        private IDatabase GetDatabase()
        {
            return DocumentDbInstance
                .Connect(Helpers.Configuration["EndpointUri"], Helpers.Configuration["PrimaryKey"])
                .Database(DatabaseId);
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
