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

            // Each call notification type gets its own sub-key (unlike Task's shared "task:{id}" for
            // Created/Updated/Deleted) because CallParticipantsChanged is driven by an async,
            // potentially-delayed, at-least-once Event Grid webhook — sharing one key would let a
            // delayed participants-changed notification advance the guard past a legitimate,
            // not-yet-applied CallEnded (or vice versa) for the same call.
            BoardActionNotificationType.CallStarted =>
                $"call:{((CallStartedPayload)notification.Payload).Call.Id}:started",

            BoardActionNotificationType.CallParticipantsChanged =>
                $"call:{((CallParticipantsChangedPayload)notification.Payload).BoardCallId}:participants",

            BoardActionNotificationType.CallEnded =>
                $"call:{((CallEndedPayload)notification.Payload).BoardCallId}:ended",

            _ => $"type:{(byte)notification.Type}",
        };
}
