using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using System.Reflection;

namespace DocumentDb.Fluent
{
    /// <summary>
    /// Provides properties and instance methods for interacting easily with a DocumentDB instance.
    /// </summary>
    public class Account : IAccount
    {
        private string _endpointUri { get; set; }
        private string _key { get; set; }

        public DocumentClient Client { get; private set; }

        public Account(string endpointUri, string key)
        {
            _endpointUri = endpointUri;
            _key = key;

            Client = new DocumentClient(new Uri(_endpointUri), _key);
        }

        #region IDocumentDbInstance

        public static IAccount Connect(string endpointUri, string primaryKey)
        {
            return new Account(endpointUri, primaryKey);
        }

        public IDatabase Database(string dbId)
        {
            return new Database(this, dbId);
        }

        #endregion

        #region IReadable

        public Microsoft.Azure.Documents.DatabaseAccount Read()
        {
            return Helpers.Synchronize(this.ReadAsync());
        }

        public Task<Microsoft.Azure.Documents.DatabaseAccount> ReadAsync()
        {
            return Client.GetDatabaseAccountAsync();
        }

        #endregion

        #region ICollectionCrud

        public IAccount Add(Microsoft.Azure.Documents.Database database)
        {
            return Helpers.Synchronize(this.AddAsync(database));
        }

        public async Task<IAccount> AddAsync(Microsoft.Azure.Documents.Database database)
        {
            await Client.CreateDatabaseAsync(database);

            return this;
        }

        public IAccount Add(IEnumerable<Microsoft.Azure.Documents.Database> databases)
        {
            return Helpers.Synchronize(this.AddAsync(databases));
        }

        public async Task<IAccount> AddAsync(IEnumerable<Microsoft.Azure.Documents.Database> databases)
        {
            var tasks = new List<Task<ResourceResponse<Microsoft.Azure.Documents.Database>>>();

            foreach (var database in databases)
            {
                tasks.Add(Client.CreateDatabaseAsync(database));
            }

            await Task.WhenAll(tasks.ToArray());

            return this;
        }

        public IAccount Clear()
        {
            return Helpers.Synchronize(this.ClearAsync());
        }

        public async Task<IAccount> ClearAsync()
        {
            await Task.WhenAll(this.WrappedQuery.Select(db => db.DeleteAsync()));
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
        public IAccount Account { get; private set; }

        public DocumentClient Client => Account.Client;
        public string Id { get; set; }

        private Uri _link => UriFactory.CreateDatabaseUri(this.Id);
        private bool _ensured;
        private RequestOptions _requestOptions;

        public Database(IAccount account, string id)
        {
            Account = account;
            Id = id;
        }

        public Uri GetLink()
        {
            return Helpers.Synchronize(this.GetLinkAsync());
        }

        public async Task<Uri> GetLinkAsync()
        {
            await this.EnsureAsync();
            return _link;
        }

        public async Task<IDatabase> EnsureAsync()
        {
            if(!_ensured)
            {
                await this.CreateAsync(new Microsoft.Azure.Documents.Database { Id = this.Id });
                _ensured = true;
            }

            return this;
        }

        #region IDatabase

        public IDocumentCollection<TUnderlying> Collection<TUnderlying>(string collectionId = null) where TUnderlying : class, IId, new()
        {
            if (collectionId == null)
                collectionId = $"{typeof(TUnderlying).Name}s";

            return new DocumentCollection<TUnderlying>(this, collectionId);
        }

        #endregion

        #region ICrd

        public IDatabase Create(Microsoft.Azure.Documents.Database database)
        {
            return Helpers.Synchronize(this.CreateAsync(database));
        }

        public async Task<IDatabase> CreateAsync(Microsoft.Azure.Documents.Database database)
        {
            this.Id = (await Client.CreateDatabaseIfNotExistsAsync(database, _requestOptions)).Resource.Id;
            return this;
        }

        public IEnumerable<IDatabase> Create(IEnumerable<Microsoft.Azure.Documents.Database> databases)
        {
            return Helpers.Synchronize(Account.Database().CreateAsync(databases));
        }

        public async Task<IEnumerable<IDatabase>> CreateAsync(IEnumerable<Microsoft.Azure.Documents.Database> databases)
        {
            return await Task.WhenAll(databases.Select(d => Account.Database().CreateAsync(d)));
        }

        public IDatabase WithRequestOptions(RequestOptions options)
        {
            this._requestOptions = options;
            return this;
        }

        public Microsoft.Azure.Documents.Database Read()
        {
            return Helpers.Synchronize(this.ReadAsync());
        }

        public async Task<Microsoft.Azure.Documents.Database> ReadAsync()
        {
            return (await Client.ReadDatabaseAsync(await this.GetLinkAsync())).Resource;
        }

        public void Delete()
        {
            Helpers.Synchronize(this.DeleteAsync());
        }

        public async Task DeleteAsync()
        {
            await Client.DeleteDatabaseAsync(await this.GetLinkAsync());
        }

        #endregion

        #region ICollectionCrud

        public IDatabase Add(Microsoft.Azure.Documents.DocumentCollection collection)
        {
            return Helpers.Synchronize(this.AddAsync(collection));
        }

        public async Task<IDatabase> AddAsync(Microsoft.Azure.Documents.DocumentCollection collection)
        {
            await Client.CreateDocumentCollectionAsync(await this.GetLinkAsync(), collection);
            return this;
        }

        public IDatabase Add(IEnumerable<Microsoft.Azure.Documents.DocumentCollection> collections)
        {
            return Helpers.Synchronize(this.AddAsync(collections));
        }

        public async Task<IDatabase> AddAsync(IEnumerable<Microsoft.Azure.Documents.DocumentCollection> collections)
        {
            await Task.WhenAll(collections.Select(dc => this.AddAsync(dc)));
            return this;
        }

        public IDatabase Clear()
        {
            return Helpers.Synchronize(this.ClearAsync());
        }

        public async Task<IDatabase> ClearAsync()
        {
            await Task.WhenAll(this.WrappedQuery.Select(dc => dc.DeleteAsync()));
            return this;
        }

        public IOrderedQueryable<Microsoft.Azure.Documents.DocumentCollection> Query => Client.CreateDocumentCollectionQuery(this.GetLink());

        public IEnumerable<IDocumentCollection<HasId>> WrappedQuery => Query.AsEnumerable().Select(dc => new DocumentCollection<HasId>(this, dc.Id));

        #endregion
    }

    /// <summary>
    /// Provides properties and instance methods for interacting with a <see cref="Microsoft.Azure.Documents.DocumentCollection"/> and its <see cref="Microsoft.Azure.Documents.Document"/>s.
    /// </summary>
    /// <typeparam name="TItemUnderlying">The underlying item type (e.g., <code>TodoItem</code>).</typeparam>
    public class DocumentCollection<TItemUnderlying> : 
        IDocumentCollection<TItemUnderlying> 
        where TItemUnderlying : class, IId, new()
    {
        public IDatabase Database { get; private set; }

        public DocumentClient Client => Database.Client;
        public string Id { get; set; }

        private Uri _link => UriFactory.CreateDocumentCollectionUri(Database.Id, this.Id);
        private bool _ensured;
        private Dictionary<string, string> _checkpoints = new Dictionary<string, string>();
        private RequestOptions _requestOptions;

        public DocumentCollection(IDatabase database, string id)
        {
            Database = database;
            Id = id;
        }

        public Uri GetLink()
        {
            return Helpers.Synchronize(this.GetLinkAsync());
        }

        public async Task<Uri> GetLinkAsync()
        {
            await this.EnsureAsync();
            return _link;
        }

        public async Task<IDocumentCollection<TItemUnderlying>> EnsureAsync()
        {
            if (!_ensured)
            {
                await this.CreateAsync(new Microsoft.Azure.Documents.DocumentCollection { Id = this.Id });
                _ensured = true;
            }

            return this;
        }

        #region IDocumentCollection

        public IDocument<TItemUnderlying> Document(string documentId = null)
        {
            return new Document<TItemUnderlying>(this, documentId);
        }

        public IEnumerable<TItemUnderlying> GetChanges()
        {
            return Helpers.Synchronize(this.GetChangesAsync());
        }

        public async Task<IEnumerable<TItemUnderlying>> GetChangesAsync()
        {
            var result = new List<TItemUnderlying>();
            string pkRangesResponseContinuation = null;
            var partitionKeyRanges = new List<Microsoft.Azure.Documents.PartitionKeyRange>();

            do
            {
                var pkRangesResponse = await Client.ReadPartitionKeyRangeFeedAsync(
                    await this.GetLinkAsync(),
                    new FeedOptions { RequestContinuation = pkRangesResponseContinuation });

                partitionKeyRanges.AddRange(pkRangesResponse);
                pkRangesResponseContinuation = pkRangesResponse.ResponseContinuation;
            }
            while (pkRangesResponseContinuation != null);

            foreach (Microsoft.Azure.Documents.PartitionKeyRange pkRange in partitionKeyRanges)
            {
                _checkpoints.TryGetValue(pkRange.Id, out string continuation);

                var query = Client.CreateDocumentChangeFeedQuery(
                    await this.GetLinkAsync(),
                    new ChangeFeedOptions
                    {
                        PartitionKeyRangeId = pkRange.Id,
                        StartFromBeginning = true,
                        RequestContinuation = continuation,
                        MaxItemCount = 1
                    });

                while (query.HasMoreResults)
                {
                    var readChangesResponse = query.ExecuteNextAsync<TItemUnderlying>().Result;

                    foreach (var changedDocument in readChangesResponse)
                    {
                        result.Add(changedDocument);
                    }

                    _checkpoints[pkRange.Id] = readChangesResponse.ResponseContinuation;
                }
            }

            return result;
        }

        public IDocumentCollection<T> Cast<T>() where T : class, TItemUnderlying, new()
        {
            return new DocumentCollection<T>(this.Database, this.Id);
        }

        #endregion

        #region ICrud

        public IDocumentCollection<TItemUnderlying> Create(Microsoft.Azure.Documents.DocumentCollection collection)
        {
            return Helpers.Synchronize(this.CreateAsync(collection));
        }

        public async Task<IDocumentCollection<TItemUnderlying>> CreateAsync(Microsoft.Azure.Documents.DocumentCollection collection)
        {
            this.Id = (await Client.CreateDocumentCollectionIfNotExistsAsync(await this.Database.GetLinkAsync(), collection, _requestOptions)).Resource.Id;
            return this;
        }

        public IEnumerable<IDocumentCollection<TItemUnderlying>> Create(IEnumerable<Microsoft.Azure.Documents.DocumentCollection> collections)
        {
            return Helpers.Synchronize(Database.Collection<TItemUnderlying>().CreateAsync(collections));
        }

        public async Task<IEnumerable<IDocumentCollection<TItemUnderlying>>> CreateAsync(IEnumerable<Microsoft.Azure.Documents.DocumentCollection> collections)
        {
            return await Task.WhenAll(collections.Select(d => Database.Collection<TItemUnderlying>().CreateAsync(d)));
        }

        public IDocumentCollection<TItemUnderlying> WithRequestOptions(RequestOptions options)
        {
            this._requestOptions = options;
            return this;
        }

        public Microsoft.Azure.Documents.DocumentCollection Read()
        {
            return Helpers.Synchronize(this.ReadAsync());
        }

        public async Task<Microsoft.Azure.Documents.DocumentCollection> ReadAsync()
        {
            return (await Client.ReadDocumentCollectionAsync(await this.GetLinkAsync())).Resource;
        }

        public IDocumentCollection<TItemUnderlying> Update(Microsoft.Azure.Documents.DocumentCollection collection)
        {
            return Helpers.Synchronize(this.UpdateAsync(collection));
        }

        public async Task<IDocumentCollection<TItemUnderlying>> UpdateAsync(Microsoft.Azure.Documents.DocumentCollection collection)
        {
            await Client.ReplaceDocumentCollectionAsync(await this.GetLinkAsync(), collection);
            return this;
        }

        public IDocumentCollection<TItemUnderlying> Edit(Action<Microsoft.Azure.Documents.DocumentCollection> func)
        {
            return Helpers.Synchronize(this.EditAsync(func));
        }

        public async Task<IDocumentCollection<TItemUnderlying>> EditAsync(Action<Microsoft.Azure.Documents.DocumentCollection> func)
        {
            var collection = await this.ReadAsync();
            func(collection);
            await this.UpdateAsync(collection);

            return this;
        }

        public void Delete()
        {
            Helpers.Synchronize(this.DeleteAsync());
        }

        public async Task DeleteAsync()
        {
            await Client.DeleteDocumentCollectionAsync(await this.GetLinkAsync());
        }

        #endregion

        #region ICollectionCrud

        public IDocumentCollection<TItemUnderlying> Add(TItemUnderlying document)
        {
            return Helpers.Synchronize(this.AddAsync(document));
        }

        public async Task<IDocumentCollection<TItemUnderlying>> AddAsync(TItemUnderlying document)
        {
            var doc = new Document<TItemUnderlying>(this, (await Client.CreateDocumentAsync(await this.GetLinkAsync(), document)).Resource.Id);

            return this;
        }

        public IDocumentCollection<TItemUnderlying> Add(IEnumerable<TItemUnderlying> documents)
        {
            return Helpers.Synchronize(this.AddAsync(documents));
        }

        public async Task<IDocumentCollection<TItemUnderlying>> AddAsync(IEnumerable<TItemUnderlying> documents)
        {
            await Task.WhenAll(documents.Select(doc => this.AddAsync(doc)));
            return this;
        }

        public IDocumentCollection<TItemUnderlying> Clear()
        {
            return Helpers.Synchronize(this.ClearAsync());
        }

        public async Task<IDocumentCollection<TItemUnderlying>> ClearAsync()
        {
            // TODO: When I add sprocs, add a sproc by default that can bulk delete.
            await Task.WhenAll(this.WrappedQuery.Select(doc => doc.DeleteAsync()));
            return this;
        }

        public IOrderedQueryable<TItemUnderlying> Query => Client.CreateDocumentQuery<TItemUnderlying>(this.GetLink());

        public IEnumerable<IDocument<TItemUnderlying>> WrappedQuery => Query.AsEnumerable().Select(doc => new Document<TItemUnderlying>(this, doc.Id));

        #endregion
    }

    /// <summary>
    /// Provides properties and instance methods for interacting with a <see cref="Microsoft.Azure.Documents.Document"/>.
    /// </summary>
    /// <typeparam name="TUnderlying">The underlying item type (e.g., <code>TodoItem</code>).</typeparam>
    public class Document<TUnderlying> : 
        IDocument<TUnderlying> 
        where TUnderlying : class, IId, new()
    {
        public IDocumentCollection<TUnderlying> Collection { get; private set; }

        public DocumentClient Client => Collection.Client;
        public string Id { get; set; }

        private Uri _link => UriFactory.CreateDocumentUri(Collection.Database.Id, Collection.Id, this.Id);
        private RequestOptions _requestOptions;

        public Document(IDocumentCollection<TUnderlying> collection, string id)
        {
            Collection = collection;
            Id = id;
        }

        public Uri GetLink()
        {
            return Helpers.Synchronize(this.GetLinkAsync());
        }

        public async Task<Uri> GetLinkAsync()
        {
            await this.EnsureAsync();
            return _link;
        }

        public Task<IDocument<TUnderlying>> EnsureAsync()
        {
            return Task.FromResult(this as IDocument<TUnderlying>);
        }

        #region IDocument

        public IDocument<T> Cast<T>() where T : class, TUnderlying, new()
        {
            return new Document<T>(this.Collection.Cast<T>(), this.Id);
        }

        #endregion

        #region ICrud

        public IDocument<TUnderlying> Create(TUnderlying document)
        {
            return Helpers.Synchronize(this.CreateAsync(document));
        }

        public async Task<IDocument<TUnderlying>> CreateAsync(TUnderlying document)
        {
            this.Id = (await Client.CreateDocumentAsync(await Collection.GetLinkAsync(), document, _requestOptions)).Resource.Id;
            return this;
        }

        public IEnumerable<IDocument<TUnderlying>> Create(IEnumerable<TUnderlying> documents)
        {
            return Helpers.Synchronize(Collection.Document().CreateAsync(documents));
        }

        public async Task<IEnumerable<IDocument<TUnderlying>>> CreateAsync(IEnumerable<TUnderlying> documents)
        {
            return await Task.WhenAll(documents.Select(d => Collection.Document().CreateAsync(d)));
        }

        public IDocument<TUnderlying> WithRequestOptions(RequestOptions options)
        {
            _requestOptions = options;
            return this;
        }

        public TUnderlying Read()
        {
            return Helpers.Synchronize(this.ReadAsync());
        }

        public async Task<TUnderlying> ReadAsync()
        {
            return (TUnderlying)(dynamic)(await Client.ReadDocumentAsync(await this.GetLinkAsync())).Resource;
        }

        public IDocument<TUnderlying> Update(TUnderlying document)
        {
            return Helpers.Synchronize(this.UpdateAsync(document));
        }

        public async Task<IDocument<TUnderlying>> UpdateAsync(TUnderlying document)
        {
            document.Id = this.Id;
            await Client.UpsertDocumentAsync(await this.Collection.GetLinkAsync(), document);
            return this;
        }

        public IDocument<TUnderlying> Edit(Action<TUnderlying> func)
        {
            return Helpers.Synchronize(this.EditAsync(func));
        }

        public async Task<IDocument<TUnderlying>> EditAsync(Action<TUnderlying> func)
        {
            var document = await this.ReadAsync();
            func(document);
            await this.UpdateAsync(document);

            return this;
        }

        public void Delete()
        {
            Helpers.Synchronize(this.DeleteAsync());
        }

        public async Task DeleteAsync()
        {
            await Client.DeleteDocumentAsync(await this.GetLinkAsync());
        }

        #endregion
    }
}
