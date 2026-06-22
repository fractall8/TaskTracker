CREATE UNIQUE INDEX "IX_WorkspaceInvites_WorkspaceId_Email"
    ON "WorkspaceInvites" ("WorkspaceId", "Email")
    WHERE "IsDeleted" = false AND "Email" IS NOT NULL;

DROP INDEX IF EXISTS "IX_WorkspaceMembers_WorkspaceId_UserId";

CREATE UNIQUE INDEX "IX_WorkspaceMembers_WorkspaceId_UserId"
    ON "WorkspaceMembers" ("WorkspaceId", "UserId")
    WHERE "IsDeleted" = false;