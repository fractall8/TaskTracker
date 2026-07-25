using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;

namespace Services.Boards;

internal static class BoardActionSyncKey
{
    public static string Resolve(BoardActionNotification notification) =>
        notification.Type switch
        {
            BoardActionNotificationType.BoardRenamed => "board",

            BoardActionNotificationType.ColumnCreated =>
                $"column:{((ColumnCreatedPayload)notification.Payload).ColumnId}",

            BoardActionNotificationType.ColumnRenamed =>
                $"column:{((ColumnRenamedPayload)notification.Payload).ColumnId}",

            BoardActionNotificationType.ColumnDeleted =>
                $"column:{((ColumnDeletedPayload)notification.Payload).ColumnId}",

            BoardActionNotificationType.ColumnsReordered => "board:columns-order",

            BoardActionNotificationType.TaskCreated =>
                $"task:{((TaskCreatedPayload)notification.Payload).BoardTaskId}",

            BoardActionNotificationType.TaskUpdated =>
                $"task:{((TaskUpdatedPayload)notification.Payload).BoardTaskId}",

            BoardActionNotificationType.TaskDeleted =>
                $"task:{((TaskDeletedPayload)notification.Payload).BoardTaskId}",

            BoardActionNotificationType.TasksReordered =>
                $"task:{((TasksReorderedPayload)notification.Payload).BoardTaskId}:position",

            BoardActionNotificationType.TaskCommentsCountChanged =>
                $"task:{((TaskCommentsCountChangedPayload)notification.Payload).BoardTaskId}:comments-count",

            BoardActionNotificationType.TaskAttachmentsCountChanged =>
                $"task:{((TaskAttachmentsCountChangedPayload)notification.Payload).BoardTaskId}:attachments-count",

            BoardActionNotificationType.CallStarted =>
                $"call:{((CallStartedPayload)notification.Payload).Call.Id}",

            BoardActionNotificationType.CallParticipantsChanged =>
                $"call:{((CallParticipantsChangedPayload)notification.Payload).BoardCallId}",

            BoardActionNotificationType.CallEnded =>
                $"call:{((CallEndedPayload)notification.Payload).BoardCallId}",

            _ => $"type:{(byte)notification.Type}",
        };
}
