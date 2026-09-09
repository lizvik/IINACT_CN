namespace FetchDependencies;

public static class ApiVersion
{
    public static readonly Version IinactCnApiVersion = new(1, 6, 0);
    public static readonly string NamespaceIdentifier = 
        $"IINACT_CN_API_V{IinactCnApiVersion.ToString().Replace(".", "_")}";
}
