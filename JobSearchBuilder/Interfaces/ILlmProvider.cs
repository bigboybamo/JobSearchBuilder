using JobSearchBuilder.Models;
using System.Threading.Tasks;

namespace JobSearchBuilder.Interfaces
{
    public interface ILlmProvider
    {
        string ProviderName { get; }
        string ModelId { get; }
        Task<LlmResponse> SendAsync(LlmRequest request);
    }
}
