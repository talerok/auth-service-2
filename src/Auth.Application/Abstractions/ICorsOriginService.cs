namespace Auth.Application;

public interface ICorsOriginService
{
    bool IsOriginAllowed(string origin);
    void InvalidateCache();
}
