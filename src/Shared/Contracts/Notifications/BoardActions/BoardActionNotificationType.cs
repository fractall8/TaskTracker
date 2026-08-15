namespace Contracts.Notifications.BoardActions;

public enum BoardActionNotificationType : byte
{
    BoardRenamed = 1,
    ColumnCreated = 2,
    ColumnRenamed = 3,
    ColumnDeleted = 4,
    ColumnsReordered = 5,
    TaskCreated = 6,
    TaskUpdated = 7,
    TaskDeleted = 8,
    TasksReordered = 9,
    TaskCommentsCountChanged = 10,
    TaskAttachmentsCountChanged = 11,
    CommentAdded = 12,
    CommentUpdated = 13,
    CommentDeleted = 14,
    AttachmentAdded = 15,
    AttachmentDeleted = 16,
    TaskDetailsUpdated = 17,
    TaskDueDateUpdated = 18,
    CallStarted = 19,
    CallParticipantsChanged = 20,
    CallEnded = 21,
    TaskCompletionChanged = 22,
}
