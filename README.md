# DocumentDb.Fluent

DocumentDB interaction made easy.

## Information

### Install

```bash
dotnet add package DocumentDb.Fluent
```

### Upgrade from 1.X.X to 2.X.X

* `DocumentDbInstance` => `Account`

### Testing

Create an `appsettings.json` in `src/Tests` which defines your DocumentDB information.  **WARNING: all databases will be cleaned up after the test is complete.**

```json
{
  "EndpointUri": "<endpoint>",
  "PrimaryKey": "<key>"
}
```

Then run tests.

```bash
dotnet restore
dotnet build
dotnet test src/Tests/DocumentDb.Fluent.Tests.csproj
```

### Compatibility

Latest .NET Core.

### Examples

Basic examples.

```csharp
var collection = Account
    .Connect(Helpers.Configuration["EndpointUri"], Helpers.Configuration["PrimaryKey"])
    .Database("MyDb")
    .Collection("MyTodos");

var doc = NewTodo();

// Create a document.
var docId = collection.Document().Create(doc).Id;

// Get a document.
var todo = collection.Document(docId).Read();

// Edit document.
collection.Document(docId).Edit(o => o.Text = "newValue");

// Delete document.
collection.Document(docId).Delete();
```

Synchronous and asynchronous options.

```csharp
var collection = Account
    .Connect(Helpers.Configuration["EndpointUri"], Helpers.Configuration["PrimaryKey"])
    .Database("MyDb")
    .Collection("MyTodos");

// Single add.

var doc = NewTodo();

collection.Add(doc);
await collection.AddAsync(doc);
collection.Document().Create(doc);
await collection.Document().CreateAsync(doc);

// Range add.

var docs = NewTodos();

collection.Add(docs);
await collection.AddAsync(docs);
collection.Document().Create(docs);
await collection.Document().CreateAsync(docs);
```

Queries.

```csharp
var collection = Account
    .Connect(Helpers.Configuration["EndpointUri"], Helpers.Configuration["PrimaryKey"])
    .Database("MyDb")
    .Collection("MyTodos");

// Query documents.
var urgentTodos = collection.Query.Where(t => t.IsUrgent).ToList();

// See changes.
var current = await collection.GetChangesAsync();

collection.Document().Create(NewTodo());
collection.Document().Create(NewTodo());

var addedSinceCurrent = collection.GetChanges();
```

## License

```
The MIT License (MIT)

Copyright (c) 2016 Aaron Roney

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
