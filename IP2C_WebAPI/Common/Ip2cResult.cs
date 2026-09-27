using IP2C_WebAPI.DTO;

namespace IP2C_WebAPI.Common;

public enum Ip2cStatus
{
    Ok,
    NotFound,
    InvalidIp,
    InvalidCountryCode,
    UpstreamError,
    ConnectionError,
    InternalError
}

public record Ip2cResult(IpInfoDTO? IpInfo, Ip2cStatus Status, string? ErrorMessage = null)
{
    public static Ip2cResult Success(IpInfoDTO info) => new(info, Ip2cStatus.Ok);
    public static Ip2cResult Failure(Ip2cStatus status, string? message = null) => new(null, status, message);

    public bool IsSuccess => Status == Ip2cStatus.Ok && IpInfo is not null;
}

public record IpReportResult(List<IpReportDTO>? Reports, Ip2cStatus Status, string? ErrorMessage = null)
{
    public static IpReportResult Success(List<IpReportDTO> reports) => new(reports, Ip2cStatus.Ok);
    public static IpReportResult Failure(Ip2cStatus status, string? message = null) => new(null, status, message);

    public bool IsSuccess => Status == Ip2cStatus.Ok && Reports is not null;
}
