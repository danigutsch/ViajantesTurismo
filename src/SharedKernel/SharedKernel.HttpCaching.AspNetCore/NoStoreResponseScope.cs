using Microsoft.AspNetCore.Http;

namespace SharedKernel.HttpCaching.AspNetCore;

internal sealed record NoStoreResponseScope(PathString PathPrefix);
