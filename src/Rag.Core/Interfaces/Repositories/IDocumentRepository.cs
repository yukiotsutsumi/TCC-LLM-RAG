using Rag.Core.Domain.Entities;

namespace Rag.Core.Interfaces.Repositories;

public interface IDocumentRepository 
{ 
    Task InsertAsync(Document d); 
    Task<Document?> GetAsync(Guid id); 
}