using AutoMapper;
using Grpc.Core;
using PlatformService;
using PlatformService.Data;

namespace SyncDataServices.Grpc;

public class GrpcPlatformService : GrpcPlatform.GrpcPlatformBase
{
    private readonly IPlatformRepo _repo;
    private readonly IMapper _mapper;
    public GrpcPlatformService(IPlatformRepo repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }
    public override Task<PlatformResponse> GetAllPlatforms(GetAllRequests request, ServerCallContext context)
    {
        PlatformResponse response = new();
        var platforms = _repo.GetAllPlatforms();
        foreach (var platform in platforms)
        {
            response.Platforms.Add(_mapper.Map<GrpcPlatformModel>(platform));
        }
        return Task.FromResult(response);
    }
}