using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using System.Threading.Tasks;

namespace JobSearchBuilder.Services
{
    public class InMemoryLlmProvider : ILlmProvider
    {
        public string ProviderName { get { return "InMemory"; } }
        public string ModelId { get { return "test-model"; } }

        public LlmResponse NextResponse { get; set; }
        public LlmRequest LastRequest { get; private set; }

        public InMemoryLlmProvider()
        {
            NextResponse = new LlmResponse();
        }

        public Task<LlmResponse> SendAsync(LlmRequest request)
        {
            LastRequest = request;
            return Task.FromResult(NextResponse ?? new LlmResponse());
        }
    }
}
