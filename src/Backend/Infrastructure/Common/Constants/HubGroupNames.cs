namespace Infrastructure.Common.Constants;

public static class HubGroupNames
{
    public static class BoardExportStatus
    {
        public static string Get(Guid boardId) => $"board-export:{boardId:N}";
    }

    public static class BoardActions
    {
        public static string Get(Guid boardId) => $"board-actions:{boardId:N}";
    }
}
