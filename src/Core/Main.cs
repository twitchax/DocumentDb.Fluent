using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

namespace DocumentDb.Fluent
{
    /// <summary>
    /// Provides properties and instance methods for interacting easily with a DocumentDB instance.
    /// </summary>
    public class DocumentDbInstance : IDocumentDbInstance
    {
        private string _endpointUri { get; set; }
        private string _key { get; set; }

        public DocumentClient Client { get; private set; }

        public DocumentDbInstance(string endpointUri, string key)
        {
            _endpointUri = endpointUri;
            _key = key;

            Client = new DocumentClient(new Uri(_endpointUri), _key);
        }

        #region IDocumentDbInstance

        public static IDocumentDbInstance Connect(string endpointUri, string primaryKey)
        {
            return new DocumentDbInstance(endpointUri, primaryKey);
        }

        public IDatabase Database(string dbId)
        {
            var db = new Database(this, dbId);
            return db.Init();
        }

        public async Task<IDatabase> DatabaseAsync(string dbId)
        {
            var db = new Database(this, dbId);
            return await db.InitAsync();
        }

        #endregion

        #region CRUD

        public IDocumentDbInstance Add(Microsoft.Azure.Documents.Database database)
        {
            var task = Client.CreateDatabaseAsync(database);
            task.Wait();

            return this;
        }

        public async Task<IDocumentDbInstance> AddAsync(Microsoft.Azure.Documents.Database database)
        {
            await Client.CreateDatabaseAsync(database);

            return this;
        }

        public IDocumentDbInstance Add(IEnumerable<Microsoft.Azure.Documents.Database> databases)
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.Database>>>();

            foreach(var database in databases)
            {
                tasks.Add(Client.CreateDatabaseAsync(database));
            }

            Task.WaitAll(tasks.ToArray());

            return this;
        }

        public async Task<IDocumentDbInstance> AddAsync(IEnumerable<Microsoft.Azure.Documents.Database> databases)
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.Database>>>();

            foreach (var database in databases)
            {
                tasks.Add(Client.CreateDatabaseAsync(database));
            }

            await Task.WhenAll(tasks.ToArray());

            return this;
        }

        public IDocumentDbInstance Clear()
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.Database>>>();

            foreach(var db in Client.CreateDatabaseQuery())
            {
                tasks.Add(Client.DeleteDatabaseAsync(db.SelfLink));
            }

            Task.WaitAll();
            
            return this;
        }

        public async Task<IDocumentDbInstance> ClearAsync()
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.Database>>>();

            foreach (var db in Client.CreateDatabaseQuery())
            {
                tasks.Add(Client.DeleteDatabaseAsync(db.SelfLink));
            }

            await Task.WhenAll();

            return this;
        }

        public IOrderedQueryable<Microsoft.Azure.Documents.Database> Query => Client.CreateDatabaseQuery();

        public IEnumerable<IDatabase> WrappedQuery => Query.AsEnumerable().Select(db => new Database(this, db.Id));

        #endregion
    }

    /// <summary>
    /// Provides properties and instance methods for interacting with a <see cref="Microsoft.Azure.Documents.Database"/> and its <see cref="Microsoft.Azure.Documents.DocumentCollection"/>s.
    /// </summary>
    public class Database : IDatabase
    {
        public IDocumentDbInstance Instance { get; private set; }

        public DocumentClient Client => Instance.Client;
        public string Id { get; set; }
        public Uri Link => UriFactory.CreateDatabaseUri(this.Id);

        public Database(IDocumentDbInstance instance, string id)
        {
            Instance = instance;
            Id = id;
        }

        public IDatabase Init()
        {
            Client.CreateDatabaseIfNotExistsAsync(new Microsoft.Azure.Documents.Database { Id = this.Id }).Wait();
            return this;
        }

        public async Task<IDatabase> InitAsync()
        {
            await Client.CreateDatabaseIfNotExistsAsync(new Microsoft.Azure.Documents.Database { Id = this.Id });
            return this;
        }

        #region IDatabase

        public IDocumentCollection<TUnderlying> Collection<TUnderlying>(string collectionId = null) where TUnderlying : class, IId
        {
            if (collectionId == null)
                collectionId = $"{typeof(TUnderlying).Name}s";

            var collection = new DocumentCollection<TUnderlying>(this, collectionId);
            return collection.Init();
        }

        public async Task<IDocumentCollection<TUnderlying>> CollectionAsync<TUnderlying>(string collectionId = null) where TUnderlying : class, IId
        {
            if (collectionId == null)
                collectionId = $"{typeof(TUnderlying).Name}s";

            var collection = new DocumentCollection<TUnderlying>(this, collectionId);
            return await collection.InitAsync();
        }

        #endregion

        #region CRUD

        public IDatabase Add(Microsoft.Azure.Documents.DocumentCollection collection)
        {
            var task = Client.CreateDocumentCollectionAsync(this.Link, collection);
            task.Wait();

            return this;
        }

        public async Task<IDatabase> AddAsync(Microsoft.Azure.Documents.DocumentCollection collection)
        {
            await Client.CreateDocumentCollectionAsync(this.Link, collection);

            return this;
        }

        public IDatabase Add(IEnumerable<Microsoft.Azure.Documents.DocumentCollection> collections)
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.DocumentCollection>>>();

            foreach (var collection in collections)
            {
                tasks.Add(Client.CreateDocumentCollectionAsync(this.Link, collection));
            }

            Task.WaitAll(tasks.ToArray());

            return this;
        }

        public async Task<IDatabase> AddAsync(IEnumerable<Microsoft.Azure.Documents.DocumentCollection> collections)
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.DocumentCollection>>>();

            foreach (var collection in collections)
            {
                tasks.Add(Client.CreateDocumentCollectionAsync(this.Link, collection));
            }

            await Task.WhenAll(tasks.ToArray());

            return this;
        }

        public Microsoft.Azure.Documents.Database Read()
        {
            var task = Client.ReadDatabaseAsync(this.Link);
            task.Wait();

            return task.Result.Resource;
        }
        public async Task<Microsoft.Azure.Documents.Database> ReadAsync()
        {
            return (await Client.ReadDatabaseAsync(this.Link)).Resource;
        }

        public void Delete()
        {
            Client.DeleteDatabaseAsync(this.Link).Wait();
        }

        public Task DeleteAsync()
        {
            return Client.DeleteDatabaseAsync(this.Link);
        }

        public IDatabase Clear()
        {
            Client.DeleteDatabaseAsync(this.Link).Wait();
            return this.Init();
        }

        public async Task<IDatabase> ClearAsync()
        {
            await Client.DeleteDatabaseAsync(this.Link);
            return await this.InitAsync();
        }

        public IOrderedQueryable<Microsoft.Azure.Documents.DocumentCollection> Query => Client.CreateDocumentCollectionQuery(this.Link);

        public IEnumerable<IDocumentCollection<IId>> WrappedQuery => Query.AsEnumerable().Select(db => new DocumentCollection<IId>(this, db.Id));

        #endregion
    }

    /// <summary>
    /// Provides properties and instance methods for interacting with a <see cref="Microsoft.Azure.Documents.DocumentCollection"/> and its <see cref="Microsoft.Azure.Documents.Document"/>s.
    /// </summary>
    /// <typeparam name="TUnderlying">The underlying item type (e.g., <code>TodoItem</code>).</typeparam>
    public class DocumentCollection<TUnderlying> : IDocumentCollection<TUnderlying> where TUnderlying : class, IId
    {
        public IDatabase Database { get; private set; }

        public DocumentClient Client => Database.Client;
        public string Id { get; set; }
        public Uri Link => UriFactory.CreateDocumentCollectionUri(Database.Id, this.Id);

        private Dictionary<string, string> _checkpoints = new Dictionary<string, string>();

        public DocumentCollection(IDatabase database, string id)
        {
            Database = database;
            Id = id;
        }

        public IDocumentCollection<TUnderlying> Init()
        {
            Client.CreateDocumentCollectionIfNotExistsAsync(Database.Link, new Microsoft.Azure.Documents.DocumentCollection { Id = this.Id }).Wait();
            return this;
        }

        public async Task<IDocumentCollection<TUnderlying>> InitAsync()
        {
            await Client.CreateDocumentCollectionIfNotExistsAsync(Database.Link, new Microsoft.Azure.Documents.DocumentCollection { Id = this.Id });
            return this;
        }

        #region IDocumentCollection

        public IDocument<TUnderlying> Document(string documentId = null)
        {
            var document = new Document<TUnderlying>(this, documentId);
            return document.Init();
        }

        public async Task<IDocument<TUnderlying>> DocumentAsync(string documentId = null)
        {

            var document = new Document<TUnderlying>(this, documentId);
            return await document.InitAsync();
        }

        public async Task<IEnumerable<TUnderlying>> GetChangesAsync()
        {
            var result = new List<TUnderlying>();
            string pkRangesResponseContinuation = null;
            var partitionKeyRanges = new List<Microsoft.Azure.Documents.PartitionKeyRange>();

            do
            {
                var pkRangesResponse = await Client.ReadPartitionKeyRangeFeedAsync(
                    this.Link,
                    new FeedOptions { RequestContinuation = pkRangesResponseContinuation });

                partitionKeyRanges.AddRange(pkRangesResponse);
                pkRangesResponseContinuation = pkRangesResponse.ResponseContinuation;
            }
            while (pkRangesResponseContinuation != null);

            foreach (Microsoft.Azure.Documents.PartitionKeyRange pkRange in partitionKeyRanges)
            {
                _checkpoints.TryGetValue(pkRange.Id, out string continuation);

                var query = Client.CreateDocumentChangeFeedQuery(
                    this.Link,
                    new ChangeFeedOptions
                    {
                        PartitionKeyRangeId = pkRange.Id,
                        StartFromBeginning = true,
                        RequestContinuation = continuation,
                        MaxItemCount = 1
                    });

                while (query.HasMoreResults)
                {
                    var readChangesResponse = query.ExecuteNextAsync<TUnderlying>().Result;

                    foreach (var changedDocument in readChangesResponse)
                    {
                        result.Add(changedDocument);
                    }

                    _checkpoints[pkRange.Id] = readChangesResponse.ResponseContinuation;
                }
            }

            return result;
        }

        public IDocumentCollection<T> Cast<T>() where T : class, TUnderlying
        {
            return new DocumentCollection<T>(this.Database, this.Id);
        }

        #endregion

        #region CRUD

        public IDocumentCollection<TUnderlying> Add(TUnderlying document)
        {
            var task = Client.CreateDocumentAsync(this.Link, document);
            task.Wait();

            var doc = new Document<TUnderlying>(this, task.Result.Resource.Id);
            doc.Init();

            return this;
        }

        public async Task<IDocumentCollection<TUnderlying>> AddAsync(TUnderlying document)
        {
            var doc = new Document<TUnderlying>(this, (await Client.CreateDocumentAsync(this.Link, document)).Resource.Id);
            await doc.InitAsync();

            return this;
        }

        public IDocumentCollection<TUnderlying> Add(IEnumerable<TUnderlying> documents)
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.Document>>>();

            foreach (var document in documents)
                tasks.Add(Client.CreateDocumentAsync(this.Link, document));

            Task.WaitAll(tasks.ToArray());

            return this;
        }

        public async Task<IDocumentCollection<TUnderlying>> AddAsync(IEnumerable<TUnderlying> documents)
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.Document>>>();

            foreach (var document in documents)
                tasks.Add(Client.CreateDocumentAsync(this.Link, document));

            await Task.WhenAll(tasks.ToArray());

            return this;
        }

        public Microsoft.Azure.Documents.DocumentCollection Read()
        {
            var task = Client.ReadDocumentCollectionAsync(this.Link);
            task.Wait();

            return task.Result.Resource;
        }

        public async Task<Microsoft.Azure.Documents.DocumentCollection> ReadAsync()
        {
            return (await Client.ReadDocumentCollectionAsync(this.Link)).Resource;
        }

        public void Delete()
        {
            Client.DeleteDocumentCollectionAsync(this.Link).Wait();
        }

        public Task DeleteAsync()
        {
            return Client.DeleteDocumentCollectionAsync(this.Link);
        }

        public IDocumentCollection<TUnderlying> Clear()
        {
            Client.DeleteDocumentCollectionAsync(this.Link).Wait();
            return this.Init();
        }

        public async Task<IDocumentCollection<TUnderlying>> ClearAsync()
        {
            await Client.DeleteDocumentCollectionAsync(this.Link);
            return await this.InitAsync();
        }

        public IOrderedQueryable<TUnderlying> Query => Client.CreateDocumentQuery<TUnderlying>(this.Link);

        #endregion
    }

    /// <summary>
    /// Provides properties and instance methods for interacting with a <see cref="Microsoft.Azure.Documents.Document"/>.
    /// </summary>
    /// <typeparam name="TUnderlying">The underlying item type (e.g., <code>TodoItem</code>).</typeparam>
    public class Document<TUnderlying> : IDocument<TUnderlying> where TUnderlying : class, IId
    {
        public IDocumentCollection<TUnderlying> Collection { get; private set; }

        public DocumentClient Client => Collection.Client;
        public string Id { get; set; }
        public Uri Link => UriFactory.CreateDocumentUri(Collection.Database.Id, Collection.Id, this.Id);

        public Document(IDocumentCollection<TUnderlying> collection, string id)
        {
            Collection = collection;
            Id = id;
        }

        public IDocument<TUnderlying> Init()
        {
            return this;
        }

        public Task<IDocument<TUnderlying>> InitAsync()
        {
            return Task.FromResult(this as IDocument<TUnderlying>);
        }

        #region IDocument

        public IDocument<TUnderlying> Edit(Action<TUnderlying> func)
        {
            var document = this.Read();
            func(document);
            this.Update(document);

            return this;
        }

        public async Task<IDocument<TUnderlying>> EditAsync(Action<TUnderlying> func)
        {
            var document = await this.ReadAsync();
            func(document);
            await this.UpdateAsync(document);

            return this;
        }

        public IDocument<T> Cast<T>() where T : class, TUnderlying
        {
            return new Document<T>(this.Collection.Cast<T>(), this.Id);
        }

        #endregion

        #region CRUD

        public IDocument<TUnderlying> Create(TUnderlying document)
        {
            var task = Client.CreateDocumentAsync(Collection.Link, document);
            task.Wait();

            this.Id = task.Result.Resource.Id;

            return this;
        }

        public async Task<IDocument<TUnderlying>> CreateAsync(TUnderlying document)
        {
            this.Id = (await Client.CreateDocumentAsync(Collection.Link, document)).Resource.Id;
            return this;
        }

        public IEnumerable<IDocument<TUnderlying>> Create(IEnumerable<TUnderlying> documents)
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.Document>>>();

            foreach (var document in documents)
                tasks.Add(Client.CreateDocumentAsync(Collection.Link, document));

            Task.WaitAll(tasks.ToArray());

            return tasks.Select(t =>
            {
                var doc = new Document<TUnderlying>(this.Collection, t.Result.Resource.Id);
                return doc.Init();
            });
        }

        public async Task<IEnumerable<IDocument<TUnderlying>>> CreateAsync(IEnumerable<TUnderlying> documents)
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.Document>>>();

            foreach (var document in documents)
                tasks.Add(Client.CreateDocumentAsync(Collection.Link, document));

            await Task.WhenAll(tasks.ToArray());

            return tasks.Select(t =>
            {
                var doc = new Document<TUnderlying>(this.Collection, t.Result.Resource.Id);
                return doc.Init();
            });
        }

        public TUnderlying Read()
        {
            var task = Client.ReadDocumentAsync(this.Link);
            task.Wait();

            return (TUnderlying)(dynamic)task.Result.Resource;
        }

        public async Task<TUnderlying> ReadAsync()
        {
            return (TUnderlying)(dynamic)(await Client.ReadDocumentAsync(this.Link)).Resource;
        }

        public IDocument<TUnderlying> Update(TUnderlying document)
        {
            document.Id = this.Id;
            Client.UpsertDocumentAsync(this.Collection.Link, document).Wait();
            return this;
        }

        public async Task<IDocument<TUnderlying>> UpdateAsync(TUnderlying document)
        {
            document.Id = this.Id;
            await Client.UpsertDocumentAsync(this.Collection.Link, document);
            return this;
        }

        public void Delete()
        {
            Client.DeleteDocumentAsync(this.Link).Wait();
        }

        public Task DeleteAsync()
        {
            return Client.DeleteDocumentAsync(this.Link);
        }

        #endregion
    }
}
